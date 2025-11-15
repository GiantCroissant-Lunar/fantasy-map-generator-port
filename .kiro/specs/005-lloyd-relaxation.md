---
title: Lloyd Relaxation for Point Distribution
status: draft
priority: low
estimated_effort: 2-3 hours
---

# Lloyd Relaxation for Point Distribution

## Overview

Implement Lloyd relaxation algorithm to improve Voronoi point distribution. This creates more uniform cell sizes by iteratively moving points toward their Voronoi cell centroids.

## Goals

- Improve point distribution uniformity
- Reduce cell size variation
- Create more aesthetically pleasing maps
- Optional feature (doesn't break existing generation)

## Non-Goals

- Replace Poisson disk sampling (keep as default)
- Guarantee perfect uniformity
- Real-time relaxation visualization

## Requirements

### Functional Requirements

1. **Lloyd Relaxation Algorithm**
   - Move each point to its Voronoi cell centroid
   - Repeat for N iterations (typically 1-3)
   - Maintain points within map bounds

2. **Centroid Calculation**
   - Calculate geometric center of Voronoi cell
   - Handle irregular polygons
   - Handle edge cases (border cells)

3. **Configuration**
   - Enable/disable relaxation
   - Configurable iteration count
   - Apply after initial point generation

### Non-Functional Requirements

1. **Performance**: <2s per iteration for 10,000 points
2. **Determinism**: Same seed produces same results
3. **Quality**: Reduces cell size variance by 20-30%

## Design

### Algorithm (from Reference Project)

```csharp
// src/FantasyMapGenerator.Core/Geometry/GeometryUtils.cs

/// <summary>
/// Applies Lloyd relaxation to improve point distribution.
/// Moves points toward their Voronoi cell centroids.
/// 
/// Based on Choochoo's implementation.
/// Reference: https://github.com/Choochoo/FantasyMapGenerator
/// </summary>
public static List<Point> ApplyLloydRelaxation(
    List<Point> points, 
    int width, 
    int height, 
    int iterations = 1)
{
    for (int iter = 0; iter < iterations; iter++)
    {
        // Generate Voronoi diagram from current points
        var voronoi = Voronoi.FromPoints(
            points.ToArray(), 
            points.Count, 
            width, 
            height);
        
        // Move each point to its cell's centroid
        for (int i = 0; i < points.Count; i++)
        {
            var cellVertices = voronoi.GetCellVertices(i);
            
            if (cellVertices.Count >= 3)
            {
                var centroid = CalculateCentroid(cellVertices);
                
                // Keep within bounds
                centroid.X = Math.Clamp(centroid.X, 0, width);
                centroid.Y = Math.Clamp(centroid.Y, 0, height);
                
                points[i] = centroid;
            }
        }
    }
    
    return points;
}

/// <summary>
/// Calculates the centroid (geometric center) of a polygon.
/// </summary>
private static Point CalculateCentroid(List<Point> vertices)
{
    if (vertices.Count == 0)
        return Point.Zero;
    
    double x = vertices.Average(v => v.X);
    double y = vertices.Average(v => v.Y);
    
    return new Point(x, y);
}
```

### Integration

```csharp
// src/FantasyMapGenerator.Core/Generators/MapGenerator.cs

public MapData Generate(MapGenerationSettings settings)
{
    // ... existing code ...
    
    // Generate initial points
    var points = settings.GridMode switch
    {
        GridMode.Jittered => GeometryUtils.GenerateJitteredGridPoints(...),
        GridMode.Poisson => GeometryUtils.GeneratePoissonDiskPoints(...),
        _ => GeometryUtils.GeneratePoissonDiskPoints(...)
    };
    
    // NEW: Apply Lloyd relaxation if enabled
    if (settings.ApplyLloydRelaxation)
    {
        Console.WriteLine($"Applying Lloyd relaxation ({settings.LloydIterations} iterations)...");
        points = GeometryUtils.ApplyLloydRelaxation(
            points, 
            settings.Width, 
            settings.Height, 
            settings.LloydIterations);
    }
    
    mapData.Points = points;
    
    // ... rest of generation ...
}
```

### Configuration

```csharp
// src/FantasyMapGenerator.Core/Models/MapGenerationSettings.cs
public class MapGenerationSettings
{
    // Existing properties...
    
    /// <summary>
    /// Apply Lloyd relaxation to improve point distribution
    /// </summary>
    public bool ApplyLloydRelaxation { get; set; } = false;
    
    /// <summary>
    /// Number of Lloyd relaxation iterations (1-3 typical)
    /// </summary>
    public int LloydIterations { get; set; } = 1;
}
```

## Implementation Tasks

### Phase 1: Configuration (15 min)
- [ ] Add Lloyd relaxation settings to `MapGenerationSettings`
- [ ] Set defaults (disabled by default)

### Phase 2: Core Algorithm (1.5 hours)
- [ ] Implement `ApplyLloydRelaxation()` in `GeometryUtils`
- [ ] Implement `CalculateCentroid()` helper
- [ ] Handle edge cases (border cells, degenerate polygons)
- [ ] Add bounds checking

### Phase 3: Integration (30 min)
- [ ] Update `MapGenerator.Generate()` to apply relaxation
- [ ] Add logging
- [ ] Ensure correct ordering (after point generation, before Voronoi)

### Phase 4: Testing (45 min)
- [ ] Unit test: points move toward centroids
- [ ] Unit test: points stay within bounds
- [ ] Unit test: deterministic from seed
- [ ] Performance test: iterations complete quickly
- [ ] Quality test: reduces cell size variance

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void ApplyLloydRelaxation_MovesPointsTowardCentroids()
{
    var points = new List<Point>
    {
        new Point(100, 100),
        new Point(200, 100),
        new Point(150, 200)
    };
    
    var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 1);
    
    // Points should have moved
    Assert.NotEqual(points[0].X, relaxed[0].X, precision: 1);
}

[Fact]
public void ApplyLloydRelaxation_KeepsPointsWithinBounds()
{
    var points = GenerateRandomPoints(100, 800, 600);
    
    var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 3);
    
    Assert.All(relaxed, p =>
    {
        Assert.True(p.X >= 0 && p.X <= 800);
        Assert.True(p.Y >= 0 && p.Y <= 600);
    });
}

[Fact]
public void ApplyLloydRelaxation_IsDeterministic()
{
    var points1 = GeneratePointsWithSeed(12345, 100);
    var points2 = GeneratePointsWithSeed(12345, 100);
    
    var relaxed1 = GeometryUtils.ApplyLloydRelaxation(points1, 800, 600, 2);
    var relaxed2 = GeometryUtils.ApplyLloydRelaxation(points2, 800, 600, 2);
    
    for (int i = 0; i < relaxed1.Count; i++)
    {
        Assert.Equal(relaxed1[i].X, relaxed2[i].X, precision: 5);
        Assert.Equal(relaxed1[i].Y, relaxed2[i].Y, precision: 5);
    }
}
```

### Quality Test

```csharp
[Fact]
public void ApplyLloydRelaxation_ReducesCellSizeVariance()
{
    var points = GenerateRandomPoints(1000, 800, 600);
    
    // Generate Voronoi before relaxation
    var voronoi1 = Voronoi.FromPoints(points.ToArray(), points.Count, 800, 600);
    var areas1 = CalculateCellAreas(voronoi1);
    var variance1 = CalculateVariance(areas1);
    
    // Apply relaxation
    var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 3);
    
    // Generate Voronoi after relaxation
    var voronoi2 = Voronoi.FromPoints(relaxed.ToArray(), relaxed.Count, 800, 600);
    var areas2 = CalculateCellAreas(voronoi2);
    var variance2 = CalculateVariance(areas2);
    
    // Variance should be reduced
    Assert.True(variance2 < variance1 * 0.8, 
        $"Variance should reduce by at least 20% (before: {variance1}, after: {variance2})");
}
```

### Performance Test

```csharp
[Theory]
[InlineData(1000, 1)]
[InlineData(1000, 3)]
[InlineData(5000, 1)]
public void ApplyLloydRelaxation_CompletesInReasonableTime(int pointCount, int iterations)
{
    var points = GenerateRandomPoints(pointCount, 800, 600);
    var stopwatch = Stopwatch.StartNew();
    
    GeometryUtils.ApplyLloydRelaxation(points, 800, 600, iterations);
    
    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 2000 * iterations, 
        $"Relaxation took {stopwatch.ElapsedMilliseconds}ms");
}
```

## Success Criteria

- [ ] Lloyd relaxation improves point distribution
- [ ] Cell size variance reduced by 20-30%
- [ ] Points stay within map bounds
- [ ] Deterministic from seed
- [ ] Performance: <2s per iteration
- [ ] Optional feature (disabled by default)
- [ ] All tests pass

## References

- Reference implementation: `ref-projects/FantasyMapGenerator/Terrain/Terrain.cs` (ImprovePoints method)
- Documentation: `docs/REFERENCE_PROJECT_ANALYSIS.md` (Lloyd Relaxation section)
- Algorithm: Lloyd's algorithm (1982)
- Wikipedia: https://en.wikipedia.org/wiki/Lloyd%27s_algorithm

## Notes

- This is an **optional** feature, disabled by default
- Based on Choochoo's implementation
- Standard technique in procedural generation
- 1-2 iterations usually sufficient
- More iterations = more uniform but slower
- Can be combined with any point generation method
- Particularly useful with jittered grid
- Less necessary with good Poisson disk sampling
