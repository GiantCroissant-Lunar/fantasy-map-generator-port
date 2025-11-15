---
title: River Erosion Algorithm
status: draft
priority: high
estimated_effort: 1-2 hours
---

# River Erosion Algorithm

## Overview

Implement river erosion to carve valleys into terrain. Rivers with high water flow should lower the terrain around them, creating realistic valleys and gorges.

## Goals

- Rivers carve valleys based on water flux
- Modify `Cell.Height` values along river paths
- Create more realistic terrain with depth variation
- Maintain deterministic generation

## Non-Goals

- Visual rendering of valleys (handled by external projects)
- Erosion animation or simulation over time
- Complex geological modeling

## Requirements

### Functional Requirements

1. **Erosion Calculation**
   - Calculate erosion based on water flux
   - Only erode highlands (height >= 35)
   - Limit maximum erosion per cell (MAX_DOWNCUT = 5)
   - Preserve minimum land height (sea level = 20)

2. **Neighbor-Based Erosion**
   - Consider higher neighbors (upstream)
   - Calculate average flux from higher neighbors
   - Erosion power = current_flux / avg_higher_flux

3. **Configuration**
   - Enable/disable erosion
   - Configurable maximum downcut depth
   - Configurable minimum erosion height

### Non-Functional Requirements

1. **Performance**: <1s for 10,000 cells
2. **Determinism**: Same seed produces same erosion
3. **Stability**: No terrain inversion (rivers don't go uphill)

## Design

### Algorithm

```csharp
// src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs

/// <summary>
/// Simulates river erosion by downcutting riverbeds.
/// Creates valleys and gorges in highland areas.
/// </summary>
private void DowncutRivers()
{
    const int MAX_DOWNCUT = 5;
    const int MIN_DOWNCUT_HEIGHT = 35; // Only downcut highlands
    const int SEA_LEVEL = 20;
    
    int cellsEroded = 0;
    int totalErosion = 0;
    
    foreach (var cell in _map.Cells)
    {
        // Skip if not a river cell or too low
        if (!cell.HasRiver || cell.Height < MIN_DOWNCUT_HEIGHT)
            continue;
        
        // Get flux for this cell
        if (!_flowAccumulation.TryGetValue(cell.Id, out int flux) || flux == 0)
            continue;
        
        // Find higher neighbors (upstream)
        var higherNeighbors = cell.Neighbors
            .Where(nId => nId >= 0 && nId < _map.Cells.Count)
            .Where(nId => _map.Cells[nId].Height > cell.Height)
            .ToList();
        
        if (!higherNeighbors.Any())
            continue;
        
        // Calculate average flux from higher neighbors
        double avgHigherFlux = higherNeighbors
            .Select(nId => _flowAccumulation.GetValueOrDefault(nId, 0))
            .Where(f => f > 0)
            .DefaultIfEmpty(1)
            .Average();
        
        if (avgHigherFlux == 0)
            continue;
        
        // Calculate erosion power
        double erosionPower = flux / avgHigherFlux;
        int downcutAmount = (int)Math.Floor(erosionPower);
        
        if (downcutAmount > 0)
        {
            // Apply downcut with limits
            int actualDowncut = Math.Min(downcutAmount, MAX_DOWNCUT);
            int newHeight = Math.Max(cell.Height - actualDowncut, SEA_LEVEL);
            
            if (newHeight < cell.Height)
            {
                int erosion = cell.Height - newHeight;
                cell.Height = (byte)newHeight;
                cellsEroded++;
                totalErosion += erosion;
            }
        }
    }
    
    Console.WriteLine($"Eroded {cellsEroded} cells, total erosion: {totalErosion} units");
}
```

### Integration

```csharp
public void Generate()
{
    Console.WriteLine("Generating hydrology...");
    
    FillPits();
    CalculateFlowDirections();
    CalculateFlowAccumulation();
    GenerateRivers();
    IdentifyLakes();
    CalculateRiverWidths();
    
    // NEW: Apply erosion AFTER rivers are generated
    if (_settings.EnableRiverErosion)
    {
        DowncutRivers();
    }
    
    // Meandering should come after erosion
    if (_settings.EnableRiverMeandering)
    {
        AddMeanderingToRivers();
    }
    
    // ... rest of code ...
}
```

### Configuration

```csharp
// src/FantasyMapGenerator.Core/Models/MapGenerationSettings.cs
public class MapGenerationSettings
{
    // Existing properties...
    
    /// <summary>
    /// Enable river erosion/downcutting
    /// </summary>
    public bool EnableRiverErosion { get; set; } = true;
    
    /// <summary>
    /// Maximum height a river can erode per cell (1-10)
    /// </summary>
    public int MaxErosionDepth { get; set; } = 5;
    
    /// <summary>
    /// Minimum height for erosion to occur
    /// </summary>
    public int MinErosionHeight { get; set; } = 35;
}
```

## Implementation Tasks

### Phase 1: Configuration (15 min)
- [ ] Add erosion settings to `MapGenerationSettings`
- [ ] Add default values

### Phase 2: Core Algorithm (45 min)
- [ ] Implement `DowncutRivers()` method in `HydrologyGenerator`
- [ ] Add safety checks (bounds, null checks)
- [ ] Add logging/diagnostics
- [ ] Handle edge cases

### Phase 3: Integration (15 min)
- [ ] Call `DowncutRivers()` from `Generate()`
- [ ] Ensure correct ordering (after rivers, before meandering)
- [ ] Respect configuration settings

### Phase 4: Testing (30 min)
- [ ] Unit test: river cells are lower after erosion
- [ ] Unit test: erosion respects maximum downcut
- [ ] Unit test: erosion doesn't go below sea level
- [ ] Integration test: full map generation with erosion

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void DowncutRivers_LowersRiverCells()
{
    // Setup map with river
    var cell = _map.Cells[100];
    cell.HasRiver = true;
    cell.Height = 50;
    int originalHeight = cell.Height;
    
    _generator.DowncutRivers();
    
    Assert.True(cell.Height <= originalHeight);
}

[Fact]
public void DowncutRivers_RespectsMaxDowncut()
{
    var cell = _map.Cells[100];
    cell.HasRiver = true;
    cell.Height = 80;
    int originalHeight = cell.Height;
    
    _generator.DowncutRivers();
    
    int erosion = originalHeight - cell.Height;
    Assert.True(erosion <= 5); // MAX_DOWNCUT
}

[Fact]
public void DowncutRivers_DoesNotGoBelowSeaLevel()
{
    var cell = _map.Cells[100];
    cell.HasRiver = true;
    cell.Height = 22;
    
    _generator.DowncutRivers();
    
    Assert.True(cell.Height >= 20); // SEA_LEVEL
}

[Fact]
public void DowncutRivers_OnlyErodesHighlands()
{
    var lowlandCell = _map.Cells[100];
    lowlandCell.HasRiver = true;
    lowlandCell.Height = 30; // Below MIN_DOWNCUT_HEIGHT
    int originalHeight = lowlandCell.Height;
    
    _generator.DowncutRivers();
    
    Assert.Equal(originalHeight, lowlandCell.Height); // No erosion
}
```

### Integration Test

```csharp
[Fact]
public void MapGeneration_WithErosion_CreatesValleys()
{
    var settings = new MapGenerationSettings
    {
        Seed = 12345,
        EnableRiverErosion = true,
        MaxErosionDepth = 5
    };
    
    var map = new MapGenerator().Generate(settings);
    
    // Check that some river cells are lower than neighbors
    var riverCells = map.Cells.Where(c => c.HasRiver && c.Height >= 35).ToList();
    int valleyCells = riverCells.Count(cell =>
    {
        var avgNeighborHeight = cell.Neighbors
            .Select(nId => map.Cells[nId].Height)
            .Average();
        return cell.Height < avgNeighborHeight;
    });
    
    Assert.True(valleyCells > 0, "Erosion should create some valleys");
}
```

## Success Criteria

- [ ] River cells in highlands are lower than before erosion
- [ ] Erosion respects maximum downcut limit
- [ ] No cells eroded below sea level
- [ ] Lowlands (<35 height) are not eroded
- [ ] Performance: <1s for typical map
- [ ] All tests pass

## References

- Original Azgaar implementation: `ref-projects/Fantasy-Map-Generator/modules/river-generator.js` (downcutRivers function)
- Documentation: `docs/MISSING_FEATURES_GUIDE.md` (River Erosion section)
- Comparison: `docs/COMPARISON_WITH_ORIGINAL.md`

## Notes

- This is a **data modification** feature, not rendering
- External projects will automatically see the modified heights
- Erosion creates valleys that make terrain more realistic
- Should run AFTER river generation but BEFORE meandering
- Simple algorithm - more advanced erosion can be added later (see spec 004)
