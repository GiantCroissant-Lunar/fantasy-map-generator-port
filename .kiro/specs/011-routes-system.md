# Spec 011: Routes System

## Status
- **State:** ✅ Complete
- **Priority:** ⭐⭐⭐ Medium
- **Estimated Effort:** 1 week
- **Dependencies:** Burgs (006), States (007)
- **Blocks:** None (optional feature)
- **Completed:** November 15, 2025

## Overview

Implement the routes system that generates roads and sea routes connecting burgs. Routes follow terrain, avoid obstacles, and create realistic trade networks.

## Goals

1. **Road Generation** - Create land routes between burgs
2. **Sea Routes** - Create naval routes for coastal burgs
3. **Route Optimization** - Use A* pathfinding for efficient routes
4. **Route Types** - Classify as roads, trails, or sea routes
5. **Route Networks** - Connect burgs within states and between neighbors

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/routes-generator.js`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class Route
{
    public int Id { get; set; }
    public RouteType Type { get; set; }
    public int StartBurgId { get; set; }
    public int EndBurgId { get; set; }
    public List<int> Path { get; set; } = new(); // Cell IDs
    public double Length { get; set; }
    public int FeatureId { get; set; } // For sea routes
}

public enum RouteType
{
    Road,       // Major road
    Trail,      // Minor trail
    SeaRoute    // Naval route
}
```

## Algorithm

### 1. Generate Roads

```csharp
private List<Route> GenerateRoads(List<Burg> burgs, List<State> states)
{
    var routes = new List<Route>();
    int routeId = 1;
    
    // Connect burgs within each state
    foreach (var state in states.Where(s => s.Id > 0))
    {
        var stateBurgs = burgs
            .Where(b => b != null && b.StateId == state.Id)
            .ToList();
        
        if (stateBurgs.Count < 2) continue;
        
        // Connect capital to all other burgs
        var capital = stateBurgs.First(b => b.IsCapital);
        
        foreach (var burg in stateBurgs.Where(b => !b.IsCapital))
        {
            var path = FindPath(capital.CellId, burg.CellId, RouteType.Road);
            
            if (path != null && path.Any())
            {
                routes.Add(new Route
                {
                    Id = routeId++,
                    Type = RouteType.Road,
                    StartBurgId = capital.Id,
                    EndBurgId = burg.Id,
                    Path = path,
                    Length = CalculatePathLength(path)
                });
            }
        }
        
        // Connect nearby burgs
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
                        routes.Add(new Route
                        {
                            Id = routeId++,
                            Type = RouteType.Trail,
                            StartBurgId = burg1.Id,
                            EndBurgId = burg2.Id,
                            Path = path,
                            Length = CalculatePathLength(path)
                        });
                    }
                }
            }
        }
    }
    
    return routes;
}
```

### 2. A* Pathfinding

```csharp
private List<int>? FindPath(int startCell, int endCell, RouteType routeType)
{
    var openSet = new PriorityQueue<PathNode, double>();
    var cameFrom = new Dictionary<int, int>();
    var gScore = new Dictionary<int, double>();
    var fScore = new Dictionary<int, double>();
    
    gScore[startCell] = 0;
    fScore[startCell] = Heuristic(startCell, endCell);
    openSet.Enqueue(new PathNode(startCell), fScore[startCell]);
    
    while (openSet.Count > 0)
    {
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
            double tentativeGScore = gScore[current.CellId] + moveCost;
            
            if (!gScore.ContainsKey(neighborId) || tentativeGScore < gScore[neighborId])
            {
                cameFrom[neighborId] = current.CellId;
                gScore[neighborId] = tentativeGScore;
                fScore[neighborId] = tentativeGScore + Heuristic(neighborId, endCell);
                
                openSet.Enqueue(new PathNode(neighborId), fScore[neighborId]);
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
    
    // Water crossing penalty
    if (to.Height < 20)
    {
        if (routeType == RouteType.SeaRoute)
            cost = 5; // Low cost for sea routes
        else
            cost += 1000; // High penalty for land routes
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

private record PathNode(int CellId);
```

### 3. Generate Sea Routes

```csharp
private List<Route> GenerateSeaRoutes(List<Burg> burgs)
{
    var routes = new List<Route>();
    int routeId = 1000; // Start at 1000 to avoid conflicts
    
    // Get all port burgs
    var ports = burgs.Where(b => b != null && b.IsPort).ToList();
    
    if (ports.Count < 2) return routes;
    
    // Connect ports on same water body
    var portsByFeature = ports.GroupBy(p => p.PortFeatureId);
    
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
                .Take(3) // Connect to 3 nearest
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
```

### 4. Optimize Routes

```csharp
private void OptimizeRoutes(List<Route> routes)
{
    // Remove redundant routes
    var toRemove = new HashSet<int>();
    
    for (int i = 0; i < routes.Count; i++)
    {
        if (toRemove.Contains(routes[i].Id)) continue;
        
        for (int j = i + 1; j < routes.Count; j++)
        {
            if (toRemove.Contains(routes[j].Id)) continue;
            
            // Check if routes are very similar
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
            }
            
            // Check path overlap
            var overlap = route1.Path.Intersect(route2.Path).Count();
            double overlapPercent = overlap / (double)Math.Min(route1.Path.Count, route2.Path.Count);
            
            if (overlapPercent > 0.8)
            {
                // Routes are very similar, keep one
                if (route1.Type == RouteType.Road && route2.Type == RouteType.Trail)
                    toRemove.Add(route2.Id);
                else if (route2.Type == RouteType.Road && route1.Type == RouteType.Trail)
                    toRemove.Add(route1.Id);
            }
        }
    }
    
    routes.RemoveAll(r => toRemove.Contains(r.Id));
}
```

## Implementation Steps

### Step 1: Models (Day 1)
- [x] Create `Route.cs` model
- [x] Create `RouteType.cs` enum
- [x] Update `MapData.cs`

### Step 2: Pathfinding (Day 2-3)
- [x] Create `RoutesGenerator.cs`
- [x] Implement A* pathfinding
- [x] Implement cost calculation

### Step 3: Road Generation (Day 4)
- [x] Implement `GenerateRoads()`
- [x] Connect burgs within states
- [x] Connect nearby burgs

### Step 4: Sea Routes (Day 5)
- [x] Implement `GenerateSeaRoutes()`
- [x] Connect ports on same water body

### Step 5: Optimization (Day 6)
- [x] Implement `OptimizeRoutes()`
- [x] Remove redundant routes
- [x] Merge overlapping routes

### Step 6: Integration & Testing (Day 7)
- [x] Add to `MapGenerator.cs`
- [x] Unit tests
- [x] Integration tests
- [x] Documentation

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Enable route generation
    /// </summary>
    public bool GenerateRoutes { get; set; } = true;
    
    /// <summary>
    /// Generate sea routes for naval powers
    /// </summary>
    public bool GenerateSeaRoutes { get; set; } = true;
    
    /// <summary>
    /// Maximum route length (cells)
    /// </summary>
    public int MaxRouteLength { get; set; } = 100;
}
```

## Success Criteria

- [x] Roads connect burgs within states
- [x] Sea routes connect ports
- [x] Routes follow terrain realistically
- [x] Redundant routes removed
- [x] All tests passing
- [x] Performance < 3 seconds

## Dependencies

**Required:**
- Burgs (006)
- States (007)

**Blocks:**
- None (optional feature)

## Notes

- Routes prefer existing paths (reuse)
- Rivers require bridges (cost penalty)
- Mountains are expensive to cross
- Sea routes only for ports
- A* ensures optimal paths

## References

- Original JS: `modules/routes-generator.js`
- Algorithm: A* pathfinding with terrain costs
