# Library Adoption Roadmap

## Executive Summary

This document outlines how to adopt modern .NET libraries recommended for the Fantasy Map Generator port, improving performance, robustness, and maintainability while maintaining compatibility with the original Azgaar's Fantasy Map Generator data model.

**Current Status**: The project has a solid foundation with custom implementations of Delaunay triangulation and Voronoi diagrams, basic heightmap generation, and biome assignment. However, it lacks:
- Standardized noise generation
- Robust geometry operations
- Deterministic seeding across all subsystems
- River/hydrology systems
- Vector tile export capabilities

**Goal**: Incrementally adopt industry-standard libraries to accelerate development while maintaining the current architecture.

---

## Current Implementation Analysis

### What's Working Well ✅

1. **Custom Delaunator.cs**: Solid C# port of the JavaScript library
   - Handles 2D Delaunay triangulation efficiently
   - Maintains halfedge data structure for dual graph
   - Compatible with original FMG approach

2. **Voronoi.cs**: Clean abstraction over Delaunator
   - Computes Voronoi cells from Delaunay triangulation
   - Stores vertices, neighbors, borders
   - Good separation of concerns

3. **Data Model**: Matches FMG export schema
   - Cells, vertices, edges
   - Features (burgs, states, cultures, biomes)
   - Enables future compatibility with ecosystem tools

4. **Modular Architecture**:
   - Core library (no UI dependencies)
   - Separate rendering (SkiaSharp)
   - Multiple UI options (Avalonia)

### What Needs Improvement ⚠️

1. **Noise Generation**:
   - Current: Simple random noise and blob/line primitives
   - Missing: Perlin, Simplex, Cellular, Fractal/octave noise
   - Impact: Limited terrain variety and realism

2. **Geometry Operations**:
   - Current: Basic polygon area, centroid, point-in-polygon
   - Missing: Robust clipping, buffering, intersection, simplification
   - Impact: Can't do advanced operations (e.g., rain shadows, border smoothing)

3. **Randomization**:
   - Current: Mix of `new Random(seed)` and `Random.Shared`
   - Problem: Not fully deterministic from seed
   - Impact: Can't reproduce maps reliably

4. **Hydrology**:
   - Current: Placeholder data structures only
   - Missing: Flow accumulation, watershed analysis, river generation
   - Impact: Major feature gap vs. original FMG

5. **Vector Export**:
   - Current: None
   - Missing: MVT (Mapbox Vector Tiles) for zoom-level rendering
   - Impact: Can't export for web viewers or Foundry VTT

---

## Recommended Libraries

### 1. Voronoi/Delaunay Triangulation

**Current**: Custom `Delaunator.cs` (port from JS)

**Options**:

| Library | Pros | Cons | Recommendation |
|---------|------|------|----------------|
| **Keep Current** | Already working, no breaking changes, known behavior | No maintenance, limited features | ✅ **KEEP for now** |
| [Delaunator-sharp](https://github.com/nol1fe/delaunator-sharp) | Official C# port, actively maintained, NuGet package | Essentially same as current implementation | Consider for future maintenance |
| [Triangle.NET](https://www.nuget.org/packages/Triangle.NET) | Robust, constrained Delaunay, refines meshes | Heavier, more complex API | Overkill for current needs |

**Decision**: **Keep current Delaunator.cs**
- It's working correctly
- Matches original FMG approach (d3-delaunay → Delaunator)
- Minimal migration cost
- Can swap to Delaunator-sharp later for maintenance if needed

**Action Items**: None immediately; consider switching to NuGet package in future for updates

---

### 2. Noise Generation

**Current**: Template-based DSL (blob, line, smooth, noise)

**Recommended**: [FastNoiseLite](https://github.com/Auburn/FastNoiseLite)

**Why**:
- Single-file drop-in (or NuGet if available)
- Supports Perlin, Simplex, Cellular, Value, and more
- Fractal/octave layering built-in
- Domain warping for organic terrain
- Highly optimized
- Deterministic from seed

**Migration Path**:

**Phase 1 - Add alongside existing** (Low risk):
```csharp
// Keep HeightmapGenerator.cs template system
// Add new FastNoiseHeightmapGenerator.cs

public class FastNoiseHeightmapGenerator
{
    private readonly FastNoiseLite _noise;

    public FastNoiseHeightmapGenerator(int seed)
    {
        _noise = new FastNoiseLite(seed);
        _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        _noise.SetFractalOctaves(5);
    }

    public byte[] Generate(int width, int height, double scale)
    {
        var heightmap = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)width - 0.5f;
                float ny = y / (float)height - 0.5f;

                float value = _noise.GetNoise(nx * scale, ny * scale);
                // Map from [-1, 1] to [0, 100]
                heightmap[y * width + x] = (byte)((value + 1) * 50);
            }
        }
        return heightmap;
    }
}
```

**Phase 2 - Hybrid approach** (Medium risk):
```csharp
// Enhance template DSL with noise commands
// Template: "noise perlin amplitude=50 octaves=5 scale=2.5"

public class EnhancedHeightmapGenerator
{
    private readonly Dictionary<string, FastNoiseLite> _noiseGenerators;

    public void ProcessNoiseCommand(string command, byte[] heightmap)
    {
        // Parse: "noise perlin amplitude=50 octaves=5"
        var parts = command.Split(' ');
        var noiseType = parts[1]; // "perlin", "simplex", "cellular"
        var amplitude = ParseParam("amplitude", parts);
        var octaves = ParseParam("octaves", parts);

        var noise = GetOrCreateNoise(noiseType, octaves);

        // Apply to heightmap...
    }
}
```

**Phase 3 - Full replacement** (Future):
- Deprecate blob/line primitives
- Use noise + domain warping for all features
- Keep template system for combining noise layers

**Files to Modify**:
- `src/FantasyMapGenerator.Core/Generators/HeightmapGenerator.cs` (keep)
- `src/FantasyMapGenerator.Core/Generators/FastNoiseHeightmapGenerator.cs` (new)
- `src/FantasyMapGenerator.Core/MapGenerator.cs` (add option to choose generator)

**See**: [noise-generation-guide.md](./noise-generation-guide.md) for detailed implementation

---

### 3. Geometry Operations

**Current**: Basic utilities in `GeometryUtils.cs`

**Recommended**: [NetTopologySuite (NTS)](https://www.nuget.org/packages/NetTopologySuite/)

**Why**:
- Industry-standard JTS port
- Robust polygon operations (buffer, clip, intersect, union, difference)
- Line simplification (Douglas-Peucker, Visvalingam)
- Topology validation and repair
- Spatial indexing (STRtree for fast lookups)
- Well-tested, production-ready

**Use Cases in FMG**:

| Feature | Current | With NTS |
|---------|---------|----------|
| Border smoothing | Manual averaging | `Geometry.Buffer().Simplify()` |
| State boundaries | Cell neighbor traversal | `Polygon.Union()` on cell geometries |
| Rain shadows | Not implemented | `LineString.Buffer().Intersection(polygon)` |
| River valleys | Not implemented | `LineString.Buffer(width).Difference(terrain)` |
| Coast lines | Cell border detection | `Polygon.Boundary.Simplify()` |
| Territory expansion | Manual growth | `Geometry.Buffer(distance)` |

**Migration Path**:

**Phase 1 - Additive wrapper** (Low risk):
```csharp
// Create NtsGeometryAdapter.cs
public class NtsGeometryAdapter
{
    private readonly GeometryFactory _factory = new GeometryFactory();

    public Polygon CellToPolygon(Cell cell, List<Point> vertices)
    {
        var coords = cell.Vertices
            .Select(i => vertices[i])
            .Select(p => new Coordinate(p.X, p.Y))
            .ToArray();

        // Close the ring
        if (!coords[0].Equals(coords[^1]))
        {
            coords = coords.Append(coords[0]).ToArray();
        }

        return _factory.CreatePolygon(coords);
    }

    public List<Polygon> GetStateBoundary(int stateId, MapData map)
    {
        var cells = map.GetStateCells(stateId);
        var polygons = cells.Select(c => CellToPolygon(c, map.Vertices));

        var union = UnaryUnionOp.Union(polygons);
        // Returns simplified, merged state boundary
        return ExtractPolygons(union);
    }
}
```

**Phase 2 - Integrate into generators** (Medium risk):
```csharp
// Use in BiomeGenerator for smoothing
public void SmoothBiomeBoundaries(MapData map)
{
    var adapter = new NtsGeometryAdapter();

    foreach (var biome in map.Biomes)
    {
        var cells = map.GetBiomeCells(biome.Id);
        var region = adapter.UnionCells(cells, map.Vertices);

        // Smooth and simplify
        var smoothed = region.Buffer(0.5).Buffer(-0.5); // Morphological smoothing
        var simplified = DouglasPeuckerSimplifier.Simplify(smoothed, 0.1);

        // Update cells based on simplified boundary...
    }
}
```

**Phase 3 - Spatial indexing** (Performance):
```csharp
// Use STRtree for fast cell lookups
public class SpatialMapData : MapData
{
    private readonly STRtree<Cell> _cellIndex;

    public SpatialMapData()
    {
        _cellIndex = new STRtree<Cell>();
    }

    public void BuildIndex()
    {
        foreach (var cell in Cells)
        {
            var envelope = GetCellEnvelope(cell);
            _cellIndex.Insert(envelope, cell);
        }
    }

    public IEnumerable<Cell> QueryRadius(Point center, double radius)
    {
        var envelope = new Envelope(
            center.X - radius, center.X + radius,
            center.Y - radius, center.Y + radius);

        return _cellIndex.Query(envelope);
    }
}
```

**Files to Create**:
- `src/FantasyMapGenerator.Core/Geometry/NtsGeometryAdapter.cs`
- `src/FantasyMapGenerator.Core/Geometry/SpatialIndexing.cs`

**Files to Modify**:
- `src/FantasyMapGenerator.Core/Generators/BiomeGenerator.cs` (smoothing)
- `src/FantasyMapGenerator.Core/Generators/StateGenerator.cs` (boundaries)
- `src/FantasyMapGenerator.Core/Models/MapData.cs` (optional spatial indexing)

**See**: [geometry-operations-guide.md](./geometry-operations-guide.md) for detailed implementation

---

### 4. Deterministic Seeding / RNG

**Current**: Mix of `Random(seed)` and `Random.Shared`

**Recommended**: PCG (Permuted Congruential Generator)

**NuGet**: [Pcg](https://www.nuget.org/packages/Pcg/) or custom implementation

**Why**:
- Reproducible across platforms (.NET Framework, .NET Core, Mono)
- Better statistical properties than System.Random
- Fast (competitive with System.Random)
- Multiple streams from single seed

**Current Issues**:

```csharp
// MapGenerator.cs
var random = new Random((int)settings.Seed); // ❌ Loses precision (long → int)
GenerateRandomPoints(random); // ✅ Uses passed RNG

// HeightmapGenerator.cs
public void AddNoise(byte[] heightmap, byte amplitude)
{
    // ❌ Uses Random.Shared - not deterministic!
    var value = (byte)Random.Shared.Next(-amplitude, amplitude + 1);
}

// BiomeGenerator.cs
private double CalculateTemperature(Cell cell, double equator, double mapHeight)
{
    double randomFactor = Random.Shared.NextDouble() * 0.1 - 0.05; // ❌ Not seeded!
}
```

**Migration Path**:

**Phase 1 - Create RNG abstraction** (Low risk):
```csharp
// src/FantasyMapGenerator.Core/Random/IRandomSource.cs
public interface IRandomSource
{
    int Next();
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
    void NextBytes(byte[] buffer);
}

// src/FantasyMapGenerator.Core/Random/SystemRandomSource.cs
public class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public int Next() => _random.Next();
    // ... implement interface
}

// src/FantasyMapGenerator.Core/Random/PcgRandomSource.cs
public class PcgRandomSource : IRandomSource
{
    private readonly Pcg.Pcg _pcg;

    public PcgRandomSource(ulong seed)
    {
        _pcg = new Pcg.Pcg(seed);
    }

    public int Next() => _pcg.Next();
    // ... implement interface
}
```

**Phase 2 - Thread RNG through generators** (Medium risk):
```csharp
// Update all generators to take IRandomSource
public class HeightmapGenerator
{
    // OLD: Uses Random.Shared
    public void AddNoise(byte[] heightmap, byte amplitude)
    {
        var value = (byte)Random.Shared.Next(-amplitude, amplitude + 1);
    }

    // NEW: Uses injected RNG
    public void AddNoise(byte[] heightmap, byte amplitude, IRandomSource random)
    {
        var value = (byte)random.Next(-amplitude, amplitude + 1);
    }
}

// MapGenerator orchestrates
public class MapGenerator
{
    public MapData Generate(MapGenerationSettings settings)
    {
        // Use full long seed
        var random = new PcgRandomSource((ulong)settings.Seed);

        // Create child RNGs for subsystems (different streams)
        var terrainRng = CreateChild(random, 1);
        var climateRng = CreateChild(random, 2);
        var politicalRng = CreateChild(random, 3);

        GenerateHeightmap(terrainRng);
        GenerateBiomes(climateRng);
        GenerateStates(politicalRng);
    }
}
```

**Phase 3 - Validation** (Critical):
```csharp
// Test that same seed produces identical maps
[Fact]
public void SameSeed_ProducesIdenticalMaps()
{
    var settings1 = new MapGenerationSettings { Seed = 12345 };
    var settings2 = new MapGenerationSettings { Seed = 12345 };

    var map1 = new MapGenerator().Generate(settings1);
    var map2 = new MapGenerator().Generate(settings2);

    // Compare all cells, heights, biomes, etc.
    Assert.Equal(map1.Cells.Count, map2.Cells.Count);
    for (int i = 0; i < map1.Cells.Count; i++)
    {
        Assert.Equal(map1.Cells[i].Height, map2.Cells[i].Height);
        Assert.Equal(map1.Cells[i].Biome, map2.Cells[i].Biome);
    }
}
```

**Files to Create**:
- `src/FantasyMapGenerator.Core/Random/IRandomSource.cs`
- `src/FantasyMapGenerator.Core/Random/SystemRandomSource.cs`
- `src/FantasyMapGenerator.Core/Random/PcgRandomSource.cs`

**Files to Modify**:
- `src/FantasyMapGenerator.Core/Generators/HeightmapGenerator.cs`
- `src/FantasyMapGenerator.Core/Generators/BiomeGenerator.cs`
- `src/FantasyMapGenerator.Core/Generators/StateGenerator.cs`
- `src/FantasyMapGenerator.Core/MapGenerator.cs`
- `tests/FantasyMapGenerator.Core.Tests/*` (add reproducibility tests)

**See**: [deterministic-seeding-guide.md](./deterministic-seeding-guide.md) for detailed implementation

---

### 5. Hydrology (NEW - Not Yet Implemented)

**Current**: Placeholder `River` model only

**Recommended Approach**: Custom implementation using existing data structures

**Algorithm Overview**:

1. **Flow Direction**: For each cell, find downhill neighbor
   ```csharp
   private int FindDownhillNeighbor(Cell cell, MapData map)
   {
       int steepest = -1;
       int maxDrop = 0;

       foreach (var neighborId in cell.Neighbors)
       {
           var neighbor = map.Cells[neighborId];
           int drop = cell.Height - neighbor.Height;

           if (drop > maxDrop)
           {
               maxDrop = drop;
               steepest = neighborId;
           }
       }

       return steepest; // -1 if local minimum (pit)
   }
   ```

2. **Pit Filling**: Resolve local minima (create lakes)
   ```csharp
   private void FillPits(MapData map)
   {
       // Priority queue: process lowest elevations first
       var queue = new PriorityQueue<int, int>();
       var processed = new HashSet<int>();

       // Seed with ocean cells
       foreach (var cell in map.Cells.Where(c => c.Height == 0))
       {
           queue.Enqueue(cell.Id, 0);
       }

       while (queue.TryDequeue(out var cellId, out var elevation))
       {
           if (processed.Contains(cellId)) continue;
           processed.Add(cellId);

           var cell = map.Cells[cellId];

           // Ensure cell is at least as high as its processed neighbors
           if (cell.Height < elevation)
           {
               cell.Height = (byte)elevation; // Fill the pit
           }

           // Process uphill neighbors
           foreach (var neighborId in cell.Neighbors)
           {
               if (!processed.Contains(neighborId))
               {
                   var neighbor = map.Cells[neighborId];
                   queue.Enqueue(neighborId, neighbor.Height);
               }
           }
       }
   }
   ```

3. **Flow Accumulation**: Count upstream cells
   ```csharp
   private Dictionary<int, int> CalculateFlowAccumulation(MapData map)
   {
       var accumulation = new Dictionary<int, int>();
       var sorted = TopologicalSort(map); // Highest to lowest

       foreach (var cellId in sorted)
       {
           var cell = map.Cells[cellId];
           var flow = accumulation.GetValueOrDefault(cellId, 1); // Start with self

           var downhill = FindDownhillNeighbor(cell, map);
           if (downhill >= 0)
           {
               accumulation[downhill] = accumulation.GetValueOrDefault(downhill, 1) + flow;
           }

           accumulation[cellId] = flow;
       }

       return accumulation;
   }
   ```

4. **River Generation**: Extract paths with high accumulation
   ```csharp
   private List<River> GenerateRivers(MapData map, Dictionary<int, int> flowAccumulation, int threshold)
   {
       var rivers = new List<River>();
       var visited = new HashSet<int>();

       // Start from high-accumulation cells
       var sources = flowAccumulation
           .Where(kvp => kvp.Value >= threshold)
           .OrderByDescending(kvp => kvp.Value)
           .Select(kvp => kvp.Key);

       foreach (var sourceId in sources)
       {
           if (visited.Contains(sourceId)) continue;

           var river = new River { Cells = new List<int>() };
           var current = sourceId;

           // Trace downhill until ocean or existing river
           while (current >= 0 && !visited.Contains(current))
           {
               river.Cells.Add(current);
               visited.Add(current);
               map.Cells[current].HasRiver = true;

               current = FindDownhillNeighbor(map.Cells[current], map);
           }

           if (river.Cells.Count >= 3) // Minimum length
           {
               river.Source = river.Cells[0];
               river.Mouth = river.Cells[^1];
               rivers.Add(river);
           }
       }

       return rivers;
   }
   ```

**Libraries Needed**: None (use existing data structures)

**Optional Enhancement**: Use [QuikGraph](https://www.nuget.org/packages/QuikGraph/) for graph algorithms
- Dijkstra for pathfinding
- Topological sort for flow order
- Strongly connected components for watersheds

**Files to Create**:
- `src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs`

**Files to Modify**:
- `src/FantasyMapGenerator.Core/MapGenerator.cs` (add hydrology step)
- `src/FantasyMapGenerator.Core/Models/River.cs` (add width calculation)

**See**: [hydrology-implementation-guide.md](./hydrology-implementation-guide.md) for detailed implementation

---

### 6. Vector Tile Export (NEW - Optional)

**Current**: None

**Recommended**: [NetTopologySuite.IO.VectorTiles](https://www.nuget.org/packages/NetTopologySuite.IO.VectorTiles/)

**Why**:
- Export maps as Mapbox Vector Tiles (MVT)
- Enables web viewers (Mapbox GL JS, MapLibre)
- Zoom-level styling
- Integration with Foundry VTT and other tools

**Use Case**: Export map for web viewer or game engine

**Example**:
```csharp
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;

public class VectorTileExporter
{
    public void ExportMap(MapData map, string outputDir)
    {
        var tree = new VectorTileTree(256); // Tile size

        // Add terrain layer
        var terrainLayer = new Layer { Name = "terrain" };
        foreach (var cell in map.Cells)
        {
            var polygon = CellToNtsPolygon(cell, map.Vertices);
            var feature = new Feature(polygon, new AttributesTable
            {
                { "height", cell.Height },
                { "biome", cell.Biome }
            });
            terrainLayer.Features.Add(feature);
        }

        // Add river layer
        var riverLayer = new Layer { Name = "rivers" };
        foreach (var river in map.Rivers)
        {
            var lineString = RiverToNtsLineString(river, map);
            var feature = new Feature(lineString, new AttributesTable
            {
                { "width", river.Width }
            });
            riverLayer.Features.Add(feature);
        }

        // Write tiles
        tree.Add(terrainLayer);
        tree.Add(riverLayer);

        var tiles = tree.Build();
        foreach (var tile in tiles)
        {
            var mvt = MapboxTileWriter.Write(tile);
            File.WriteAllBytes($"{outputDir}/{tile.Zoom}/{tile.X}/{tile.Y}.mvt", mvt);
        }
    }
}
```

**Decision**: Low priority - implement after core features (rivers, advanced terrain) are complete

**Files to Create** (future):
- `src/FantasyMapGenerator.Export/VectorTileExporter.cs`

---

## Migration Plan

### Milestone 0: Deterministic Seeding (CRITICAL) 🔴
**Priority**: High
**Risk**: Low
**Effort**: 1-2 days

**Goals**:
- Full reproducibility from seed
- Cross-platform consistency

**Steps**:
1. Create `IRandomSource` abstraction
2. Implement `SystemRandomSource` (backwards compatible)
3. Thread RNG through all generators (remove `Random.Shared`)
4. Add reproducibility tests
5. (Optional) Add PCG implementation

**Validation**:
```bash
# Generate same map 100 times, verify identical
dotnet test --filter "Category=Reproducibility"
```

**Files**: See "Deterministic Seeding" section above

---

### Milestone 1: Noise Generation 🟡
**Priority**: Medium
**Risk**: Low
**Effort**: 2-3 days

**Goals**:
- Richer terrain generation
- More natural-looking heightmaps
- Fractal/octave noise for detail

**Steps**:
1. Add FastNoiseLite (single file or NuGet)
2. Create `FastNoiseHeightmapGenerator` (alongside existing)
3. Add UI option to choose generator
4. Extend template DSL with noise commands (optional)

**Validation**:
- Visual comparison: template-based vs noise-based heightmaps
- Ensure both are deterministic

**Files**: See "Noise Generation" section above

---

### Milestone 2: Hydrology 🔵
**Priority**: High (major feature gap)
**Risk**: Medium
**Effort**: 4-5 days

**Goals**:
- Rivers generated from flow accumulation
- Lakes at local minima
- River width based on accumulation

**Steps**:
1. Implement pit filling
2. Implement flow direction
3. Implement flow accumulation
4. Generate rivers from threshold
5. Add river rendering

**Validation**:
- Rivers flow downhill
- Rivers don't cross watersheds
- Rivers connect to ocean

**Files**: See "Hydrology" section above

---

### Milestone 3: Geometry Operations 🟢
**Priority**: Low (nice-to-have)
**Risk**: Medium (API complexity)
**Effort**: 3-4 days

**Goals**:
- Robust polygon operations
- Border smoothing
- State boundary generation

**Steps**:
1. Add NTS NuGet package
2. Create `NtsGeometryAdapter` wrapper
3. Implement border smoothing in BiomeGenerator
4. Implement state boundary generation
5. (Optional) Add spatial indexing

**Validation**:
- Smoothed borders don't create gaps/overlaps
- State boundaries match cell membership

**Files**: See "Geometry Operations" section above

---

### Milestone 4: Vector Tile Export 🟣
**Priority**: Low (optional)
**Risk**: Low
**Effort**: 2-3 days

**Goals**:
- Export maps as MVT
- Enable web viewer integration

**Steps**:
1. Add NTS.IO.VectorTiles NuGet package
2. Create `VectorTileExporter`
3. Export terrain, rivers, borders, burgs
4. (Optional) Create web viewer demo

**Validation**:
- MVT tiles load in Mapbox GL JS
- Layers match original map

**Files**: See "Vector Tile Export" section above

---

## Summary: Library Adoption Strategy

| Library | Adopt? | When | Risk | Benefit |
|---------|--------|------|------|---------|
| **Delaunator.cs** | ✅ Keep | N/A | None | Already working |
| **FastNoiseLite** | ✅ Yes | M1 | Low | Much better terrain |
| **NetTopologySuite** | ✅ Yes | M3 | Medium | Robust geometry ops |
| **PCG RNG** | ✅ Yes | M0 | Low | Cross-platform seeding |
| **Custom Hydrology** | ✅ Yes | M2 | Medium | Core feature parity |
| **NTS VectorTiles** | ⏸️ Maybe | M4 | Low | Export capability |

---

## Next Steps

1. **Read detailed guides**:
   - [deterministic-seeding-guide.md](./deterministic-seeding-guide.md)
   - [noise-generation-guide.md](./noise-generation-guide.md)
   - [hydrology-implementation-guide.md](./hydrology-implementation-guide.md)
   - [geometry-operations-guide.md](./geometry-operations-guide.md)

2. **Start with M0 (Seeding)**:
   - Most critical for development (enables testing)
   - Lowest risk (pure refactor)
   - Enables all future work

3. **Proceed incrementally**:
   - Don't adopt all libraries at once
   - Validate each milestone before moving on
   - Keep backwards compatibility where possible

4. **Measure impact**:
   - Performance benchmarks (generation time)
   - Visual quality comparisons (screenshots)
   - Reproducibility tests (same seed = same map)

---

## Questions / Decisions Needed

1. **PCG vs System.Random**: Do you need cross-platform reproducibility? (Recommendation: Yes, use PCG)

2. **FastNoiseLite integration**: Replace template system or extend it? (Recommendation: Extend - keep templates for backward compatibility)

3. **NTS adoption timeline**: Before or after hydrology? (Recommendation: After - focus on core features first)

4. **Vector tile export**: Required or nice-to-have? (Recommendation: Nice-to-have - defer to M4)

5. **Migration pace**: All at once or incremental? (Recommendation: Incremental - milestone by milestone)

---

**Last Updated**: 2025-11-04
**Author**: Claude Code
**Related Docs**: deterministic-seeding-guide.md, noise-generation-guide.md, hydrology-implementation-guide.md, geometry-operations-guide.md
