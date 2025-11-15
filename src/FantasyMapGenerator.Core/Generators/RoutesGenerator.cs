namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

public class RoutesGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;
    private readonly List<Route> _existingRoutes = new();

    public RoutesGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    public List<Route> Generate()
    {
        Console.WriteLine("Generating routes...");

        var routes = new List<Route>();

        // Generate roads
        var roads = GenerateRoads(_map.Burgs, _map.States);
        routes.AddRange(roads);
        Console.WriteLine($"Generated {roads.Count} roads");

        // Generate sea routes
        if (_settings.GenerateSeaRoutes)
        {
            var seaRoutes = GenerateSeaRoutes(_map.Burgs);
            routes.AddRange(seaRoutes);
            Console.WriteLine($"Generated {seaRoutes.Count} sea routes");
        }

        // Optimize routes
        OptimizeRoutes(routes);
        Console.WriteLine($"Optimized to {routes.Count} total routes");

        return routes;
    }

    private List<Route> GenerateRoads(List<Burg> burgs, List<State> states)
    {
        var routes = new List<Route>();
        int routeId = 1;

        foreach (var state in states.Where(s => s.Id > 0))
        {
            var stateBurgs = burgs
                .Where(b => b != null && b.StateId == state.Id)
                .ToList();

            if (stateBurgs.Count < 2) continue;

            // Find capital
            var capital = stateBurgs.FirstOrDefault(b => b.IsCapital);
            if (capital == null) capital = stateBurgs[0];

            // Connect capital to all other burgs
            foreach (var burg in stateBurgs.Where(b => b.Id != capital.Id))
            {
                var path = FindPath(capital.CellId, burg.CellId, RouteType.Road);

                if (path != null && path.Any())
                {
                    var route = new Route
                    {
                        Id = routeId++,
                        Type = RouteType.Road,
                        StartBurgId = capital.Id,
                        EndBurgId = burg.Id,
                        Path = path,
                        Length = CalculatePathLength(path)
                    };
                    routes.Add(route);
                    _existingRoutes.Add(route);
                }
            }

            // Connect nearby burgs with trails
            for (int i = 0; i < stateBurgs.Count; i++)
            {
                for (int j = i + 1; j < stateBurgs.Count; j++)
                {
                    var burg1 = stateBurgs[i];
                    var burg2 = stateBurgs[j];

                    double distance = Distance(
                        _map.Cells[burg1.CellId].Center,
                        _map.Cells[burg2.CellId].Center);

                    // Only connect if close enough
                    if (distance < (_map.Width + _map.Height) / 10.0)
                    {
                        var path = FindPath(burg1.CellId, burg2.CellId, RouteType.Trail);

                        if (path != null && path.Any())
                        {
                            var route = new Route
                            {
                                Id = routeId++,
                                Type = RouteType.Trail,
                                StartBurgId = burg1.Id,
                                EndBurgId = burg2.Id,
                                Path = path,
                                Length = CalculatePathLength(path)
                            };
                            routes.Add(route);
                            _existingRoutes.Add(route);
                        }
                    }
                }
            }
        }

        return routes;
    }

    private List<Route> GenerateSeaRoutes(List<Burg> burgs)
    {
        var routes = new List<Route>();
        int routeId = 1000;

        // Get all port burgs
        var ports = burgs.Where(b => b != null && b.IsPort).ToList();

        if (ports.Count < 2) return routes;

        // Connect ports on same water body
        var portsByFeature = ports.GroupBy(p => p.PortFeatureId ?? 0);

        foreach (var group in portsByFeature)
        {
            var featurePorts = group.ToList();

            if (featurePorts.Count < 2) continue;

            // Connect each port to nearest ports
            foreach (var port in featurePorts)
            {
                var nearestPorts = featurePorts
                    .Where(p => p.Id != port.Id)
                    .OrderBy(p => Distance(
                        _map.Cells[port.CellId].Center,
                        _map.Cells[p.CellId].Center))
                    .Take(3)
                    .ToList();

                foreach (var nearPort in nearestPorts)
                {
                    // Check if route already exists
                    if (routes.Any(r =>
                        (r.StartBurgId == port.Id && r.EndBurgId == nearPort.Id) ||
                        (r.StartBurgId == nearPort.Id && r.EndBurgId == port.Id)))
                        continue;

                    var path = FindPath(port.CellId, nearPort.CellId, RouteType.SeaRoute);

                    if (path != null && path.Any())
                    {
                        routes.Add(new Route
                        {
                            Id = routeId++,
                            Type = RouteType.SeaRoute,
                            StartBurgId = port.Id,
                            EndBurgId = nearPort.Id,
                            Path = path,
                            Length = CalculatePathLength(path),
                            FeatureId = port.PortFeatureId ?? 0
                        });
                    }
                }
            }
        }

        return routes;
    }

    private List<int>? FindPath(int startCell, int endCell, RouteType routeType)
    {
        if (startCell == endCell) return new List<int> { startCell };

        var openSet = new PriorityQueue<PathNode, double>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, double>();

        gScore[startCell] = 0;
        double fScore = Heuristic(startCell, endCell);
        openSet.Enqueue(new PathNode(startCell), fScore);

        int iterations = 0;
        int maxIterations = _settings.MaxRouteLength * 10;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            var current = openSet.Dequeue();

            if (current.CellId == endCell)
            {
                return ReconstructPath(cameFrom, current.CellId);
            }

            var cell = _map.Cells[current.CellId];

            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];

                // Calculate movement cost
                double moveCost = CalculateMovementCost(cell, neighbor, routeType);
                
                // Skip if impassable
                if (moveCost >= 10000) continue;

                double tentativeGScore = gScore[current.CellId] + moveCost;

                if (!gScore.ContainsKey(neighborId) || tentativeGScore < gScore[neighborId])
                {
                    cameFrom[neighborId] = current.CellId;
                    gScore[neighborId] = tentativeGScore;
                    double fScoreNeighbor = tentativeGScore + Heuristic(neighborId, endCell);

                    openSet.Enqueue(new PathNode(neighborId), fScoreNeighbor);
                }
            }
        }

        return null; // No path found
    }

    private double CalculateMovementCost(Cell from, Cell to, RouteType routeType)
    {
        // Base cost
        double cost = 10;

        // Height difference penalty
        double heightDiff = Math.Abs(to.Height - from.Height);
        cost += heightDiff * 2;

        // Water crossing
        if (to.Height < 20)
        {
            if (routeType == RouteType.SeaRoute)
                cost = 5; // Low cost for sea routes
            else
                return 10000; // Impassable for land routes
        }
        else if (routeType == RouteType.SeaRoute)
        {
            return 10000; // Sea routes can't go on land
        }

        // River crossing penalty
        if (to.RiverId > 0 && to.RiverId != from.RiverId)
        {
            cost += Math.Min(to.Flux / 10.0, 50); // Bridge cost
        }

        // Existing road bonus
        if (_existingRoutes.Any(r => r.Path.Contains(to.Id)))
        {
            cost *= 0.5; // Prefer existing routes
        }

        // Mountain penalty
        if (to.Height >= 67)
        {
            cost += 100;
        }

        return cost;
    }

    private double Heuristic(int cellId, int goalId)
    {
        var cell = _map.Cells[cellId];
        var goal = _map.Cells[goalId];

        return Distance(cell.Center, goal.Center);
    }

    private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var path = new List<int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    private double CalculatePathLength(List<int> path)
    {
        double length = 0;

        for (int i = 0; i < path.Count - 1; i++)
        {
            var cell1 = _map.Cells[path[i]];
            var cell2 = _map.Cells[path[i + 1]];
            length += Distance(cell1.Center, cell2.Center);
        }

        return length;
    }

    private void OptimizeRoutes(List<Route> routes)
    {
        var toRemove = new HashSet<int>();

        for (int i = 0; i < routes.Count; i++)
        {
            if (toRemove.Contains(routes[i].Id)) continue;

            for (int j = i + 1; j < routes.Count; j++)
            {
                if (toRemove.Contains(routes[j].Id)) continue;

                var route1 = routes[i];
                var route2 = routes[j];

                // Same endpoints?
                bool sameEndpoints =
                    (route1.StartBurgId == route2.StartBurgId && route1.EndBurgId == route2.EndBurgId) ||
                    (route1.StartBurgId == route2.EndBurgId && route1.EndBurgId == route2.StartBurgId);

                if (sameEndpoints)
                {
                    // Keep shorter route
                    if (route1.Length > route2.Length)
                        toRemove.Add(route1.Id);
                    else
                        toRemove.Add(route2.Id);
                    continue;
                }

                // Check path overlap
                var overlap = route1.Path.Intersect(route2.Path).Count();
                double overlapPercent = overlap / (double)Math.Min(route1.Path.Count, route2.Path.Count);

                if (overlapPercent > 0.8)
                {
                    // Routes are very similar, keep road over trail
                    if (route1.Type == RouteType.Road && route2.Type == RouteType.Trail)
                        toRemove.Add(route2.Id);
                    else if (route2.Type == RouteType.Road && route1.Type == RouteType.Trail)
                        toRemove.Add(route1.Id);
                }
            }
        }

        routes.RemoveAll(r => toRemove.Contains(r.Id));
    }

    private double Distance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private record PathNode(int CellId);
}
