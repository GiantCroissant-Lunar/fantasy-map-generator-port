# Spec 006: Burgs (Settlement System)

## Status
- **State:** Not Started
- **Priority:** ⭐⭐⭐⭐⭐ Critical (Foundation for all political features)
- **Estimated Effort:** 2 weeks
- **Dependencies:** None (can start immediately)
- **Blocks:** States (007), Provinces (009), Routes (010)

## Overview

Implement the settlement (burg) placement and management system. Burgs are cities, towns, and villages that serve as population centers, trade hubs, and state capitals.

## Goals

1. **Capital Placement** - Place one capital per state with optimal spacing
2. **Town Placement** - Place secondary settlements based on population density
3. **Port Detection** - Identify coastal settlements with harbors
4. **Feature Assignment** - Determine burg features (citadel, walls, plaza, etc.)
5. **Population Calculation** - Calculate realistic population values
6. **Type Classification** - Classify burgs by type (Naval, Highland, River, etc.)

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/burgs-and-states.js`
- Lines 1-300: Burg placement and specification
- Lines 150-200: Port detection
- Lines 200-250: Feature assignment

## Data Models

### Burg Model

```csharp
namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a settlement (city, town, or village)
/// </summary>
public class Burg
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Settlement name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Cell where the burg is located
    /// </summary>
    public int CellId { get; set; }
    
    /// <summary>
    /// Precise position (may differ from cell center for ports/rivers)
    /// </summary>
    public Point Position { get; set; }
    
    /// <summary>
    /// State this burg belongs to (0 = neutral)
    /// </summary>
    public int StateId { get; set; }
    
    /// <summary>
    /// Culture of this burg
    /// </summary>
    public int CultureId { get; set; }
    
    /// <summary>
    /// True if this is a state capital
    /// </summary>
    public bool IsCapital { get; set; }
    
    /// <summary>
    /// True if this is a port city
    /// </summary>
    public bool IsPort { get; set; }
    
    /// <summary>
    /// Water feature ID if this is a port (ocean, sea, or lake)
    /// </summary>
    public int? PortFeatureId { get; set; }
    
    /// <summary>
    /// Population in thousands
    /// </summary>
    public double Population { get; set; }
    
    /// <summary>
    /// Burg type based on location and characteristics
    /// </summary>
    public BurgType Type { get; set; }
    
    /// <summary>
    /// Feature ID (water body) this burg is on
    /// </summary>
    public int FeatureId { get; set; }
    
    // Burg Features
    
    /// <summary>
    /// Has a citadel/fortress
    /// </summary>
    public bool HasCitadel { get; set; }
    
    /// <summary>
    /// Has a central plaza/square
    /// </summary>
    public bool HasPlaza { get; set; }
    
    /// <summary>
    /// Has defensive walls
    /// </summary>
    public bool HasWalls { get; set; }
    
    /// <summary>
    /// Has shantytown/slums
    /// </summary>
    public bool HasShanty { get; set; }
    
    /// <summary>
    /// Has a major temple
    /// </summary>
    public bool HasTemple { get; set; }
    
    /// <summary>
    /// Coat of arms (heraldry)
    /// </summary>
    public CoatOfArms? CoA { get; set; }
}

/// <summary>
/// Burg classification based on location and characteristics
/// </summary>
public enum BurgType
{
    /// <summary>
    /// Generic inland settlement
    /// </summary>
    Generic,
    
    /// <summary>
    /// Port city on ocean or sea
    /// </summary>
    Naval,
    
    /// <summary>
    /// Settlement on lake shore
    /// </summary>
    Lake,
    
    /// <summary>
    /// Mountain or highland settlement
    /// </summary>
    Highland,
    
    /// <summary>
    /// Major river crossing or riverside city
    /// </summary>
    River,
    
    /// <summary>
    /// Desert or steppe nomadic settlement
    /// </summary>
    Nomadic,
    
    /// <summary>
    /// Forest hunting settlement
    /// </summary>
    Hunting
}

/// <summary>
/// Coat of arms for heraldry
/// </summary>
public class CoatOfArms
{
    public string Shield { get; set; } = "heater";
    public List<string> Charges { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    // Additional heraldry properties as needed
}
```

### Cell Extensions

```csharp
// Add to Cell.cs
public class Cell
{
    // ... existing properties ...
    
    /// <summary>
    /// Burg ID if this cell contains a settlement (0 = none)
    /// </summary>
    public int BurgId { get; set; }
    
    /// <summary>
    /// Harbor quality (0 = none, 1 = good, 2 = excellent)
    /// </summary>
    public byte Harbor { get; set; }
    
    /// <summary>
    /// Nearest water cell (for port detection)
    /// </summary>
    public int? HavenCell { get; set; }
}
```

## Algorithm

### 1. Capital Placement

**Goal:** Place one capital per state with good spacing

```csharp
private List<Burg> PlaceCapitals(int stateCount)
{
    var burgs = new List<Burg> { null }; // Index 0 is reserved
    
    // Calculate cell scores for capital placement
    var scores = new int[_map.Cells.Count];
    for (int i = 0; i < _map.Cells.Count; i++)
    {
        var cell = _map.Cells[i];
        if (cell.Population == 0 || cell.CultureId == 0)
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
            CultureId = cell.CultureId,
            FeatureId = cell.FeatureId
        };
        
        burgs.Add(burg);
        burgTree.Add(cell.Center, burg.Id);
        
        // If we've tried all cells, reduce spacing
        if (attempts == sortedCells.Count - 1 && burgs.Count <= stateCount)
        {
            spacing *= 0.8;
            attempts = 0;
        }
    }
    
    return burgs;
}
```

### 2. Town Placement

**Goal:** Place secondary settlements based on population

```csharp
private void PlaceTowns(List<Burg> burgs, int targetCount)
{
    // Calculate cell scores with randomization
    var scores = new int[_map.Cells.Count];
    for (int i = 0; i < _map.Cells.Count; i++)
    {
        var cell = _map.Cells[i];
        if (cell.BurgId > 0 || cell.Population == 0 || cell.CultureId == 0)
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
            if (cell.BurgId > 0) continue;
            
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
                CultureId = cell.CultureId,
                FeatureId = cell.FeatureId
            };
            
            burgs.Add(burg);
            burgTree.Add(cell.Center, burg.Id);
            cell.BurgId = burg.Id;
            placed++;
        }
        
        spacing *= 0.5; // Reduce spacing if we can't place enough
    }
    
    Console.WriteLine($"Placed {placed} towns (target: {targetCount})");
}
```

### 3. Specify Burgs (Ports, Population, Type)

```csharp
private void SpecifyBurgs(List<Burg> burgs)
{
    foreach (var burg in burgs.Where(b => b != null))
    {
        var cell = _map.Cells[burg.CellId];
        
        // 1. Detect ports
        if (cell.HavenCell.HasValue)
        {
            var havenCell = _map.Cells[cell.HavenCell.Value];
            var feature = _map.Features[havenCell.FeatureId];
            
            // Port if: capital with any harbor OR town with good harbor
            bool isPort = feature.Cells.Count > 1 && 
                         ((burg.IsCapital && cell.Harbor > 0) || cell.Harbor == 1);
            
            if (isPort && _map.Temperature[cell.GridCellId] > 0) // Not frozen
            {
                burg.IsPort = true;
                burg.PortFeatureId = havenCell.FeatureId;
                
                // Move position closer to water edge
                burg.Position = GetPortPosition(cell, havenCell);
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

private BurgType GetBurgType(Cell cell, bool isPort)
{
    if (isPort) return BurgType.Naval;
    
    // Lake
    if (cell.HavenCell.HasValue)
    {
        var haven = _map.Cells[cell.HavenCell.Value];
        var feature = _map.Features[haven.FeatureId];
        if (feature.Type == FeatureType.Lake)
            return BurgType.Lake;
    }
    
    // Highland
    if (cell.Height > 60)
        return BurgType.Highland;
    
    // River
    if (cell.RiverId > 0 && cell.Flux >= 100)
        return BurgType.River;
    
    // Nomadic (desert/steppe with low population)
    if (cell.Population <= 5 && IsDesertOrSteppe(cell.BiomeId))
        return BurgType.Nomadic;
    
    // Hunting (forest)
    if (cell.BiomeId >= 5 && cell.BiomeId <= 9)
        return BurgType.Hunting;
    
    return BurgType.Generic;
}
```

### 4. Define Burg Features

```csharp
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
        var cell = _map.Cells[burg.CellId];
        bool hasReligion = cell.ReligionId > 0;
        
        burg.HasTemple = (hasReligion && P(0.5)) || 
                        pop > 50 || 
                        (pop > 35 && P(0.75)) || 
                        (pop > 20 && P(0.5));
    }
}

// Probability helper
private bool P(double probability) => _random.NextDouble() < probability;
```

## Implementation Steps

### Step 1: Create Models (Day 1)
- [ ] Create `Burg.cs` model
- [ ] Create `BurgType.cs` enum
- [ ] Create `CoatOfArms.cs` model
- [ ] Add `BurgId` and `Harbor` to `Cell.cs`

### Step 2: Create Generator (Day 2-3)
- [ ] Create `BurgsGenerator.cs`
- [ ] Implement `PlaceCapitals()`
- [ ] Implement `PlaceTowns()`
- [ ] Implement `SpecifyBurgs()`
- [ ] Implement `DefineBurgFeatures()`

### Step 3: Helper Methods (Day 4)
- [ ] Implement `GetPortPosition()`
- [ ] Implement `GetBurgType()`
- [ ] Implement `Gauss()` distribution
- [ ] Implement `QuadTree` for spatial indexing

### Step 4: Integration (Day 5)
- [ ] Add to `MapGenerator.cs` pipeline
- [ ] Update `MapData.cs` to include burgs
- [ ] Add configuration to `MapGenerationSettings.cs`

### Step 5: Testing (Day 6-7)
- [ ] Unit tests for capital placement
- [ ] Unit tests for town placement
- [ ] Unit tests for port detection
- [ ] Unit tests for feature assignment
- [ ] Integration tests

### Step 6: Name Generation (Day 8-9)
- [ ] Implement basic name generation
- [ ] Use culture-based naming
- [ ] Generate unique names

### Step 7: Documentation (Day 10)
- [ ] Update README
- [ ] Add usage examples
- [ ] Document configuration options

## Configuration

```csharp
public class MapGenerationSettings
{
    // ... existing properties ...
    
    /// <summary>
    /// Number of states to generate (capitals)
    /// </summary>
    public int StateCount { get; set; } = 10;
    
    /// <summary>
    /// Number of towns to generate (0 = auto-calculate)
    /// </summary>
    public int TownCount { get; set; } = 0; // Auto = cells / 50
    
    /// <summary>
    /// Minimum spacing between capitals (as fraction of map size)
    /// </summary>
    public double CapitalSpacing { get; set; } = 0.1;
    
    /// <summary>
    /// Minimum spacing between towns (as fraction of map size)
    /// </summary>
    public double TownSpacing { get; set; } = 0.02;
}
```

## Testing Requirements

### Unit Tests

```csharp
[Fact]
public void PlaceCapitals_ShouldPlaceCorrectNumber()
{
    var generator = new BurgsGenerator(_map, _random);
    var burgs = generator.PlaceCapitals(10);
    
    Assert.Equal(11, burgs.Count); // 10 + null at index 0
    Assert.All(burgs.Skip(1), b => Assert.True(b.IsCapital));
}

[Fact]
public void PlaceCapitals_ShouldRespectMinimumSpacing()
{
    var generator = new BurgsGenerator(_map, _random);
    var burgs = generator.PlaceCapitals(10);
    
    double minSpacing = (_map.Width + _map.Height) / 2.0 / 10;
    
    for (int i = 1; i < burgs.Count; i++)
    {
        for (int j = i + 1; j < burgs.Count; j++)
        {
            double dist = Distance(burgs[i].Position, burgs[j].Position);
            Assert.True(dist >= minSpacing * 0.8); // Allow 20% tolerance
        }
    }
}

[Fact]
public void SpecifyBurgs_ShouldDetectPorts()
{
    var generator = new BurgsGenerator(_map, _random);
    var burgs = CreateTestBurgs();
    
    generator.SpecifyBurgs(burgs);
    
    var ports = burgs.Where(b => b.IsPort).ToList();
    Assert.All(ports, p => Assert.True(p.PortFeatureId.HasValue));
}

[Fact]
public void DefineBurgFeatures_CapitalsShouldHaveCitadel()
{
    var generator = new BurgsGenerator(_map, _random);
    var burgs = CreateTestBurgs();
    
    generator.DefineBurgFeatures(burgs);
    
    Assert.All(burgs.Where(b => b.IsCapital), b => Assert.True(b.HasCitadel));
}
```

### Performance Tests

```csharp
[Fact]
public void PlaceCapitals_ShouldCompleteInReasonableTime()
{
    var sw = Stopwatch.StartNew();
    var generator = new BurgsGenerator(_largeMap, _random);
    var burgs = generator.PlaceCapitals(50);
    sw.Stop();
    
    Assert.True(sw.ElapsedMilliseconds < 1000); // < 1 second
}
```

## Success Criteria

- [ ] Capitals placed with good spacing
- [ ] Towns placed based on population
- [ ] Ports correctly detected
- [ ] Population values realistic
- [ ] Burg types correctly classified
- [ ] Features assigned appropriately
- [ ] All tests passing
- [ ] Performance < 2 seconds for typical map

## Dependencies

**Required:**
- Cell population data
- Culture data (for naming)
- Feature data (for ports)
- Temperature data (for frozen ports)

**Blocks:**
- States generation (needs capitals)
- Provinces generation (needs burgs)
- Routes generation (needs burgs)

## Notes

- Burg ID 0 is reserved (no burg)
- Capital placement is critical - affects entire political map
- Port detection requires harbor quality calculation
- Population should scale with map size
- Name generation can be basic initially, enhanced later

## References

- Original JS: `modules/burgs-and-states.js`
- Algorithm: Poisson disk sampling with scoring
- Spacing: Quadtree spatial indexing
