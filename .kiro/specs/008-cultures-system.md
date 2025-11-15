# Spec 008: Cultures System

## Status
- **State:** Not Started
- **Priority:** ⭐⭐⭐⭐⭐ Critical (Foundation for States and Burgs)
- **Estimated Effort:** 2 weeks
- **Dependencies:** None (can start immediately)
- **Blocks:** States (007), Burgs naming (006)

## Overview

Implement the culture system that defines how different peoples spread across the map, their characteristics, and naming conventions. Cultures are the foundation of political and social organization.

## Goals

1. **Culture Selection** - Choose cultures from defaults or generate random
2. **Culture Placement** - Place culture centers strategically
3. **Culture Types** - Classify by geography (Naval, Highland, River, etc.)
4. **Culture Expansion** - Spread cultures using Dijkstra algorithm
5. **Name Bases** - Assign linguistic bases for naming
6. **Statistics** - Calculate area, population, and demographics

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/cultures-generator.js`
- Lines 1-150: Culture selection and placement
- Lines 150-300: Culture expansion algorithm
- Lines 300-500: Default culture sets

## Data Models

### Culture Model

```csharp
namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a cultural group with distinct characteristics and naming
/// </summary>
public class Culture
{
    /// <summary>
    /// Unique identifier (0 = wildlands/no culture)
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Culture name (e.g., "Angshire", "Norse", "Eldar")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 3-letter abbreviation code
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Culture color for map display
    /// </summary>
    public SKColor Color { get; set; }
    
    // Geography
    
    /// <summary>
    /// Origin cell where culture started
    /// </summary>
    public int CenterCellId { get; set; }
    
    /// <summary>
    /// Culture type based on geography
    /// </summary>
    public CultureType Type { get; set; }
    
    /// <summary>
    /// Expansionism factor (0.5-2.0, affects spread rate)
    /// </summary>
    public double Expansionism { get; set; }
    
    // Language & Identity
    
    /// <summary>
    /// Name base ID for linguistic patterns
    /// </summary>
    public int NameBaseId { get; set; }
    
    /// <summary>
    /// Heraldic shield shape (heater, wedged, round, etc.)
    /// </summary>
    public string Shield { get; set; } = "heater";
    
    /// <summary>
    /// Parent culture IDs (for cultural evolution)
    /// </summary>
    public List<int> Origins { get; set; } = new();
    
    // Statistics
    
    /// <summary>
    /// Number of cells with this culture
    /// </summary>
    public int CellCount { get; set; }
    
    /// <summary>
    /// Total area in square kilometers
    /// </summary>
    public double Area { get; set; }
    
    /// <summary>
    /// Rural population (thousands)
    /// </summary>
    public double RuralPopulation { get; set; }
    
    /// <summary>
    /// Urban population (thousands)
    /// </summary>
    public double UrbanPopulation { get; set; }
    
    /// <summary>
    /// True if culture is locked (won't be modified)
    /// </summary>
    public bool IsLocked { get; set; }
}

/// <summary>
/// Culture type based on geography and lifestyle
/// </summary>
public enum CultureType
{
    /// <summary>
    /// Generic inland culture
    /// </summary>
    Generic,
    
    /// <summary>
    /// Seafaring coastal culture (low water crossing penalty)
    /// </summary>
    Naval,
    
    /// <summary>
    /// Lake-dwelling culture (lake crossing bonus)
    /// </summary>
    Lake,
    
    /// <summary>
    /// Mountain-dwelling culture (highland bonus)
    /// </summary>
    Highland,
    
    /// <summary>
    /// River-focused culture (river bonus)
    /// </summary>
    River,
    
    /// <summary>
    /// Desert/steppe nomadic culture (avoid forests)
    /// </summary>
    Nomadic,
    
    /// <summary>
    /// Forest hunting culture (forest bonus)
    /// </summary>
    Hunting
}

/// <summary>
/// Default culture definition
/// </summary>
public class DefaultCulture
{
    public string Name { get; set; } = string.Empty;
    public int NameBaseId { get; set; }
    public double Probability { get; set; } = 1.0; // Odd of being selected
    public Func<Cell, double>? SortingFunction { get; set; } // Cell scoring
    public string Shield { get; set; } = "heater";
}
```

### Cell Extensions

```csharp
// Add to Cell.cs
public class Cell
{
    // ... existing properties ...
    
    /// <summary>
    /// Culture ID for this cell (0 = wildlands)
    /// </summary>
    public int CultureId { get; set; }
}
```

## Algorithm

### 1. Select Cultures

```csharp
private List<Culture> SelectCultures(int count)
{
    var cultures = new List<Culture>();
    
    // Add locked cultures from previous generation
    if (_map.Cultures != null)
    {
        cultures.AddRange(_map.Cultures.Where(c => c.IsLocked && !c.IsRemoved));
    }
    
    // Get default cultures for selected set
    var defaults = GetDefaultCultures(_settings.CultureSet);
    
    // If we need exactly the default count, use all
    if (count == defaults.Count && cultures.Count == 0)
    {
        return defaults.Select((d, i) => new Culture
        {
            Id = i + 1,
            Name = d.Name,
            NameBaseId = d.NameBaseId,
            Shield = d.Shield,
            Origins = new List<int> { 0 }
        }).ToList();
    }
    
    // Otherwise, randomly select based on probability
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
    
    // Add wildlands culture at index 0
    cultures.Insert(0, new Culture
    {
        Id = 0,
        Name = "Wildlands",
        NameBaseId = 1,
        Origins = new List<int>(),
        Shield = "round"
    });
    
    return cultures;
}
```

### 2. Place Culture Centers

```csharp
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

private int FindCultureCenter(
    List<Cell> populated, 
    QuadTree centerTree, 
    double spacing,
    Culture culture)
{
    const int MAX_ATTEMPTS = 100;
    
    // Sort cells by score (culture-specific if available)
    var sorted = populated
        .OrderByDescending(c => GetCultureScore(c, culture))
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

private double GetCultureScore(Cell cell, Culture culture)
{
    // Base score is population
    double score = cell.Population;
    
    // Apply culture-specific modifiers
    // (This would use culture.SortingFunction if available)
    
    return score;
}
```

### 3. Define Culture Type

```csharp
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
        var haven = _map.Cells[cell.HavenCell.Value];
        var feature = _map.Features[haven.FeatureId];
        if (feature.Type == FeatureType.Lake && feature.Cells.Count > 5)
        {
            return CultureType.Lake;
        }
    }
    
    // Naval: coastal with good harbor
    if (cell.Harbor > 0 && P(0.1) || 
        cell.Harbor == 1 && P(0.6) ||
        _map.Features[cell.FeatureId].Group == "isle" && P(0.4))
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
```

### 4. Expand Cultures (Dijkstra)

```csharp
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
            if (neighbor.CultureId > 0 && cultures[neighbor.CultureId].IsLocked)
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
    
    var biomeCost = _map.Biomes[biome].Cost;
    
    if (type == CultureType.Hunting)
        return biomeCost * 5; // Hunters strongly prefer native biome
    
    if (type == CultureType.Nomadic && biome >= 5 && biome <= 9)
        return biomeCost * 10; // Nomads avoid forests
    
    return biomeCost * 2; // General non-native penalty
}

private double GetCultureHeightCost(Cell cell, CultureType type)
{
    var feature = _map.Features[cell.FeatureId];
    double area = cell.Area;
    
    if (type == CultureType.Lake && feature.Type == FeatureType.Lake)
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

private record CultureExpansionNode(int CellId, int CultureId, double Cost);
```

### 5. Default Culture Sets

```csharp
public static class DefaultCultures
{
    public static List<DefaultCulture> GetEuropean() => new()
    {
        new() { Name = "Shwazen", NameBaseId = 0, Shield = "swiss" },
        new() { Name = "Angshire", NameBaseId = 1, Shield = "wedged" },
        new() { Name = "Luari", NameBaseId = 2, Shield = "french" },
        new() { Name = "Tallian", NameBaseId = 3, Shield = "horsehead" },
        new() { Name = "Astellian", NameBaseId = 4, Shield = "spanish" },
        new() { Name = "Slovan", NameBaseId = 5, Shield = "polish" },
        new() { Name = "Norse", NameBaseId = 6, Shield = "heater" },
        new() { Name = "Elladan", NameBaseId = 7, Shield = "boeotian" },
        new() { Name = "Romian", NameBaseId = 8, Shield = "roman" },
        new() { Name = "Soumi", NameBaseId = 9, Shield = "pavise" },
        new() { Name = "Portuzian", NameBaseId = 13, Shield = "renaissance" },
        new() { Name = "Vengrian", NameBaseId = 15, Shield = "horsehead2" },
        new() { Name = "Turchian", NameBaseId = 16, Probability = 0.05, Shield = "round" },
        new() { Name = "Euskati", NameBaseId = 20, Probability = 0.05, Shield = "oldFrench" },
        new() { Name = "Keltan", NameBaseId = 22, Probability = 0.05, Shield = "oval" }
    };
    
    public static List<DefaultCulture> GetOriental() => new()
    {
        new() { Name = "Koryo", NameBaseId = 10, Shield = "round" },
        new() { Name = "Hantzu", NameBaseId = 11, Shield = "banner" },
        new() { Name = "Yamoto", NameBaseId = 12, Shield = "round" },
        new() { Name = "Turchian", NameBaseId = 16, Shield = "round" },
        new() { Name = "Berberan", NameBaseId = 17, Probability = 0.2, Shield = "oval" },
        new() { Name = "Eurabic", NameBaseId = 18, Shield = "oval" },
        new() { Name = "Efratic", NameBaseId = 23, Probability = 0.1, Shield = "round" },
        new() { Name = "Tehrani", NameBaseId = 24, Shield = "round" },
        new() { Name = "Maui", NameBaseId = 25, Probability = 0.2, Shield = "vesicaPiscis" },
        new() { Name = "Carnatic", NameBaseId = 26, Probability = 0.5, Shield = "round" },
        new() { Name = "Vietic", NameBaseId = 29, Probability = 0.8, Shield = "banner" },
        new() { Name = "Guantzu", NameBaseId = 30, Probability = 0.5, Shield = "banner" },
        new() { Name = "Ulus", NameBaseId = 31, Shield = "banner" }
    };
    
    public static List<DefaultCulture> GetHighFantasy() => new()
    {
        new() { Name = "Quenian (Elfish)", NameBaseId = 33, Shield = "gondor" },
        new() { Name = "Eldar (Elfish)", NameBaseId = 33, Shield = "noldor" },
        new() { Name = "Trow (Dark Elfish)", NameBaseId = 34, Probability = 0.9, Shield = "hessen" },
        new() { Name = "Lothian (Dark Elfish)", NameBaseId = 34, Probability = 0.3, Shield = "wedged" },
        new() { Name = "Dunirr (Dwarven)", NameBaseId = 35, Shield = "ironHills" },
        new() { Name = "Khazadur (Dwarven)", NameBaseId = 35, Shield = "erebor" },
        new() { Name = "Kobold (Goblin)", NameBaseId = 36, Shield = "moriaOrc" },
        new() { Name = "Uruk (Orkish)", NameBaseId = 37, Shield = "urukHai" },
        new() { Name = "Ugluk (Orkish)", NameBaseId = 37, Probability = 0.5, Shield = "moriaOrc" },
        new() { Name = "Yotunn (Giants)", NameBaseId = 38, Probability = 0.7, Shield = "pavise" },
        new() { Name = "Rake (Drakonic)", NameBaseId = 39, Probability = 0.7, Shield = "fantasy2" },
        new() { Name = "Arago (Arachnid)", NameBaseId = 40, Probability = 0.7, Shield = "horsehead2" },
        new() { Name = "Aj'Snaga (Serpents)", NameBaseId = 41, Probability = 0.7, Shield = "fantasy1" },
        new() { Name = "Anor (Human)", NameBaseId = 32, Shield = "fantasy5" },
        new() { Name = "Dail (Human)", NameBaseId = 32, Shield = "roman" },
        new() { Name = "Rohand (Human)", NameBaseId = 16, Shield = "round" },
        new() { Name = "Dulandir (Human)", NameBaseId = 31, Shield = "easterling" }
    };
}
```

## Implementation Steps

### Step 1: Create Models (Day 1)
- [ ] Update `Culture.cs` model with all properties
- [ ] Create `CultureType.cs` enum
- [ ] Create `DefaultCulture.cs` class
- [ ] Add `CultureId` to `Cell.cs`

### Step 2: Create Generator (Day 2-4)
- [ ] Create `CulturesGenerator.cs`
- [ ] Implement `SelectCultures()`
- [ ] Implement `PlaceCultureCenters()`
- [ ] Implement `DefineCultureType()`
- [ ] Implement `DefineCultureExpansionism()`

### Step 3: Expansion Algorithm (Day 5-7)
- [ ] Implement `ExpandCultures()` with Dijkstra
- [ ] Implement cost calculation methods
- [ ] Implement culture-specific modifiers

### Step 4: Default Culture Sets (Day 8)
- [ ] Create `DefaultCultures.cs` static class
- [ ] Add European culture set
- [ ] Add Oriental culture set
- [ ] Add High Fantasy culture set
- [ ] Add Dark Fantasy culture set

### Step 5: Integration (Day 9)
- [ ] Add to `MapGenerator.cs` pipeline
- [ ] Update `MapData.cs`
- [ ] Add configuration options

### Step 6: Testing (Day 10-12)
- [ ] Unit tests for culture selection
- [ ] Unit tests for placement
- [ ] Unit tests for expansion
- [ ] Integration tests
- [ ] Performance tests

### Step 7: Statistics (Day 13)
- [ ] Implement `CollectStatistics()`
- [ ] Calculate area and population
- [ ] Update culture demographics

### Step 8: Documentation (Day 14)
- [ ] Update README
- [ ] Add usage examples
- [ ] Document culture sets

## Configuration

```csharp
public class MapGenerationSettings
{
    // ... existing properties ...
    
    /// <summary>
    /// Number of cultures to generate
    /// </summary>
    public int CultureCount { get; set; } = 10;
    
    /// <summary>
    /// Culture set to use (European, Oriental, HighFantasy, etc.)
    /// </summary>
    public string CultureSet { get; set; } = "European";
    
    /// <summary>
    /// Neutral area rate (affects culture expansion)
    /// </summary>
    public double NeutralRate { get; set; } = 1.0;
    
    /// <summary>
    /// Size variety factor (affects expansionism)
    /// </summary>
    public double SizeVariety { get; set; } = 1.0;
}
```

## Success Criteria

- [ ] Cultures selected from defaults or random
- [ ] Culture centers placed strategically
- [ ] Culture types correctly classified
- [ ] Cultures expand realistically
- [ ] No unpopulated cells with culture
- [ ] All tests passing
- [ ] Performance < 3 seconds for typical map

## Dependencies

**Required:**
- Cell population data
- Biome data
- Feature data (for water bodies)
- Height data

**Blocks:**
- States (needs culture for expansion costs)
- Burgs (needs culture for naming)
- Religions (often follows culture borders)

## Notes

- Culture ID 0 is wildlands (no culture)
- Culture expansion is similar to state expansion but with different costs
- Culture types affect both culture and state expansion
- Name bases will be used for generating place names
- Shield shapes are for heraldry (coat of arms)

## References

- Original JS: `modules/cultures-generator.js`
- Algorithm: Dijkstra shortest path with culture-specific costs
- Default cultures: Multiple themed sets for variety
