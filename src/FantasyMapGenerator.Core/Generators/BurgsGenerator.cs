namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

/// <summary>
/// Generates settlements (burgs) on the map
/// </summary>
public class BurgsGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public BurgsGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    /// <summary>
    /// Generate all burgs (capitals and towns)
    /// </summary>
    public List<Burg> Generate()
    {
        Console.WriteLine("Generating burgs...");

        // Start with null at index 0 (reserved)
        var burgs = new List<Burg> { null! };

        // Place capitals first
        burgs = PlaceCapitals(_settings.NumStates);
        Console.WriteLine($"Placed {burgs.Count - 1} capitals");

        // Calculate town count if not specified
        int townCount = _settings.NumBurgs > 0 
            ? _settings.NumBurgs 
            : Math.Max(_map.Cells.Count / 50, 10);

        // Place towns
        PlaceTowns(burgs, townCount);
        Console.WriteLine($"Total burgs: {burgs.Count - 1}");

        // Specify burg properties (ports, population, type)
        SpecifyBurgs(burgs);

        // Define burg features (citadel, walls, etc.)
        DefineBurgFeatures(burgs);

        // Generate names
        GenerateNames(burgs);

        return burgs;
    }

    /// <summary>
    /// Place one capital per state with optimal spacing
    /// </summary>
    private List<Burg> PlaceCapitals(int stateCount)
    {
        var burgs = new List<Burg> { null! }; // Index 0 is reserved

        // Calculate cell scores for capital placement
        var scores = new int[_map.Cells.Count];
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            if (cell.Population == 0 || cell.Culture == -1)
            {
                scores[i] = 0;
                continue;
            }

            // Score = population * random factor (0.5-1.0)
            scores[i] = (int)(cell.Population * (0.5 + _random.NextDouble() * 0.5));
        }

        // Sort cells by score (highest first)
        var sortedCells = Enumerable.Range(0, _map.Cells.Count)
            .Where(i => scores[i] > 0)
            .OrderByDescending(i => scores[i])
            .ToList();

        if (sortedCells.Count < stateCount * 10)
        {
            Console.WriteLine($"Warning: Not enough populated cells for {stateCount} states");
            stateCount = Math.Max(sortedCells.Count / 10, 1);
        }

        // Use quadtree for spacing
        var burgTree = new QuadTree(_map.Width, _map.Height);
        double spacing = (_map.Width + _map.Height) / 2.0 / stateCount;

        int attempts = 0;
        while (burgs.Count <= stateCount && attempts < sortedCells.Count)
        {
            var cellId = sortedCells[attempts++];
            var cell = _map.Cells[cellId];

            // Check if too close to existing capitals
            if (burgTree.FindNearest(cell.Center, spacing) != null)
                continue;

            // Place capital
            var burg = new Burg
            {
                Id = burgs.Count,
                CellId = cellId,
                Position = cell.Center,
                IsCapital = true,
                CultureId = cell.Culture,
                FeatureId = cell.Feature
            };

            burgs.Add(burg);
            burgTree.Add(cell.Center, burg.Id);
            cell.Burg = burg.Id;

            // If we've tried all cells, reduce spacing
            if (attempts == sortedCells.Count - 1 && burgs.Count <= stateCount)
            {
                spacing *= 0.8;
                attempts = 0;
            }
        }

        return burgs;
    }

    /// <summary>
    /// Place secondary settlements based on population
    /// </summary>
    private void PlaceTowns(List<Burg> burgs, int targetCount)
    {
        // Calculate cell scores with randomization
        var scores = new int[_map.Cells.Count];
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            if (cell.Burg > 0 || cell.Population == 0 || cell.Culture == -1)
            {
                scores[i] = 0;
                continue;
            }

            // Score with Gaussian randomization
            scores[i] = (int)(cell.Population * Gauss(1, 3, 0, 20, 3));
        }

        var sortedCells = Enumerable.Range(0, _map.Cells.Count)
            .Where(i => scores[i] > 0)
            .OrderByDescending(i => scores[i])
            .ToList();

        var burgTree = new QuadTree(_map.Width, _map.Height);

        // Add existing capitals to tree
        foreach (var burg in burgs.Where(b => b != null))
        {
            burgTree.Add(burg.Position, burg.Id);
        }

        double spacing = (_map.Width + _map.Height) / 150.0 / Math.Pow(targetCount, 0.7) / 66.0;
        int placed = 0;

        while (placed < targetCount && spacing > 1)
        {
            foreach (var cellId in sortedCells)
            {
                if (placed >= targetCount) break;

                var cell = _map.Cells[cellId];
                if (cell.Burg > 0) continue;

                // Randomize spacing slightly
                double s = spacing * Gauss(1, 0.3, 0.2, 2, 2);

                if (burgTree.FindNearest(cell.Center, s) != null)
                    continue;

                // Place town
                var burg = new Burg
                {
                    Id = burgs.Count,
                    CellId = cellId,
                    Position = cell.Center,
                    IsCapital = false,
                    CultureId = cell.Culture,
                    FeatureId = cell.Feature
                };

                burgs.Add(burg);
                burgTree.Add(cell.Center, burg.Id);
                cell.Burg = burg.Id;
                placed++;
            }

            spacing *= 0.5; // Reduce spacing if we can't place enough
        }

        Console.WriteLine($"Placed {placed} towns (target: {targetCount})");
    }

    /// <summary>
    /// Specify burg properties (ports, population, type)
    /// </summary>
    private void SpecifyBurgs(List<Burg> burgs)
    {
        foreach (var burg in burgs.Where(b => b != null))
        {
            var cell = _map.Cells[burg.CellId];

            // 1. Detect ports
            if (cell.HavenCell.HasValue)
            {
                var havenCell = _map.Cells[cell.HavenCell.Value];
                
                // Port if: capital with any harbor OR town with good harbor
                bool isPort = (burg.IsCapital && cell.Harbor > 0) || cell.Harbor == 1;

                if (isPort)
                {
                    burg.IsPort = true;
                    burg.PortFeatureId = havenCell.Feature;

                    // Move position closer to water edge (simple approximation)
                    double dx = havenCell.Center.X - cell.Center.X;
                    double dy = havenCell.Center.Y - cell.Center.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > 0)
                    {
                        burg.Position = new Point(
                            cell.Center.X + dx * 0.3 / dist,
                            cell.Center.Y + dy * 0.3 / dist
                        );
                    }
                }
            }

            // 2. Calculate population
            double basePop = Math.Max(cell.Population / 8.0 + burg.Id / 1000.0, 0.1);

            if (burg.IsCapital)
                basePop *= 1.3; // Capitals are larger

            if (burg.IsPort)
                basePop *= 1.3; // Ports are larger

            // Add random variation
            burg.Population = basePop * Gauss(2, 3, 0.6, 20, 3);

            // 3. Shift river burgs slightly
            if (!burg.IsPort && cell.RiverId > 0)
            {
                double shift = Math.Min(cell.Flux / 150.0, 1.0);
                if (cell.Id % 2 == 0)
                    burg.Position = new Point(burg.Position.X + shift, burg.Position.Y);
                else
                    burg.Position = new Point(burg.Position.X - shift, burg.Position.Y);
            }

            // 4. Determine type
            burg.Type = GetBurgType(cell, burg.IsPort);
        }
    }

    /// <summary>
    /// Determine burg type based on location
    /// </summary>
    private BurgType GetBurgType(Cell cell, bool isPort)
    {
        if (isPort) return BurgType.Naval;

        // Lake
        if (cell.HavenCell.HasValue)
        {
            return BurgType.Lake;
        }

        // Highland
        if (cell.Height > 60)
            return BurgType.Highland;

        // River
        if (cell.RiverId > 0 && cell.Flux >= 100)
            return BurgType.River;

        // Nomadic (desert/steppe with low population)
        if (cell.Population <= 5 && IsDesertOrSteppe(cell.Biome))
            return BurgType.Nomadic;

        // Hunting (forest)
        if (cell.Biome >= 5 && cell.Biome <= 9)
            return BurgType.Hunting;

        return BurgType.Generic;
    }

    /// <summary>
    /// Check if biome is desert or steppe
    /// </summary>
    private bool IsDesertOrSteppe(int biomeId)
    {
        // Biome IDs: 0=Marine, 1=Hot Desert, 2=Cold Desert, 3=Savanna, 4=Grassland/Steppe
        return biomeId == 1 || biomeId == 2 || biomeId == 4;
    }

    /// <summary>
    /// Define burg features (citadel, walls, plaza, etc.)
    /// </summary>
    private void DefineBurgFeatures(List<Burg> burgs)
    {
        foreach (var burg in burgs.Where(b => b != null))
        {
            double pop = burg.Population;

            // Citadel (fortress)
            burg.HasCitadel = burg.IsCapital ||
                             (pop > 50 && P(0.75)) ||
                             (pop > 15 && P(0.5)) ||
                             P(0.1);

            // Plaza (central square)
            burg.HasPlaza = pop > 20 ||
                           (pop > 10 && P(0.8)) ||
                           (pop > 4 && P(0.7)) ||
                           P(0.6);

            // Walls
            burg.HasWalls = burg.IsCapital ||
                           pop > 30 ||
                           (pop > 20 && P(0.75)) ||
                           (pop > 10 && P(0.5)) ||
                           P(0.1);

            // Shantytown (slums)
            burg.HasShanty = pop > 60 ||
                            (pop > 40 && P(0.75)) ||
                            (pop > 20 && burg.HasWalls && P(0.4));

            // Temple
            burg.HasTemple = P(0.5) ||
                            pop > 50 ||
                            (pop > 35 && P(0.75)) ||
                            (pop > 20 && P(0.5));
        }
    }

    /// <summary>
    /// Generate names for burgs
    /// </summary>
    private void GenerateNames(List<Burg> burgs)
    {
        // Simple name generation for now
        // TODO: Implement culture-based name generation
        foreach (var burg in burgs.Where(b => b != null))
        {
            if (string.IsNullOrEmpty(burg.Name))
            {
                string prefix = burg.IsCapital ? "Capital" : "Town";
                burg.Name = $"{prefix}{burg.Id}";
            }
        }
    }

    /// <summary>
    /// Gaussian distribution helper
    /// </summary>
    private double Gauss(double mean, double deviation, double min, double max, double skew)
    {
        double u = 0, v = 0;
        while (u == 0) u = _random.NextDouble();
        while (v == 0) v = _random.NextDouble();

        double num = Math.Sqrt(-2.0 * Math.Log(u)) * Math.Cos(2.0 * Math.PI * v);
        num = num * deviation + mean;

        if (skew != 0)
        {
            num = Math.Pow(num, skew);
        }

        return Math.Max(min, Math.Min(max, num));
    }

    /// <summary>
    /// Probability helper
    /// </summary>
    private bool P(double probability) => _random.NextDouble() < probability;
}
