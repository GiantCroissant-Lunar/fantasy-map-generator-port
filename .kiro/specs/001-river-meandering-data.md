---
title: River Meandering Data Generation
status: draft
priority: high
estimated_effort: 2-3 hours
---

# River Meandering Data Generation

## Overview

Generate meandered path points for rivers to enable smooth curve rendering in external projects. This adds natural-looking curves to rivers instead of straight lines between cell centers.

## Goals

- Generate interpolated points along river paths
- Store meandered paths in `River.MeanderedPath`
- Provide data for external rendering projects to draw smooth curves
- Maintain deterministic generation from seed

## Non-Goals

- Rendering the curves (handled by external projects)
- Visual styling or colors
- UI components

## Requirements

### Functional Requirements

1. **Path Interpolation**
   - Generate intermediate points between river cells
   - Apply sinusoidal offset for natural meandering
   - Reduce meandering in steep terrain
   - Increase meandering in flat areas

2. **Data Model**
   - Add `MeanderedPath` property to `River` class
   - Store list of `Point` objects
   - Maintain order from source to mouth

3. **Algorithm Parameters**
   - Base meandering factor (0.0-1.0, default 0.5)
   - Distance-based decay (meander less far from source)
   - Terrain-based adjustment (less meander in mountains)

### Non-Functional Requirements

1. **Performance**: <100ms for typical river (50 cells)
2. **Determinism**: Same seed produces same meandered paths
3. **Memory**: Minimal overhead (3-5x cell count for points)

## Design

### Data Model Changes

```csharp
// src/FantasyMapGenerator.Core/Models/River.cs
public class River
{
    // Existing properties...
    
    /// <summary>
    /// Meandered path points for smooth curve rendering.
    /// External rendering projects use these points to draw natural curves.
    /// </summary>
    public List<Point> MeanderedPath { get; set; } = new();
}
```

### New Class: RiverMeandering

```csharp
// src/FantasyMapGenerator.Core/Generators/RiverMeandering.cs
public class RiverMeandering
{
    private readonly MapData _map;
    
    public RiverMeandering(MapData map)
    {
        _map = map;
    }
    
    /// <summary>
    /// Generates meandered path points for a river.
    /// </summary>
    /// <param name="river">River to process</param>
    /// <param name="baseMeandering">Base meandering factor (0.5 = moderate)</param>
    /// <returns>List of points forming the meandered path</returns>
    public List<Point> GenerateMeanderedPath(River river, double baseMeandering = 0.5)
    {
        // Implementation
    }
    
    private double CalculateMeanderingFactor(int step, double baseMeandering)
    {
        // Decreases with distance from source
    }
    
    private List<Point> InterpolatePoints(Point start, Point end, double meander)
    {
        // Generate intermediate points with sinusoidal offset
    }
}
```

### Integration Point

```csharp
// src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs
public void Generate()
{
    // ... existing code ...
    
    GenerateRivers();
    CalculateRiverWidths();
    
    // NEW: Add meandering
    if (_settings.EnableRiverMeandering)
    {
        AddMeanderingToRivers();
    }
    
    // ... rest of code ...
}

private void AddMeanderingToRivers()
{
    var meandering = new RiverMeandering(_map);
    
    foreach (var river in _map.Rivers)
    {
        river.MeanderedPath = meandering.GenerateMeanderedPath(
            river, 
            _settings.MeanderingFactor);
    }
}
```

### Configuration

```csharp
// src/FantasyMapGenerator.Core/Models/MapGenerationSettings.cs
public class MapGenerationSettings
{
    // Existing properties...
    
    /// <summary>
    /// Enable river meandering path generation
    /// </summary>
    public bool EnableRiverMeandering { get; set; } = true;
    
    /// <summary>
    /// Base meandering factor (0.0 = straight, 1.0 = very curvy)
    /// </summary>
    public double MeanderingFactor { get; set; } = 0.5;
}
```

## Implementation Tasks

### Phase 1: Data Model (30 min)
- [ ] Add `MeanderedPath` property to `River` class
- [ ] Add configuration properties to `MapGenerationSettings`
- [ ] Update `River` constructor/initialization

### Phase 2: Core Algorithm (1-1.5 hours)
- [ ] Create `RiverMeandering.cs` class
- [ ] Implement `GenerateMeanderedPath()` method
- [ ] Implement `CalculateMeanderingFactor()` helper
- [ ] Implement `InterpolatePoints()` helper
- [ ] Handle edge cases (short rivers, steep terrain)

### Phase 3: Integration (30 min)
- [ ] Add `AddMeanderingToRivers()` to `HydrologyGenerator`
- [ ] Call from `Generate()` method
- [ ] Respect configuration settings

### Phase 4: Testing (30 min)
- [ ] Unit test: meandered path has more points than cells
- [ ] Unit test: deterministic from seed
- [ ] Unit test: respects meandering factor
- [ ] Integration test: full map generation with meandering

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void GenerateMeanderedPath_CreatesMorePoints()
{
    var river = new River { Cells = new List<int> { 0, 1, 2, 3 } };
    var meandering = new RiverMeandering(map);
    
    var path = meandering.GenerateMeanderedPath(river);
    
    Assert.True(path.Count > river.Cells.Count);
}

[Fact]
public void GenerateMeanderedPath_IsDeterministic()
{
    var river = new River { Cells = new List<int> { 0, 1, 2, 3 } };
    var meandering = new RiverMeandering(map);
    
    var path1 = meandering.GenerateMeanderedPath(river, 0.5);
    var path2 = meandering.GenerateMeanderedPath(river, 0.5);
    
    Assert.Equal(path1.Count, path2.Count);
    for (int i = 0; i < path1.Count; i++)
    {
        Assert.Equal(path1[i].X, path2[i].X, precision: 5);
        Assert.Equal(path1[i].Y, path2[i].Y, precision: 5);
    }
}

[Theory]
[InlineData(0.0)] // Straight
[InlineData(0.5)] // Moderate
[InlineData(1.0)] // Very curvy
public void GenerateMeanderedPath_RespectsMeanderingFactor(double factor)
{
    var river = new River { Cells = new List<int> { 0, 1, 2, 3 } };
    var meandering = new RiverMeandering(map);
    
    var path = meandering.GenerateMeanderedPath(river, factor);
    
    Assert.NotEmpty(path);
}
```

### Integration Test

```csharp
[Fact]
public void MapGeneration_WithMeandering_PopulatesRiverPaths()
{
    var settings = new MapGenerationSettings
    {
        Seed = 12345,
        EnableRiverMeandering = true,
        MeanderingFactor = 0.5
    };
    
    var map = new MapGenerator().Generate(settings);
    
    Assert.All(map.Rivers, river =>
    {
        Assert.NotEmpty(river.MeanderedPath);
        Assert.True(river.MeanderedPath.Count >= river.Cells.Count);
    });
}
```

## Success Criteria

- [ ] All rivers have populated `MeanderedPath`
- [ ] Meandered paths have 3-5x more points than cell count
- [ ] Rivers look natural when rendered by external projects
- [ ] Generation is deterministic from seed
- [ ] Performance: <100ms per river
- [ ] All tests pass

## References

- Original Azgaar implementation: `ref-projects/Fantasy-Map-Generator/modules/river-generator.js` (addMeandering function)
- Documentation: `docs/MISSING_FEATURES_GUIDE.md` (River Meandering section)
- Reference: `docs/COMPARISON_WITH_ORIGINAL.md`

## Notes

- This is a **data generation** feature, not rendering
- External projects (HyacinthBean.MapViewer) will use this data to draw curves
- Algorithm based on original Azgaar's Fantasy Map Generator
- Meandering decreases with distance from source (natural behavior)
- Meandering reduces in steep terrain (rivers flow straighter in mountains)
