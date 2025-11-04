# Hydrology Implementation Guide

## Overview

This guide details how to implement a **realistic river and lake generation system** for the Fantasy Map Generator port using flow accumulation algorithms.

**Current State**: Placeholder `River` model only
**Target State**: Full hydrology system with rivers, lakes, watersheds, and river widths

---

## Hydrology System Components

### 1. Flow Direction
- Determine downhill neighbor for each cell
- Build directed graph of water flow

### 2. Pit Filling
- Resolve local minima (pits/sinks)
- Create lakes at appropriate locations

### 3. Flow Accumulation
- Calculate upstream drainage area for each cell
- Determines river width and importance

### 4. River Generation
- Extract river paths from high-accumulation cells
- Trace from source to mouth

### 5. Lake Formation
- Identify depression areas
- Calculate lake boundaries and outlets

---

## Algorithm Overview

```
Heightmap
    ↓
Pit Filling (Priority Flood)
    ↓
Flow Direction (D8 / Steepest Descent)
    ↓
Flow Accumulation (Topological Sort)
    ↓
River Extraction (Threshold + Tracing)
    ↓
Lake Identification (Connected Components)
    ↓
River Width Calculation (Log scale from accumulation)
```

---

## Implementation

### Step 1: Flow Direction

**File**: `src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs`

```csharp
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates rivers, lakes, and hydrology features
/// </summary>
public class HydrologyGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;

    // Flow direction: cell ID → downhill neighbor ID (-1 if ocean/pit)
    private Dictionary<int, int> _flowDirection;

    // Flow accumulation: cell ID → upstream cell count
    private Dictionary<int, int> _flowAccumulation;

    public HydrologyGenerator(MapData map, IRandomSource random)
    {
        _map = map;
        _random = random;
        _flowDirection = new Dictionary<int, int>();
        _flowAccumulation = new Dictionary<int, int>();
    }

    public void Generate()
    {
        Console.WriteLine("Generating hydrology...");

        // Step 1: Fill pits to ensure proper drainage
        FillPits();

        // Step 2: Calculate flow directions
        CalculateFlowDirections();

        // Step 3: Calculate flow accumulation
        CalculateFlowAccumulation();

        // Step 4: Generate rivers
        GenerateRivers();

        // Step 5: Identify lakes
        IdentifyLakes();

        // Step 6: Calculate river widths
        CalculateRiverWidths();

        Console.WriteLine($"Generated {_map.Rivers.Count} rivers");
    }

    /// <summary>
    /// Calculate steepest descent direction for each cell
    /// </summary>
    private void CalculateFlowDirections()
    {
        foreach (var cell in _map.Cells)
        {
            if (cell.Height == 0)
            {
                // Ocean cells don't flow anywhere
                _flowDirection[cell.Id] = -1;
                continue;
            }

            int steepestNeighbor = -1;
            int maxDrop = 0;

            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];
                int drop = cell.Height - neighbor.Height;

                if (drop > maxDrop)
                {
                    maxDrop = drop;
                    steepestNeighbor = neighborId;
                }
            }

            _flowDirection[cell.Id] = steepestNeighbor;
        }
    }
}
```

---

### Step 2: Pit Filling (Priority Flood Algorithm)

```csharp
/// <summary>
/// Fill pits to ensure proper drainage (Priority Flood algorithm)
/// Creates lakes at appropriate depressions
/// </summary>
private void FillPits()
{
    // Priority queue: process lowest elevations first
    var queue = new PriorityQueue<int, int>();
    var processed = new HashSet<int>();

    // Seed with ocean and map border cells
    foreach (var cell in _map.Cells)
    {
        if (cell.Height == 0 || cell.IsBorder)
        {
            queue.Enqueue(cell.Id, cell.Height);
        }
    }

    int lakesFilled = 0;

    while (queue.TryDequeue(out var cellId, out var elevation))
    {
        if (processed.Contains(cellId))
            continue;

        processed.Add(cellId);

        var cell = _map.Cells[cellId];

        // Ensure cell is at least as high as its processed neighbors
        if (cell.Height < elevation)
        {
            // This is a pit - fill it to create a lake
            int originalHeight = cell.Height;
            cell.Height = (byte)elevation;

            if (originalHeight < elevation - 2)
            {
                // Significant depression - mark as lake
                lakesFilled++;
            }
        }

        // Process neighbors
        foreach (var neighborId in cell.Neighbors)
        {
            if (!processed.Contains(neighborId))
            {
                var neighbor = _map.Cells[neighborId];
                queue.Enqueue(neighborId, Math.Max(neighbor.Height, cell.Height));
            }
        }
    }

    Console.WriteLine($"Filled {lakesFilled} pits/lakes");
}
```

**Key Points**:
- **Priority Flood**: Processes cells from lowest to highest elevation
- **Ensures drainage**: No local minima (pits) remain
- **Lake creation**: Cells filled by more than threshold become lakes
- **Preserves topography**: Only fills where necessary

---

### Step 3: Flow Accumulation

```csharp
/// <summary>
/// Calculate flow accumulation (upstream drainage area)
/// </summary>
private void CalculateFlowAccumulation()
{
    // Initialize all cells with 1 (self)
    foreach (var cell in _map.Cells)
    {
        _flowAccumulation[cell.Id] = 1;
    }

    // Process in topological order (highest to lowest)
    var sortedCells = TopologicalSort();

    foreach (var cellId in sortedCells)
    {
        var cell = _map.Cells[cellId];

        // Skip ocean cells
        if (cell.Height == 0)
            continue;

        // Get downhill neighbor
        if (_flowDirection.TryGetValue(cellId, out var downhillId) && downhillId >= 0)
        {
            // Add this cell's accumulation to downhill neighbor
            _flowAccumulation[downhillId] += _flowAccumulation[cellId];
        }
    }
}

/// <summary>
/// Sort cells in topological order (highest to lowest elevation)
/// Ensures we process upstream cells before downstream
/// </summary>
private List<int> TopologicalSort()
{
    var sorted = _map.Cells
        .Where(c => c.Height > 0)
        .OrderByDescending(c => c.Height)
        .ThenBy(c => c.Id) // Stable sort
        .Select(c => c.Id)
        .ToList();

    return sorted;
}
```

**Key Points**:
- **Topological Order**: Process upstream → downstream
- **Accumulation**: Each cell accumulates flow from all upstream cells
- **Drainage Area**: Higher accumulation = larger watershed

---

### Step 4: River Generation

```csharp
/// <summary>
/// Generate rivers from high flow accumulation
/// </summary>
private void GenerateRivers()
{
    _map.Rivers = new List<River>();

    // Threshold: minimum accumulation to form a river
    int threshold = (int)(_map.Cells.Count * 0.005); // ~0.5% of cells
    threshold = Math.Max(threshold, 20); // At least 20 cells

    var visited = new HashSet<int>();

    // Find river sources (high accumulation cells)
    var riverSources = _flowAccumulation
        .Where(kvp => kvp.Value >= threshold)
        .Where(kvp => _map.Cells[kvp.Key].Height > 0) // Not ocean
        .OrderByDescending(kvp => kvp.Value)
        .Select(kvp => kvp.Key)
        .Take(100); // Limit number of rivers

    foreach (var sourceId in riverSources)
    {
        if (visited.Contains(sourceId))
            continue;

        var river = TraceRiver(sourceId, visited);

        if (river != null && river.Cells.Count >= 3) // Minimum length
        {
            _map.Rivers.Add(river);
        }
    }
}

/// <summary>
/// Trace a river from source to mouth
/// </summary>
private River? TraceRiver(int sourceId, HashSet<int> visited)
{
    var river = new River
    {
        Id = _map.Rivers.Count,
        Cells = new List<int>(),
        Source = sourceId
    };

    int current = sourceId;
    int maxLength = 1000; // Prevent infinite loops
    int length = 0;

    while (current >= 0 && length < maxLength)
    {
        // Check if already visited
        if (visited.Contains(current))
        {
            // Merge into existing river
            return null;
        }

        var cell = _map.Cells[current];

        // Stop at ocean
        if (cell.Height == 0)
        {
            river.Mouth = current;
            break;
        }

        // Add to river
        river.Cells.Add(current);
        visited.Add(current);
        cell.HasRiver = true;

        // Move to downhill neighbor
        if (_flowDirection.TryGetValue(current, out var downhill))
        {
            current = downhill;
        }
        else
        {
            break;
        }

        length++;
    }

    // Set mouth (last cell before ocean)
    if (river.Cells.Count > 0 && river.Mouth == 0)
    {
        river.Mouth = river.Cells[^1];
    }

    return river;
}
```

**Key Points**:
- **Threshold**: Only cells with sufficient upstream area become rivers
- **Tracing**: Follow flow direction from source to ocean
- **Merging**: Prevent duplicate rivers where tributaries meet
- **Minimum Length**: Filter out tiny streams

---

### Step 5: Lake Identification

```csharp
/// <summary>
/// Identify lake cells (filled depressions)
/// </summary>
private void IdentifyLakes()
{
    // Lakes are flat regions surrounded by higher terrain
    var lakeCandidates = new Dictionary<int, List<int>>(); // elevation → cells

    foreach (var cell in _map.Cells)
    {
        if (cell.Height == 0 || cell.HasRiver)
            continue;

        // Check if surrounded by higher or equal terrain
        bool isPotentialLake = cell.Neighbors.All(nId =>
            _map.Cells[nId].Height >= cell.Height);

        if (isPotentialLake)
        {
            if (!lakeCandidates.ContainsKey(cell.Height))
            {
                lakeCandidates[cell.Height] = new List<int>();
            }

            lakeCandidates[cell.Height].Add(cell.Id);
        }
    }

    // Group connected lake cells at same elevation
    var lakes = new List<List<int>>();

    foreach (var (elevation, cells) in lakeCandidates)
    {
        var remaining = new HashSet<int>(cells);

        while (remaining.Count > 0)
        {
            var seed = remaining.First();
            var lake = FloodFillLake(seed, elevation, remaining);

            if (lake.Count >= 3) // Minimum lake size
            {
                lakes.Add(lake);

                // Mark cells as lake
                foreach (var cellId in lake)
                {
                    _map.Cells[cellId].Feature = -1; // Special value for lake
                }
            }
        }
    }

    Console.WriteLine($"Identified {lakes.Count} lakes");
}

/// <summary>
/// Flood fill to find connected lake cells at same elevation
/// </summary>
private List<int> FloodFillLake(int seedId, int elevation, HashSet<int> remaining)
{
    var lake = new List<int>();
    var queue = new Queue<int>();

    queue.Enqueue(seedId);
    remaining.Remove(seedId);

    while (queue.Count > 0)
    {
        var cellId = queue.Dequeue();
        lake.Add(cellId);

        var cell = _map.Cells[cellId];

        foreach (var neighborId in cell.Neighbors)
        {
            if (remaining.Contains(neighborId))
            {
                var neighbor = _map.Cells[neighborId];

                if (neighbor.Height == elevation)
                {
                    queue.Enqueue(neighborId);
                    remaining.Remove(neighborId);
                }
            }
        }
    }

    return lake;
}
```

---

### Step 6: River Width Calculation

```csharp
/// <summary>
/// Calculate river width based on flow accumulation
/// </summary>
private void CalculateRiverWidths()
{
    foreach (var river in _map.Rivers)
    {
        // Get max accumulation along river
        int maxAccumulation = river.Cells
            .Select(id => _flowAccumulation.GetValueOrDefault(id, 1))
            .Max();

        // Logarithmic scaling for width
        // Small streams: 1-2 units
        // Large rivers: 10-20 units
        double width = Math.Log10(maxAccumulation + 1) * 5;
        river.Width = (int)Math.Clamp(width, 1, 20);

        // Calculate length (approximate)
        river.Length = river.Cells.Count;
    }
}
```

**Width Formula**:
- `width = log₁₀(accumulation + 1) × 5`
- 10 cells → 5 units
- 100 cells → 10 units
- 1,000 cells → 15 units
- 10,000 cells → 20 units

---

## Advanced Features

### 1. River Mouth Delta

```csharp
/// <summary>
/// Create river deltas at mouths (split into multiple channels)
/// </summary>
private void GenerateDeltas()
{
    foreach (var river in _map.Rivers)
    {
        var mouthCell = _map.Cells[river.Mouth];

        // Only large rivers form deltas
        int mouthAccumulation = _flowAccumulation.GetValueOrDefault(river.Mouth, 0);
        if (mouthAccumulation < 500)
            continue;

        // Find coastal cells near mouth
        var coastalCells = FindCoastalCells(mouthCell, radius: 5);

        // Create 2-4 distributary channels
        int channelCount = Math.Min(coastalCells.Count, 2 + mouthAccumulation / 1000);

        for (int i = 0; i < channelCount; i++)
        {
            if (i < coastalCells.Count)
            {
                var channelEnd = coastalCells[i];
                CreateDeltaChannel(mouthCell, channelEnd);
            }
        }
    }
}

private List<Cell> FindCoastalCells(Cell center, int radius)
{
    var coastal = new List<Cell>();
    var visited = new HashSet<int>();
    var queue = new Queue<int>();

    queue.Enqueue(center.Id);
    visited.Add(center.Id);

    for (int depth = 0; depth < radius && queue.Count > 0; depth++)
    {
        int count = queue.Count;

        for (int i = 0; i < count; i++)
        {
            var cellId = queue.Dequeue();
            var cell = _map.Cells[cellId];

            // Check if coastal (land with ocean neighbor)
            if (cell.Height > 0 && cell.Neighbors.Any(n => _map.Cells[n].Height == 0))
            {
                coastal.Add(cell);
            }

            // Expand search
            foreach (var neighborId in cell.Neighbors)
            {
                if (!visited.Contains(neighborId))
                {
                    queue.Enqueue(neighborId);
                    visited.Add(neighborId);
                }
            }
        }
    }

    return coastal;
}
```

---

### 2. Seasonal Rivers (Intermittent Streams)

```csharp
/// <summary>
/// Mark rivers as seasonal based on climate
/// </summary>
private void IdentifySeasonalRivers()
{
    foreach (var river in _map.Rivers)
    {
        // Calculate average precipitation along river
        double avgPrecipitation = river.Cells
            .Select(id => _map.Cells[id].Precipitation)
            .Average();

        // Low precipitation = seasonal river
        river.IsSeasonal = avgPrecipitation < 30;

        // Adjust width for seasonal rivers
        if (river.IsSeasonal)
        {
            river.Width = (int)(river.Width * 0.5);
        }
    }
}
```

---

### 3. River Names (Based on Length/Importance)

```csharp
/// <summary>
/// Generate names for major rivers
/// </summary>
private void GenerateRiverNames(IRandomSource random)
{
    // Name prefixes/suffixes
    var prefixes = new[] { "River", "Great", "Little", "North", "South", "East", "West" };
    var names = new[] { "Alder", "Birch", "Cedar", "Dale", "Elm", "Fern", "Glen", "Hazel" };
    var suffixes = new[] { "water", "stream", "flow", "rush", "brook" };

    // Sort rivers by length (name longest first)
    var sortedRivers = _map.Rivers
        .OrderByDescending(r => r.Length)
        .ThenByDescending(r => r.Width)
        .ToList();

    for (int i = 0; i < Math.Min(sortedRivers.Count, 20); i++)
    {
        var river = sortedRivers[i];

        if (river.Width >= 5)
        {
            // Major river: "Great River" or "Riverdale"
            river.Name = $"{prefixes[random.Next(prefixes.Length)]} {names[random.Next(names.Length)]}";
        }
        else
        {
            // Minor river: "Aldwater" or "Fernbrook"
            river.Name = $"{names[random.Next(names.Length)]}{suffixes[random.Next(suffixes.Length)]}";
        }
    }
}
```

---

## River Data Model Updates

Update `src/FantasyMapGenerator.Core/Models/River.cs`:

```csharp
namespace FantasyMapGenerator.Core.Models;

public class River
{
    public int Id { get; set; }

    /// <summary>
    /// Source cell (highest point)
    /// </summary>
    public int Source { get; set; }

    /// <summary>
    /// Mouth cell (where it enters ocean/lake)
    /// </summary>
    public int Mouth { get; set; }

    /// <summary>
    /// Cells along river path (source → mouth)
    /// </summary>
    public List<int> Cells { get; set; } = new();

    /// <summary>
    /// River width (visual representation)
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// River length (number of cells)
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// River name (optional, for major rivers)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Is this a seasonal/intermittent river?
    /// </summary>
    public bool IsSeasonal { get; set; }

    /// <summary>
    /// Parent river ID (for tributaries)
    /// </summary>
    public int? ParentRiver { get; set; }

    /// <summary>
    /// Tributary IDs
    /// </summary>
    public List<int> Tributaries { get; set; } = new();
}
```

---

## Integration with MapGenerator

Update `src/FantasyMapGenerator.Core/MapGenerator.cs`:

```csharp
public MapData Generate(MapGenerationSettings settings)
{
    // ... existing code ...

    var hydrologyRng = CreateChildRng(rootRng, 4);

    // Generate terrain
    GenerateRandomPoints(mapData, terrainRng);
    GenerateSimpleHeightmap(mapData, settings, terrainRng);
    CreateSimpleCells(mapData);
    ApplyHeightsToCells(mapData);

    // Generate climate (needs terrain)
    GenerateBiomes(mapData, climateRng);

    // Generate hydrology (needs terrain + climate)
    var hydrologyGenerator = new HydrologyGenerator(mapData, hydrologyRng);
    hydrologyGenerator.Generate();

    // Generate political (can use rivers as borders)
    GenerateBasicStates(mapData, settings, politicalRng);

    return mapData;
}
```

---

## Rendering Rivers

Update `src/FantasyMapGenerator.Rendering/MapRenderer.cs`:

```csharp
using SkiaSharp;

public void RenderRivers(SKCanvas canvas, MapData map)
{
    foreach (var river in map.Rivers)
    {
        // Build path from cell centers
        using var path = new SKPath();

        for (int i = 0; i < river.Cells.Count; i++)
        {
            var cell = map.Cells[river.Cells[i]];

            if (i == 0)
            {
                path.MoveTo((float)cell.Center.X, (float)cell.Center.Y);
            }
            else
            {
                path.LineTo((float)cell.Center.X, (float)cell.Center.Y);
            }
        }

        // Smooth path with cubic curves (optional)
        // path = SmoothPath(path);

        // Draw river
        using var paint = new SKPaint
        {
            Color = river.IsSeasonal
                ? SKColors.LightBlue.WithAlpha(150)
                : SKColors.Blue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = river.Width,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        canvas.DrawPath(path, paint);

        // Draw river name (if major river)
        if (!string.IsNullOrEmpty(river.Name) && river.Width >= 5)
        {
            DrawTextAlongPath(canvas, river.Name, path);
        }
    }
}

private void DrawTextAlongPath(SKCanvas canvas, string text, SKPath path)
{
    using var paint = new SKPaint
    {
        Color = SKColors.DarkBlue,
        TextSize = 12,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic)
    };

    // Position text at middle of path
    float pathLength = path.Length;
    float textWidth = paint.MeasureText(text);

    if (textWidth < pathLength * 0.8f)
    {
        float offset = (pathLength - textWidth) / 2;
        canvas.DrawTextOnPath(text, path, offset, -5, paint);
    }
}
```

---

## Testing

### Unit Tests

```csharp
using Xunit;

public class HydrologyGeneratorTests
{
    [Fact]
    public void FlowDirection_WaterFlowsDownhill()
    {
        var map = CreateTestMap();
        var generator = new HydrologyGenerator(map, new PcgRandomSource(42));

        generator.Generate();

        // Check that each cell flows to a lower neighbor (or ocean)
        foreach (var cell in map.Cells.Where(c => c.Height > 0))
        {
            var flowDir = GetFlowDirection(cell, map);

            if (flowDir >= 0)
            {
                var downstream = map.Cells[flowDir];
                Assert.True(downstream.Height <= cell.Height,
                    "Water should flow downhill");
            }
        }
    }

    [Fact]
    public void FlowAccumulation_SourceHasMinimum()
    {
        var map = CreateTestMap();
        var generator = new HydrologyGenerator(map, new PcgRandomSource(42));

        generator.Generate();

        // Source cells (no upstream) should have accumulation = 1
        var sources = map.Cells.Where(c =>
            c.Height > 0 &&
            !c.Neighbors.Any(n => FlowsInto(map.Cells[n], c, map)));

        foreach (var source in sources)
        {
            var accumulation = GetFlowAccumulation(source, map);
            Assert.Equal(1, accumulation);
        }
    }

    [Fact]
    public void Rivers_FlowToOcean()
    {
        var map = CreateTestMap();
        var generator = new HydrologyGenerator(map, new PcgRandomSource(42));

        generator.Generate();

        foreach (var river in map.Rivers)
        {
            var mouthCell = map.Cells[river.Mouth];

            // Mouth should be ocean or have ocean neighbor
            bool hasOceanAccess =
                mouthCell.Height == 0 ||
                mouthCell.Neighbors.Any(n => map.Cells[n].Height == 0);

            Assert.True(hasOceanAccess, "River should flow to ocean");
        }
    }

    [Fact]
    public void PitFilling_RemovesLocalMinima()
    {
        var map = CreateTestMap();
        var generator = new HydrologyGenerator(map, new PcgRandomSource(42));

        generator.Generate();

        // After pit filling, every land cell should have a downhill path to ocean
        foreach (var cell in map.Cells.Where(c => c.Height > 0))
        {
            bool hasPathToOcean = TracePath(cell, map, maxSteps: 1000);
            Assert.True(hasPathToOcean, "Cell should have path to ocean");
        }
    }
}
```

---

## Performance Optimization

### 1. Parallel Flow Direction Calculation

```csharp
private void CalculateFlowDirectionsParallel()
{
    var landCells = _map.Cells.Where(c => c.Height > 0).ToArray();

    Parallel.ForEach(landCells, cell =>
    {
        int steepestNeighbor = -1;
        int maxDrop = 0;

        foreach (var neighborId in cell.Neighbors)
        {
            var neighbor = _map.Cells[neighborId];
            int drop = cell.Height - neighbor.Height;

            if (drop > maxDrop)
            {
                maxDrop = drop;
                steepestNeighbor = neighborId;
            }
        }

        lock (_flowDirection)
        {
            _flowDirection[cell.Id] = steepestNeighbor;
        }
    });
}
```

### 2. Sparse Flow Accumulation

```csharp
// Only store non-trivial accumulations (> 1)
private Dictionary<int, int> _flowAccumulation = new();

private int GetAccumulation(int cellId)
{
    return _flowAccumulation.GetValueOrDefault(cellId, 1);
}
```

---

## Validation & Debugging

### Visual Debug Overlays

```csharp
public void RenderDebugHydrology(SKCanvas canvas, MapData map)
{
    // Draw flow directions as arrows
    foreach (var cell in map.Cells.Where(c => c.Height > 0))
    {
        var flowDir = GetFlowDirection(cell, map);
        if (flowDir >= 0)
        {
            var downstream = map.Cells[flowDir];
            DrawArrow(canvas, cell.Center, downstream.Center);
        }
    }

    // Draw flow accumulation as heatmap
    int maxAccumulation = GetMaxAccumulation(map);

    foreach (var cell in map.Cells)
    {
        int accumulation = GetAccumulation(cell);
        float intensity = (float)accumulation / maxAccumulation;

        var color = SKColor.FromHsl(240 - intensity * 120, 100, 50);
        FillCell(canvas, cell, color);
    }
}
```

---

## Resources

- [Priority Flood Algorithm](https://doi.org/10.1016/j.cageo.2013.04.024) - Original paper
- [Flow Direction D8 vs D-Infinity](https://desktop.arcgis.com/en/arcmap/latest/tools/spatial-analyst-toolbox/how-flow-direction-works.htm)
- [River Width Scaling Laws](https://agupubs.onlinelibrary.wiley.com/doi/full/10.1002/2013WR014246)
- [Watershed Delineation](https://pro.arcgis.com/en/pro-app/latest/tool-reference/spatial-analyst/how-watershed-works.htm)

---

**Last Updated**: 2025-11-04
**Related**: library-adoption-roadmap.md, deterministic-seeding-guide.md, geometry-operations-guide.md
