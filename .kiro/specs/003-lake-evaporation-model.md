---
title: Lake Evaporation Model
status: draft
priority: medium
estimated_effort: 3-4 hours
---

# Lake Evaporation Model

## Overview

Implement lake evaporation modeling to create closed basins (lakes with no outlet). Some lakes should not drain to the ocean if evaporation exceeds inflow, creating salt lakes like the Dead Sea or Great Salt Lake.

## Goals

- Calculate evaporation for each lake
- Identify closed basins (evaporation >= inflow)
- Classify lakes by type (freshwater, saltwater, seasonal)
- Provide data for external rendering projects

## Non-Goals

- Visual rendering of lakes (handled by external projects)
- Detailed climate simulation
- Seasonal variation modeling (future enhancement)

## Requirements

### Functional Requirements

1. **Lake Model**
   - Create `Lake` class with evaporation properties
   - Track inflow (from rivers)
   - Calculate evaporation (based on temperature, surface area, precipitation)
   - Determine if lake is closed (evaporation >= inflow)

2. **Lake Identification**
   - Group connected water cells into lakes
   - Identify shoreline cells
   - Find potential outlet points

3. **Evaporation Calculation**
   - Base evaporation = surface_area × temperature × rate
   - Net evaporation = base - (precipitation × surface_area)
   - Closed if net_evaporation >= inflow

4. **Lake Classification**
   - Freshwater: Open lake with outlet
   - Saltwater: Closed lake (no outlet)
   - Seasonal: Evaporation close to inflow

### Non-Functional Requirements

1. **Performance**: <500ms for 100 lakes
2. **Determinism**: Same seed produces same lake types
3. **Accuracy**: Realistic evaporation rates

## Design

### Data Model

```csharp
// src/FantasyMapGenerator.Core/Models/Lake.cs
public class Lake
{
    public int Id { get; set; }
    
    /// <summary>
    /// Cells that make up the lake
    /// </summary>
    public List<int> Cells { get; set; } = new();
    
    /// <summary>
    /// Shoreline cells (land cells adjacent to lake)
    /// </summary>
    public List<int> Shoreline { get; set; } = new();
    
    /// <summary>
    /// Cell where water exits the lake (if any)
    /// </summary>
    public int OutletCell { get; set; } = -1;
    
    /// <summary>
    /// River ID that drains this lake
    /// </summary>
    public int? OutletRiver { get; set; }
    
    /// <summary>
    /// Total water flux entering the lake (m³/s)
    /// </summary>
    public double Inflow { get; set; }
    
    /// <summary>
    /// Water lost to evaporation (m³/s)
    /// </summary>
    public double Evaporation { get; set; }
    
    /// <summary>
    /// Net flux leaving the lake (Inflow - Evaporation)
    /// </summary>
    public double NetOutflow => Math.Max(Inflow - Evaporation, 0);
    
    /// <summary>
    /// True if lake has no outlet (evaporation >= inflow)
    /// </summary>
    public bool IsClosed => Evaporation >= Inflow;
    
    /// <summary>
    /// Lake type based on closure and salinity
    /// </summary>
    public LakeType Type { get; set; }
    
    /// <summary>
    /// Average temperature of lake cells
    /// </summary>
    public double Temperature { get; set; }
    
    /// <summary>
    /// Average precipitation over lake
    /// </summary>
    public double Precipitation { get; set; }
    
    /// <summary>
    /// Surface area in square kilometers
    /// </summary>
    public double SurfaceArea { get; set; }
    
    /// <summary>
    /// Rivers flowing into this lake
    /// </summary>
    public List<int> InflowingRivers { get; set; } = new();
}

public enum LakeType
{
    Freshwater,     // Open lake with outlet
    Saltwater,      // Closed lake (no outlet)
    Brackish,       // Partially closed
    Seasonal        // Dries up in summer
}
```

### Update MapData

```csharp
// src/FantasyMapGenerator.Core/Models/MapData.cs
public class MapData
{
    // Existing properties...
    
    /// <summary>
    /// Lakes and inland water bodies
    /// </summary>
    public List<Lake> Lakes { get; set; } = new();
}
```

### Algorithm

```csharp
// src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs

private void IdentifyLakesWithEvaporation()
{
    Console.WriteLine("Identifying lakes and calculating evaporation...");
    
    var lakeCells = _map.Cells
        .Where(c => c.Height < 20 && c.Height > 0) // Water but not ocean
        .ToList();
    
    if (!lakeCells.Any()) return;
    
    // Group connected water cells into lakes
    var lakes = GroupIntoLakes(lakeCells);
    
    foreach (var lake in lakes)
    {
        CalculateLakeProperties(lake);
        CalculateLakeEvaporation(lake);
        DetermineLakeOutlet(lake);
    }
    
    _map.Lakes = lakes;
    Console.WriteLine($"Identified {lakes.Count} lakes ({lakes.Count(l => l.IsClosed)} closed)");
}

private List<Lake> GroupIntoLakes(List<Cell> lakeCells)
{
    // Flood fill to find connected water cells
}

private void CalculateLakeProperties(Lake lake)
{
    // Calculate temperature, precipitation, surface area, inflow
}

private void CalculateLakeEvaporation(Lake lake)
{
    const double BASE_EVAPORATION_RATE = 0.5;
    const double PRECIP_REDUCTION_FACTOR = 0.3;
    
    // Base evaporation increases with temperature
    double tempFactor = Math.Max(lake.Temperature + 10, 0) / 30.0;
    double baseEvaporation = lake.SurfaceArea * tempFactor * BASE_EVAPORATION_RATE;
    
    // Precipitation reduces net evaporation
    double precipReduction = lake.Precipitation * lake.SurfaceArea * PRECIP_REDUCTION_FACTOR;
    
    lake.Evaporation = Math.Max(baseEvaporation - precipReduction, 0);
}

private void DetermineLakeOutlet(Lake lake)
{
    if (lake.IsClosed)
    {
        lake.OutletCell = -1;
        lake.OutletRiver = null;
        lake.Type = LakeType.Saltwater;
    }
    else
    {
        // Find lowest shoreline point as outlet
        lake.Type = LakeType.Freshwater;
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
    /// Enable lake evaporation modeling
    /// </summary>
    public bool EnableLakeEvaporation { get; set; } = true;
    
    /// <summary>
    /// Base evaporation rate (m³/s per km² per degree)
    /// </summary>
    public double BaseEvaporationRate { get; set; } = 0.5;
}
```

## Implementation Tasks

### Phase 1: Data Model (45 min)
- [ ] Create `Lake.cs` class with all properties
- [ ] Create `LakeType` enum
- [ ] Add `Lakes` property to `MapData`
- [ ] Add configuration to `MapGenerationSettings`

### Phase 2: Lake Identification (1 hour)
- [ ] Implement `GroupIntoLakes()` - flood fill algorithm
- [ ] Implement `CalculateLakeProperties()` - temperature, area, etc.
- [ ] Find shoreline cells
- [ ] Identify inflowing rivers

### Phase 3: Evaporation Calculation (45 min)
- [ ] Implement `CalculateLakeEvaporation()` algorithm
- [ ] Calculate base evaporation from temperature
- [ ] Apply precipitation reduction
- [ ] Determine net evaporation

### Phase 4: Outlet Determination (30 min)
- [ ] Implement `DetermineLakeOutlet()`
- [ ] Find lowest shoreline point
- [ ] Classify lake type
- [ ] Handle closed basins

### Phase 5: Integration & Testing (45 min)
- [ ] Integrate into `HydrologyGenerator.Generate()`
- [ ] Unit tests for evaporation calculation
- [ ] Unit tests for lake classification
- [ ] Integration test for full generation

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void CalculateLakeEvaporation_HighTemperature_HighEvaporation()
{
    var lake = new Lake
    {
        SurfaceArea = 100,
        Temperature = 30, // Hot
        Precipitation = 0.5
    };
    
    _generator.CalculateLakeEvaporation(lake);
    
    Assert.True(lake.Evaporation > 0);
}

[Fact]
public void Lake_IsClosed_WhenEvaporationExceedsInflow()
{
    var lake = new Lake
    {
        Inflow = 100,
        Evaporation = 150
    };
    
    Assert.True(lake.IsClosed);
    Assert.Equal(0, lake.NetOutflow);
}

[Fact]
public void Lake_IsOpen_WhenInflowExceedsEvaporation()
{
    var lake = new Lake
    {
        Inflow = 150,
        Evaporation = 100
    };
    
    Assert.False(lake.IsClosed);
    Assert.Equal(50, lake.NetOutflow);
}

[Fact]
public void DetermineLakeOutlet_ClosedLake_NoOutlet()
{
    var lake = new Lake
    {
        Inflow = 50,
        Evaporation = 100
    };
    
    _generator.DetermineLakeOutlet(lake);
    
    Assert.Equal(-1, lake.OutletCell);
    Assert.Null(lake.OutletRiver);
    Assert.Equal(LakeType.Saltwater, lake.Type);
}
```

### Integration Test

```csharp
[Fact]
public void MapGeneration_WithLakeEvaporation_CreatesClosedBasins()
{
    var settings = new MapGenerationSettings
    {
        Seed = 12345,
        EnableLakeEvaporation = true
    };
    
    var map = new MapGenerator().Generate(settings);
    
    Assert.NotEmpty(map.Lakes);
    
    // Should have both open and closed lakes
    Assert.Contains(map.Lakes, l => l.IsClosed);
    Assert.Contains(map.Lakes, l => !l.IsClosed);
    
    // Closed lakes should be saltwater
    var closedLakes = map.Lakes.Where(l => l.IsClosed);
    Assert.All(closedLakes, l => Assert.Equal(LakeType.Saltwater, l.Type));
}
```

## Success Criteria

- [ ] All lakes have calculated evaporation
- [ ] Some lakes are identified as closed basins
- [ ] Lake types are correctly assigned
- [ ] Closed lakes have no outlet
- [ ] Open lakes have outlet cells
- [ ] Performance: <500ms for typical map
- [ ] All tests pass

## References

- Original Azgaar implementation: `ref-projects/Fantasy-Map-Generator/modules/river-generator.js` (lake evaporation logic)
- Documentation: `docs/MISSING_FEATURES_GUIDE.md` (Lake Evaporation section)
- Real-world examples: Dead Sea, Great Salt Lake, Caspian Sea

## Notes

- This is a **data model** feature, not rendering
- External projects can use `lake.IsClosed` and `lake.Type` for styling
- Evaporation formula is simplified but realistic
- Future enhancement: seasonal variation in evaporation
- Future enhancement: salinity calculation for closed lakes
