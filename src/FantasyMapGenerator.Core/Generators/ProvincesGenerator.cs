namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

public class ProvincesGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public ProvincesGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    public List<Province> Generate()
    {
        Console.WriteLine("Generating provinces...");

        var provinces = CreateProvinces(_map.States, _map.Burgs);
        Console.WriteLine($"Created {provinces.Count - 1} provinces");

        ExpandProvinces(provinces);
        Console.WriteLine("Expanded provinces");

        NormalizeProvinces(provinces);
        Console.WriteLine("Normalized province borders");

        CollectStatistics(provinces);
        Console.WriteLine("Collected statistics");

        return provinces;
    }

    private List<Province> CreateProvinces(List<State> states, List<Burg> burgs)
    {
        var provinces = new List<Province>
        {
            new Province { Id = 0, Name = "No Province" }
        };

        int provinceId = 1;

        foreach (var state in states.Where(s => s.Id > 0))
        {
            // Get burgs in this state
            var stateBurgs = burgs
                .Where(b => b != null && b.StateId == state.Id)
                .OrderByDescending(b => b.Population)
                .ToList();

            if (!stateBurgs.Any()) continue;

            // Determine number of provinces
            int provinceCount = CalculateProvinceCount(state, stateBurgs.Count);

            // Select province capitals
            var capitals = stateBurgs.Take(provinceCount).ToList();

            foreach (var capital in capitals)
            {
                var province = new Province
                {
                    Id = provinceId++,
                    Name = GenerateProvinceName(capital, state),
                    StateId = state.Id,
                    CapitalBurgId = capital.Id,
                    CenterCellId = capital.CellId,
                    Color = GenerateProvinceColor(state.Color)
                };

                provinces.Add(province);
            }
        }

        return provinces;
    }

    private int CalculateProvinceCount(State state, int burgCount)
    {
        // Base on state size and burg count
        int baseCount = Math.Max(1, burgCount / _settings.MinBurgsPerProvince);
        int areaCount = (int)(state.Area / 10000); // 1 province per 10k km²

        return Math.Clamp(Math.Max(baseCount, areaCount), 1, burgCount);
    }

    private string GenerateProvinceName(Burg capital, State state)
    {
        // Use capital name as base
        return $"{capital.Name} Province";
    }

    private string GenerateProvinceColor(string stateColor)
    {
        // Parse state color and create a variant
        if (stateColor.StartsWith("#") && stateColor.Length == 7)
        {
            int r = Convert.ToInt32(stateColor.Substring(1, 2), 16);
            int g = Convert.ToInt32(stateColor.Substring(3, 2), 16);
            int b = Convert.ToInt32(stateColor.Substring(5, 2), 16);

            // Lighten or darken slightly
            int variation = _random.Next(-30, 30);
            r = Math.Clamp(r + variation, 0, 255);
            g = Math.Clamp(g + variation, 0, 255);
            b = Math.Clamp(b + variation, 0, 255);

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        return stateColor;
    }

    private void ExpandProvinces(List<Province> provinces)
    {
        var queue = new PriorityQueue<ProvinceExpansionNode, double>();
        var costs = new Dictionary<int, double>();

        // Initialize with province capitals
        foreach (var province in provinces.Skip(1))
        {
            var cell = _map.Cells[province.CenterCellId];
            cell.ProvinceId = province.Id;
            
            queue.Enqueue(
                new ProvinceExpansionNode(province.CenterCellId, province.Id, 0),
                0);
            costs[province.CenterCellId] = 0;
        }

        // Expand using Dijkstra
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var cell = _map.Cells[node.CellId];
            var province = provinces[node.ProvinceId];

            // Only expand within same state
            if (cell.StateId != province.StateId)
                continue;

            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];

                // Must be same state
                if (neighbor.StateId != province.StateId)
                    continue;

                // Skip if already assigned to another province capital
                if (neighbor.Burg > 0 &&
                    _map.Burgs[neighbor.Burg].Id != province.CapitalBurgId &&
                    provinces.Any(p => p.CapitalBurgId == neighbor.Burg))
                    continue;

                double cost = CalculateProvinceCost(cell, neighbor);
                double totalCost = node.Cost + cost;

                if (!costs.ContainsKey(neighborId) || totalCost < costs[neighborId])
                {
                    neighbor.ProvinceId = node.ProvinceId;
                    costs[neighborId] = totalCost;
                    queue.Enqueue(
                        new ProvinceExpansionNode(neighborId, node.ProvinceId, totalCost),
                        totalCost);
                }
            }
        }
    }

    private double CalculateProvinceCost(Cell from, Cell to)
    {
        // Simple distance-based cost
        double heightDiff = Math.Abs(to.Height - from.Height);
        double riverCost = to.RiverId > 0 ? 5 : 0;

        return 10 + heightDiff * 0.5 + riverCost;
    }

    private void NormalizeProvinces(List<Province> provinces)
    {
        // Reassign cells surrounded by different province
        foreach (var cell in _map.Cells.Where(c => c.Height >= 20 && c.ProvinceId > 0))
        {
            if (cell.Burg > 0) continue;

            var neighborProvinces = cell.Neighbors
                .Select(nId => _map.Cells[nId].ProvinceId)
                .Where(pId => pId > 0 && pId != cell.ProvinceId)
                .ToList();

            if (neighborProvinces.Count >= 3)
            {
                // Surrounded by different province
                var mostCommon = neighborProvinces
                    .GroupBy(p => p)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                cell.ProvinceId = mostCommon;
            }
        }
    }

    private void CollectStatistics(List<Province> provinces)
    {
        foreach (var province in provinces.Where(p => p.Id > 0))
        {
            province.CellCount = 0;
            province.Area = 0;
            province.RuralPopulation = 0;
            province.UrbanPopulation = 0;
            province.BurgCount = 0;
        }

        foreach (var cell in _map.Cells.Where(c => c.Population > 0))
        {
            if (cell.ProvinceId <= 0 || cell.ProvinceId >= provinces.Count) continue;

            var province = provinces[cell.ProvinceId];
            province.CellCount++;
            province.Area += 1.0;
            province.RuralPopulation += cell.Population;
        }

        foreach (var burg in _map.Burgs.Where(b => b != null))
        {
            var cell = _map.Cells[burg.CellId];
            if (cell.ProvinceId <= 0 || cell.ProvinceId >= provinces.Count) continue;

            var province = provinces[cell.ProvinceId];
            province.UrbanPopulation += burg.Population;
            province.BurgCount++;
        }
    }

    private record ProvinceExpansionNode(int CellId, int ProvinceId, double Cost);
}
