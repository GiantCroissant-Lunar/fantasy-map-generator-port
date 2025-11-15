namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Data;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

/// <summary>
/// Generates cultures and their territories
/// </summary>
public class CulturesGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public CulturesGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    /// <summary>
    /// Generate all cultures
    /// </summary>
    public List<Culture> Generate()
    {
        Console.WriteLine("Generating cultures...");

        // Select cultures from defaults
        var cultures = SelectCultures(_settings.CultureCount);
        Console.WriteLine($"Selected {cultures.Count - 1} cultures");

        // Place culture centers
        PlaceCultureCenters(cultures);
        Console.WriteLine("Placed culture centers");

        // Expand culture territories
        ExpandCultures(cultures);
        Console.WriteLine("Expanded culture territories");

        // Calculate statistics
        CollectStatistics(cultures);
        Console.WriteLine("Collected statistics");

        return cultures;
    }

    /// <summary>
    /// Select cultures from default sets
    /// </summary>
    private List<Culture> SelectCultures(int count)
    {
        var cultures = new List<Culture>();

        // Add locked cultures from previous generation
        if (_map.Cultures != null)
        {
            cultures.AddRange(_map.Cultures.Where(c => c.IsLocked && !c.IsRemoved));
        }

        // Get default cultures for selected set
        var defaults = DefaultCultures.GetCultureSet(_settings.CultureSet);

        // If we need exactly the default count, use all
        if (count == defaults.Count && cultures.Count == 0)
        {
            for (int i = 0; i < defaults.Count; i++)
            {
                var d = defaults[i];
                cultures.Add(new Culture
                {
                    Id = i + 1,
                    Name = d.Name,
                    NameBaseId = d.NameBaseId,
                    Shield = d.Shield,
                    Origins = new List<int> { 0 }
                });
            }
        }
        else
        {
            // Randomly select based on probability
            var available = new List<DefaultCulture>(defaults);

            while (cultures.Count < count && available.Any())
            {
                // Weighted random selection
                DefaultCulture? selected = null;
                int attempts = 0;

                do
                {
                    int index = _random.Next(available.Count);
                    selected = available[index];
                    attempts++;
                } while (attempts < 200 && !P(selected.Probability));

                if (selected != null)
                {
                    cultures.Add(new Culture
                    {
                        Id = cultures.Count + 1,
                        Name = selected.Name,
                        NameBaseId = selected.NameBaseId,
                        Shield = selected.Shield,
                        Origins = new List<int> { 0 }
                    });

                    available.Remove(selected);
                }
            }
        }

        // Add wildlands culture at index 0
        cultures.Insert(0, new Culture
        {
            Id = 0,
            Name = "Wildlands",
            NameBaseId = 1,
            Origins = new List<int>(),
            Shield = "round",
            Color = "#808080"
        });

        return cultures;
    }

    /// <summary>
    /// Place culture centers strategically
    /// </summary>
    private void PlaceCultureCenters(List<Culture> cultures)
    {
        var populated = _map.Cells
            .Where(c => c.Population > 0)
            .ToList();

        if (populated.Count < cultures.Count * 25)
        {
            Console.WriteLine($"Warning: Not enough populated cells for {cultures.Count} cultures");
        }

        var centerTree = new QuadTree(_map.Width, _map.Height);
        var colors = GenerateCultureColors(cultures.Count - 1);

        double spacing = (_map.Width + _map.Height) / 2.0 / cultures.Count;

        for (int i = 1; i < cultures.Count; i++)
        {
            var culture = cultures[i];

            if (culture.IsLocked)
            {
                // Keep existing center
                centerTree.Add(_map.Cells[culture.CenterCellId].Center, i);
                continue;
            }

            // Find suitable center location
            int centerCell = FindCultureCenter(populated, centerTree, spacing, culture);

            culture.CenterCellId = centerCell;
            culture.Color = colors[i - 1];
            culture.Type = DefineCultureType(centerCell);
            culture.Expansionism = DefineCultureExpansionism(culture.Type);
            culture.Code = GenerateCultureCode(culture.Name, cultures);

            var cell = _map.Cells[centerCell];
            cell.CultureId = i;

            centerTree.Add(cell.Center, i);
        }
    }

    /// <summary>
    /// Find suitable center location for culture
    /// </summary>
    private int FindCultureCenter(
        List<Cell> populated,
        QuadTree centerTree,
        double spacing,
        Culture culture)
    {
        const int MAX_ATTEMPTS = 100;

        // Sort cells by score
        var sorted = populated
            .OrderByDescending(c => GetCultureScore(c))
            .ToList();

        int max = Math.Min(sorted.Count / 2, sorted.Count);

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            // Biased random selection (favor higher scores)
            int index = (int)Math.Floor(Math.Pow(_random.NextDouble(), 5) * max);
            var cell = sorted[index];

            // Check spacing
            if (cell.CultureId == 0 &&
                centerTree.FindNearest(cell.Center, spacing) == null)
            {
                return cell.Id;
            }

            // Reduce spacing if struggling to place
            if (attempt % 20 == 19)
                spacing *= 0.9;
        }

        // Fallback: first available cell
        return sorted.First(c => c.CultureId == 0).Id;
    }

    /// <summary>
    /// Calculate culture score for cell
    /// </summary>
    private double GetCultureScore(Cell cell)
    {
        // Base score is population
        return cell.Population;
    }

    /// <summary>
    /// Generate distinct colors for cultures
    /// </summary>
    private List<string> GenerateCultureColors(int count)
    {
        var colors = new List<string>();

        for (int i = 0; i < count; i++)
        {
            // Generate colors with good saturation and brightness
            float hue = (i * 360f / count + _random.Next(0, 30)) % 360f;
            float saturation = 0.5f + (float)_random.NextDouble() * 0.3f;
            float lightness = 0.4f + (float)_random.NextDouble() * 0.3f;

            colors.Add(HSLToHex(hue, saturation, lightness));
        }

        return colors;
    }

    /// <summary>
    /// Convert HSL to hex color string
    /// </summary>
    private string HSLToHex(float h, float s, float l)
    {
        float c = (1 - Math.Abs(2 * l - 1)) * s;
        float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        float m = l - c / 2;

        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte rByte = (byte)((r + m) * 255);
        byte gByte = (byte)((g + m) * 255);
        byte bByte = (byte)((b + m) * 255);

        return $"#{rByte:X2}{gByte:X2}{bByte:X2}";
    }

    /// <summary>
    /// Define culture type based on geography
    /// </summary>
    private CultureType DefineCultureType(int centerCell)
    {
        var cell = _map.Cells[centerCell];

        // Nomadic: hot/dry biomes, avoid forests
        if (cell.Height < 70 && new[] { 1, 2, 3, 4 }.Contains(cell.BiomeId))
        {
            return CultureType.Nomadic;
        }

        // Highland: mountains
        if (cell.Height > 50)
        {
            return CultureType.Highland;
        }

        // Lake: large lake nearby
        if (cell.HavenCell.HasValue)
        {
            // Simplified: assume it's a lake if there's a haven cell
            return CultureType.Lake;
        }

        // Naval: coastal with good harbor
        if ((cell.Harbor > 0 && P(0.1)) ||
            (cell.Harbor == 1 && P(0.6)))
        {
            return CultureType.Naval;
        }

        // River: major river
        if (cell.RiverId > 0 && cell.Flux > 100)
        {
            return CultureType.River;
        }

        // Hunting: forest biomes
        if (cell.CoastDistance > 2 && new[] { 3, 7, 8, 9, 10, 12 }.Contains(cell.BiomeId))
        {
            return CultureType.Hunting;
        }

        return CultureType.Generic;
    }

    /// <summary>
    /// Define culture expansionism based on type
    /// </summary>
    private double DefineCultureExpansionism(CultureType type)
    {
        double baseExpansion = type switch
        {
            CultureType.Lake => 0.8,
            CultureType.Naval => 1.5,
            CultureType.River => 0.9,
            CultureType.Nomadic => 1.5,
            CultureType.Hunting => 0.7,
            CultureType.Highland => 1.2,
            _ => 1.0
        };

        // Add random variation
        double variation = _random.NextDouble() * _settings.SizeVariety / 2.0 + 1.0;

        return Math.Round(baseExpansion * variation, 1);
    }

    /// <summary>
    /// Generate 3-letter culture code
    /// </summary>
    private string GenerateCultureCode(string name, List<Culture> cultures)
    {
        // Remove parentheses and special characters
        string clean = new string(name.Where(c => char.IsLetter(c) || c == ' ').ToArray());
        
        // Try first 3 letters
        string code = clean.Length >= 3 ? clean.Substring(0, 3).ToUpperInvariant() : clean.ToUpperInvariant();

        // Ensure uniqueness
        int suffix = 1;
        string originalCode = code;
        while (cultures.Any(c => c.Code == code))
        {
            code = originalCode + suffix++;
        }

        return code;
    }

    /// <summary>
    /// Expand culture territories using Dijkstra algorithm
    /// </summary>
    private void ExpandCultures(List<Culture> cultures)
    {
        var queue = new PriorityQueue<CultureExpansionNode, double>();
        var costs = new Dictionary<int, double>();

        double maxCost = _map.Cells.Count * 0.6 * _settings.NeutralRate;

        // Initialize: add culture centers to queue
        foreach (var culture in cultures.Where(c => c.Id > 0 && !c.IsLocked))
        {
            var cell = _map.Cells[culture.CenterCellId];

            queue.Enqueue(
                new CultureExpansionNode(culture.CenterCellId, culture.Id, 0),
                0);
            costs[culture.CenterCellId] = 0;
        }

        // Expand using Dijkstra
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var cell = _map.Cells[node.CellId];
            var culture = cultures[node.CultureId];

            // Expand to neighbors
            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];

                // Skip if locked culture
                if (neighbor.CultureId > 0 && neighbor.CultureId < cultures.Count && 
                    cultures[neighbor.CultureId].IsLocked)
                    continue;

                // Calculate expansion cost
                double cost = CalculateCultureExpansionCost(
                    cell, neighbor, culture);

                double totalCost = node.Cost + cost;

                // Stop if too expensive
                if (totalCost > maxCost)
                    continue;

                // Update if better path found
                if (!costs.ContainsKey(neighborId) || totalCost < costs[neighborId])
                {
                    if (neighbor.Population > 0) // Only populated cells
                        neighbor.CultureId = node.CultureId;

                    costs[neighborId] = totalCost;
                    queue.Enqueue(
                        new CultureExpansionNode(neighborId, node.CultureId, totalCost),
                        totalCost);
                }
            }
        }
    }

    /// <summary>
    /// Calculate cost of culture expansion to a cell
    /// </summary>
    private double CalculateCultureExpansionCost(
        Cell from, Cell to, Culture culture)
    {
        var centerCell = _map.Cells[culture.CenterCellId];

        // Biome cost
        double biomeCost = GetCultureBiomeCost(
            centerCell.BiomeId, to.BiomeId, culture.Type);

        // Biome change penalty
        double biomeChangeCost = from.BiomeId == to.BiomeId ? 0 : 20;

        // Height cost
        double heightCost = GetCultureHeightCost(to, culture.Type);

        // River cost
        double riverCost = GetCultureRiverCost(to, culture.Type);

        // Type cost (coastline vs inland)
        double typeCost = GetCultureTypeCost(to, culture.Type);

        double totalCost = biomeCost + biomeChangeCost + heightCost +
                          riverCost + typeCost;

        return totalCost / culture.Expansionism;
    }

    private double GetCultureBiomeCost(int nativeBiome, int biome, CultureType type)
    {
        if (nativeBiome == biome) return 10; // Native biome bonus

        if (biome < 0 || biome >= _map.Biomes.Count)
            return 100;

        var biomeCost = 50; // Default cost

        if (type == CultureType.Hunting)
            return biomeCost * 5; // Hunters strongly prefer native biome

        if (type == CultureType.Nomadic && biome >= 5 && biome <= 9)
            return biomeCost * 10; // Nomads avoid forests

        return biomeCost * 2; // General non-native penalty
    }

    private double GetCultureHeightCost(Cell cell, CultureType type)
    {
        double area = 1.0; // Simplified area

        if (type == CultureType.Lake && cell.HavenCell.HasValue)
            return 10; // Lake cultures cross lakes easily

        if (type == CultureType.Naval && cell.Height < 20)
            return area * 2; // Naval cultures cross water

        if (type == CultureType.Nomadic && cell.Height < 20)
            return area * 50; // Nomads can't cross water

        if (cell.Height < 20)
            return area * 6; // General water crossing penalty

        if (type == CultureType.Highland && cell.Height < 44)
            return 3000; // Highlanders avoid lowlands

        if (type == CultureType.Highland && cell.Height < 62)
            return 200; // Highlanders prefer high ground

        if (type == CultureType.Highland)
            return 0; // No penalty for highlands

        if (cell.Height >= 67)
            return 200; // Mountain crossing

        if (cell.Height >= 44)
            return 30; // Hill crossing

        return 0;
    }

    private double GetCultureRiverCost(Cell cell, CultureType type)
    {
        if (type == CultureType.River)
            return cell.RiverId > 0 ? 0 : 100;

        if (cell.RiverId == 0)
            return 0;

        return Math.Clamp(cell.Flux / 10.0, 20, 100);
    }

    private double GetCultureTypeCost(Cell cell, CultureType type)
    {
        if (cell.CoastDistance == 1) // Coastline
        {
            if (type == CultureType.Naval || type == CultureType.Lake)
                return 0;
            if (type == CultureType.Nomadic)
                return 60;
            return 20;
        }

        if (cell.CoastDistance == 2) // Near coast
        {
            if (type == CultureType.Naval || type == CultureType.Nomadic)
                return 30;
            return 0;
        }

        if (cell.CoastDistance > 2) // Inland
        {
            if (type == CultureType.Naval || type == CultureType.Lake)
                return 100;
            return 0;
        }

        return 0;
    }

    /// <summary>
    /// Collect statistics for all cultures
    /// </summary>
    private void CollectStatistics(List<Culture> cultures)
    {
        // Reset statistics
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            culture.CellCount = 0;
            culture.Area = 0;
            culture.RuralPopulation = 0;
            culture.UrbanPopulation = 0;
        }

        // Count cells and calculate area
        foreach (var cell in _map.Cells.Where(c => c.Population > 0))
        {
            if (cell.CultureId <= 0 || cell.CultureId >= cultures.Count) continue;

            var culture = cultures[cell.CultureId];
            culture.CellCount++;
            culture.Area += 1.0; // Simplified area calculation
            culture.RuralPopulation += cell.Population;
        }

        // Count urban population from burgs
        if (_map.Burgs != null)
        {
            foreach (var burg in _map.Burgs.Where(b => b != null))
            {
                if (burg.CultureId <= 0 || burg.CultureId >= cultures.Count) continue;

                var culture = cultures[burg.CultureId];
                culture.UrbanPopulation += burg.Population;
            }
        }
    }

    /// <summary>
    /// Probability helper
    /// </summary>
    private bool P(double probability) => _random.NextDouble() < probability;

    private record CultureExpansionNode(int CellId, int CultureId, double Cost);
}
