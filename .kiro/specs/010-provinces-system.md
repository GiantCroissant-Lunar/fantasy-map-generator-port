# Spec 010: Provinces System

## Status
- **State:** Not Started
- **Priority:** ⭐⭐⭐ Medium
- **Estimated Effort:** 1 week
- **Dependencies:** States (007), Burgs (006)
- **Blocks:** None (optional feature)

## Overview

Implement the province system that divides states into administrative regions. Provinces provide sub-state organization, each with a capital burg and defined borders.

## Goals

1. **Province Creation** - Divide states into provinces
2. **Province Capitals** - Assign burgs as provincial capitals
3. **Province Expansion** - Assign cells to provinces
4. **Province Borders** - Generate province boundaries
5. **Statistics** - Calculate area, population, and burgs per province

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/provinces-generator.js`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class Province
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StateId { get; set; }
    public int CapitalBurgId { get; set; }
    public int CenterCellId { get; set; }
    public SKColor Color { get; set; }
    
    // Statistics
    public int CellCount { get; set; }
    public double Area { get; set; }
    public double RuralPopulation { get; set; }
    public double UrbanPopulation { get; set; }
    public int BurgCount { get; set; }
}
```

### Cell Extensions

```csharp
// Add to Cell.cs
public class Cell
{
    // ... existing properties ...
    
    /// <summary>
    /// Province ID this cell belongs to (0 = none)
    /// </summary>
    public int ProvinceId { get; set; }
}
```

## Algorithm

### 1. Create Provinces

```csharp
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
    int baseCount = Math.Max(1, burgCount / 3);
    int areaCount = (int)(state.Area / 10000); // 1 province per 10k km²
    
    return Math.Clamp(Math.Max(baseCount, areaCount), 1, burgCount);
}
```

### 2. Expand Provinces

```csharp
private void ExpandProvinces(List<Province> provinces)
{
    var queue = new PriorityQueue<ProvinceExpansionNode, double>();
    var costs = new Dictionary<int, double>();
    
    // Initialize with province capitals
    foreach (var province in provinces.Skip(1))
    {
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
            if (neighbor.BurgId > 0 && 
                _map.Burgs[neighbor.BurgId].Id != province.CapitalBurgId &&
                provinces.Any(p => p.CapitalBurgId == neighbor.BurgId))
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

private record ProvinceExpansionNode(int CellId, int ProvinceId, double Cost);
```

### 3. Normalize Province Borders

```csharp
private void NormalizeProvinces(List<Province> provinces)
{
    // Similar to state normalization
    // Reassign cells surrounded by different province
    
    foreach (var cell in _map.Cells.Where(c => c.Height >= 20 && c.ProvinceId > 0))
    {
        if (cell.BurgId > 0) continue;
        
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
```

## Implementation Steps

### Step 1: Models (Day 1)
- [ ] Create `Province.cs` model
- [ ] Add `ProvinceId` to `Cell.cs`
- [ ] Update `MapData.cs`

### Step 2: Generator (Day 2-3)
- [ ] Create `ProvincesGenerator.cs`
- [ ] Implement `CreateProvinces()`
- [ ] Implement `ExpandProvinces()`

### Step 3: Borders & Stats (Day 4-5)
- [ ] Implement `NormalizeProvinces()`
- [ ] Implement `CollectStatistics()`
- [ ] Generate province borders

### Step 4: Integration & Testing (Day 6-7)
- [ ] Add to `MapGenerator.cs`
- [ ] Unit tests
- [ ] Integration tests
- [ ] Documentation

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Enable province generation
    /// </summary>
    public bool GenerateProvinces { get; set; } = true;
    
    /// <summary>
    /// Minimum burgs per province
    /// </summary>
    public int MinBurgsPerProvince { get; set; } = 3;
}
```

## Success Criteria

- [ ] Provinces created for each state
- [ ] Province capitals assigned
- [ ] Provinces expanded within state borders
- [ ] Province borders normalized
- [ ] All tests passing
- [ ] Performance < 2 seconds

## Dependencies

**Required:**
- States (007)
- Burgs (006)

**Blocks:**
- None (optional feature)

## References

- Original JS: `modules/provinces-generator.js`
