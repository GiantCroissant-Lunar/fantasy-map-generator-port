---
title: Advanced Erosion Algorithm
status: draft
priority: medium
estimated_effort: 4-6 hours
dependencies: [002-river-erosion-algorithm]
---

# Advanced Erosion Algorithm

## Overview

Implement the advanced erosion algorithm from the reference C# project (Choochoo/mewo2 approach). This creates more realistic terrain by considering the number of higher neighbors, not just water flux.

## Goals

- More sophisticated erosion than simple downcutting
- Create natural-looking valleys and ridges
- Iterative refinement for better results
- Maintain compatibility with existing erosion

## Non-Goals

- Replace simple erosion (keep both as options)
- Complex geological simulation
- Real-time erosion visualization

## Requirements

### Functional Requirements

1. **Neighbor-Based Erosion**
   - Count higher neighbors for each cell
   - Erosion proportional to (higher_neighbors - 3)
   - Cells with 3 neighbors are stable
   - More than 3 = deposition, less than 3 = erosion

2. **Iterative Application**
   - Apply erosion in multiple iterations
   - Each iteration refines the terrain
   - Configurable iteration count (default 5)

3. **Configuration**
   - Enable/disable advanced erosion
   - Configurable iteration count
   - Configurable erosion amount per iteration

### Non-Functional Requirements

1. **Performance**: <2s for 10,000 cells with 5 iterations
2. **Determinism**: Same seed produces same results
3. **Stability**: Terrain doesn't become inverted

## Design

### Algorithm (from Reference Project)

```csharp
// src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs

/// <summary>
/// Advanced erosion algorithm based on mewo2's terrain generator.
/// Ported from Choochoo's C# implementation.
/// 
/// Original: https://github.com/mewo2/terrain
/// C# Port: https://github.com/Choochoo/FantasyMapGenerator
/// </summary>
public void ApplyAdvancedErosion(int iterations = 5, double amount = 0.1)
{
    Console.WriteLine($"Applying advanced erosion ({iterations} iterations)...");
    
    for (int iter = 0; iter < iterations; iter++)
    {
        var erosionDeltas = new double[_map.Cells.Count];
        
        foreach (var cell in _map.Cells.Where(c => c.IsLand))
        {
            // Count higher neighbors
            int higherNeighbors = cell.Neighbors
                .Count(nId => nId >= 0 && nId < _map.Cells.Count && 
                       _map.Cells[nId].Height > cell.Height);
            
            // Erosion proportional to (higher_neighbors - 3)
            // Cells with 3 neighbors are stable
            // More than 3 = deposition (positive delta)
            // Less than 3 = erosion (negative delta)
            erosionDeltas[cell.Id] = amount * (higherNeighbors - 3);
        }
        
        // Apply erosion
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            if (cell.IsLand)
            {
                int newHeight = (int)(cell.Height + erosionDeltas[i]);
                cell.Height = (byte)Math.Clamp(newHeight, 20, 100);
            }
        }
    }
    
    Console.WriteLine("Advanced erosion complete");
}
```

### Integration

```csharp
public void Generate()
{
    // ... existing code ...
    
    GenerateRivers();
    IdentifyLakes();
    CalculateRiverWidths();
    
    // Apply erosion (choose algorithm based on settings)
    if (_settings.UseAdvancedErosion)
    {
        ApplyAdvancedErosion(
            _settings.ErosionIterations, 
            _settings.ErosionAmount);
    }
    else if (_settings.EnableRiverErosion)
    {
        DowncutRivers(); // Simple erosion from spec 002
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
    /// Use advanced erosion algorithm (neighbor-based)
    /// If false, uses simple river downcutting
    /// </summary>
    public bool UseAdvancedErosion { get; set; } = false;
    
    /// <summary>
    /// Number of erosion iterations (3-10 typical)
    /// </summary>
    public int ErosionIterations { get; set; } = 5;
    
    /// <summary>
    /// Erosion amount per iteration (0.05-0.2 typical)
    /// </summary>
    public double ErosionAmount { get; set; } = 0.1;
}
```

## Implementation Tasks

### Phase 1: Configuration (15 min)
- [ ] Add advanced erosion settings to `MapGenerationSettings`
- [ ] Set appropriate defaults

### Phase 2: Core Algorithm (2 hours)
- [ ] Implement `ApplyAdvancedErosion()` method
- [ ] Implement neighbor counting logic
- [ ] Implement delta calculation
- [ ] Implement iterative application
- [ ] Add safety checks and bounds

### Phase 3: Integration (30 min)
- [ ] Update `Generate()` to choose erosion algorithm
- [ ] Ensure correct ordering in generation pipeline
- [ ] Add logging/diagnostics

### Phase 4: Testing (1.5 hours)
- [ ] Unit test: erosion creates valleys
- [ ] Unit test: stable cells (3 neighbors) don't change much
- [ ] Unit test: deposition occurs with many higher neighbors
- [ ] Performance test: iterations complete in reasonable time
- [ ] Integration test: compare with simple erosion

### Phase 5: Documentation (30 min)
- [ ] Add algorithm credits
- [ ] Document differences from simple erosion
- [ ] Add usage examples

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void AdvancedErosion_CreatesValleys()
{
    var settings = new MapGenerationSettings
    {
        UseAdvancedErosion = true,
        ErosionIterations = 5
    };
    
    var map = new MapGenerator().Generate(settings);
    
    // Check for height variation (valleys and ridges)
    var landCells = map.Cells.Where(c => c.IsLand).ToList();
    var heightStdDev = CalculateStandardDeviation(landCells.Select(c => (double)c.Height));
    
    Assert.True(heightStdDev > 5, "Erosion should create height variation");
}

[Fact]
public void AdvancedErosion_StableCells_MinimalChange()
{
    // Create cell with exactly 3 higher neighbors
    var cell = CreateCellWithNeighbors(higherCount: 3);
    int originalHeight = cell.Height;
    
    _generator.ApplyAdvancedErosion(iterations: 1, amount: 0.1);
    
    // Should have minimal change (delta = 0.1 * (3 - 3) = 0)
    Assert.Equal(originalHeight, cell.Height);
}

[Theory]
[InlineData(1, 0.1)]
[InlineData(5, 0.1)]
[InlineData(10, 0.05)]
public void AdvancedErosion_CompletesInReasonableTime(int iterations, double amount)
{
    var stopwatch = Stopwatch.StartNew();
    
    _generator.ApplyAdvancedErosion(iterations, amount);
    
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
        $"Erosion took {stopwatch.ElapsedMilliseconds}ms");
}
```

### Comparison Test

```csharp
[Fact]
public void AdvancedErosion_DifferentFromSimpleErosion()
{
    var settings1 = new MapGenerationSettings
    {
        Seed = 12345,
        UseAdvancedErosion = false,
        EnableRiverErosion = true
    };
    
    var settings2 = new MapGenerationSettings
    {
        Seed = 12345,
        UseAdvancedErosion = true
    };
    
    var map1 = new MapGenerator().Generate(settings1);
    var map2 = new MapGenerator().Generate(settings2);
    
    // Heights should be different
    int differentCells = 0;
    for (int i = 0; i < map1.Cells.Count; i++)
    {
        if (map1.Cells[i].Height != map2.Cells[i].Height)
            differentCells++;
    }
    
    Assert.True(differentCells > map1.Cells.Count * 0.1, 
        "Advanced erosion should produce different results");
}
```

## Success Criteria

- [ ] Advanced erosion creates more natural terrain
- [ ] Valleys and ridges are more pronounced
- [ ] Stable cells (3 neighbors) remain relatively unchanged
- [ ] Performance: <2s for 5 iterations on typical map
- [ ] Both simple and advanced erosion work
- [ ] All tests pass

## References

- Reference implementation: `ref-projects/FantasyMapGenerator/Terrain/Terrain.cs` (Erode method)
- Documentation: `docs/REFERENCE_PROJECT_ANALYSIS.md` (Enhanced Erosion section)
- Original algorithm: mewo2's terrain generator

## Notes

- This is an **alternative** to simple erosion, not a replacement
- Based on Choochoo's C# port of mewo2's algorithm
- More sophisticated than simple river downcutting
- Creates better overall terrain, not just river valleys
- Can be combined with simple erosion (run advanced first, then simple for rivers)
- Credit original authors in code comments
