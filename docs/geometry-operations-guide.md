# Geometry Operations Implementation Guide

## Overview

This guide details how to integrate **NetTopologySuite (NTS)** into the Fantasy Map Generator port for robust polygon operations, spatial indexing, and advanced geometry processing.

**Current State**: Basic utilities (distance, area, centroid, point-in-polygon)
**Target State**: NTS for robust clipping, buffering, smoothing, intersection, spatial indexing

---

## Why NetTopologySuite?

**Advantages**:
- ✅ **Industry Standard**: JTS (Java Topology Suite) port, battle-tested
- 🔧 **Rich Operations**: Buffer, clip, intersect, union, difference, simplify
- 📐 **Topology Validation**: Detect and fix self-intersections, gaps, overlaps
- 🚀 **Spatial Indexing**: STRtree for O(log n) spatial queries
- 🌐 **Standards Compliant**: OGC Simple Features, WKT/WKB, GeoJSON
- 🔗 **Ecosystem**: Works with GeoJSON.NET, NetTopologySuite.IO.VectorTiles

**Use Cases in FMG**:

| Feature | Without NTS | With NTS |
|---------|-------------|----------|
| **Border smoothing** | Manual vertex averaging | `Geometry.Buffer(ε).Buffer(-ε)` |
| **State boundaries** | Cell neighbor traversal | `UnaryUnionOp.Union(polygons)` |
| **Rain shadows** | Complex manual math | `ridge.Buffer(r).Intersection(cells)` |
| **Coast simplification** | Douglas-Peucker from scratch | `LineString.Simplify(tolerance)` |
| **Territory growth** | BFS/Dijkstra + containment | `Geometry.Buffer(distance)` |
| **Spatial queries** | Linear scan O(n) | `STRtree.Query()` O(log n) |

---

## Installation

```bash
cd src/FantasyMapGenerator.Core
dotnet add package NetTopologySuite
```

**Version**: Use latest stable (e.g., 2.5.0+)

**Optional Extensions**:
```bash
# For GeoJSON export
dotnet add package NetTopologySuite.IO.GeoJSON

# For vector tiles
dotnet add package NetTopologySuite.IO.VectorTiles
```

---

## Core Concepts

### 1. Geometry Factory

All geometries are created via `GeometryFactory`:

```csharp
using NetTopologySuite.Geometries;

var factory = new GeometryFactory();

// Create point
var point = factory.CreatePoint(new Coordinate(10, 20));

// Create line
var line = factory.CreateLineString(new[]
{
    new Coordinate(0, 0),
    new Coordinate(10, 10)
});

// Create polygon
var polygon = factory.CreatePolygon(new[]
{
    new Coordinate(0, 0),
    new Coordinate(10, 0),
    new Coordinate(10, 10),
    new Coordinate(0, 10),
    new Coordinate(0, 0) // Must close the ring!
});
```

**Important**: Polygon rings must be **closed** (first point == last point).

---

### 2. Coordinate System

NTS uses **double precision** (X, Y, Z, M):
- X, Y: Standard coordinates
- Z: Optional elevation
- M: Optional measure (e.g., distance along route)

**Conversion from FMG `Point`**:
```csharp
// FMG Point to NTS Coordinate
public static Coordinate ToCoordinate(Point p) => new Coordinate(p.X, p.Y);

// NTS Coordinate to FMG Point
public static Point ToPoint(Coordinate c) => new Point(c.X, c.Y);
```

---

### 3. Topology Rules

NTS enforces **OGC topology**:
- Polygons must have closed rings
- Exterior ring: counter-clockwise (CCW)
- Interior rings (holes): clockwise (CW)
- No self-intersections
- No duplicate consecutive vertices

**Validation**:
```csharp
var validator = new IsValidOp(geometry);
if (!validator.IsValid)
{
    Console.WriteLine($"Invalid: {validator.ValidationError}");
}
```

**Auto-Fix**:
```csharp
var fixed = GeometryFixer.Fix(geometry);
```

---

## Integration with FMG Data Model

### Adapter Pattern

Create `NtsGeometryAdapter.cs` to bridge FMG and NTS:

```csharp
using NetTopologySuite.Geometries;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// Converts between FMG data structures and NTS geometries
/// </summary>
public class NtsGeometryAdapter
{
    private readonly GeometryFactory _factory;

    public NtsGeometryAdapter()
    {
        _factory = new GeometryFactory();
    }

    /// <summary>
    /// Convert a FMG cell to NTS polygon
    /// </summary>
    public Polygon CellToPolygon(Cell cell, List<Point> vertices)
    {
        if (cell.Vertices.Count < 3)
        {
            throw new ArgumentException("Cell must have at least 3 vertices");
        }

        // Get vertex coordinates
        var coords = cell.Vertices
            .Select(i => vertices[i])
            .Select(p => new Coordinate(p.X, p.Y))
            .ToList();

        // Close the ring if not already closed
        if (!coords[0].Equals2D(coords[^1]))
        {
            coords.Add(coords[0]);
        }

        return _factory.CreatePolygon(coords.ToArray());
    }

    /// <summary>
    /// Convert multiple cells to MultiPolygon
    /// </summary>
    public MultiPolygon CellsToMultiPolygon(IEnumerable<Cell> cells, List<Point> vertices)
    {
        var polygons = cells
            .Select(c => CellToPolygon(c, vertices))
            .ToArray();

        return _factory.CreateMultiPolygon(polygons);
    }

    /// <summary>
    /// Union all cells into a single geometry (may be MultiPolygon)
    /// </summary>
    public NetTopologySuite.Geometries.Geometry UnionCells(
        IEnumerable<Cell> cells,
        List<Point> vertices)
    {
        var geometries = cells
            .Select(c => (NetTopologySuite.Geometries.Geometry)CellToPolygon(c, vertices))
            .ToList();

        return UnaryUnionOp.Union(geometries);
    }

    /// <summary>
    /// Get cells that intersect a geometry
    /// </summary>
    public List<Cell> GetIntersectingCells(
        NetTopologySuite.Geometries.Geometry geometry,
        MapData map)
    {
        var intersecting = new List<Cell>();

        foreach (var cell in map.Cells)
        {
            var cellPoly = CellToPolygon(cell, map.Vertices);

            if (geometry.Intersects(cellPoly))
            {
                intersecting.Add(cell);
            }
        }

        return intersecting;
    }

    /// <summary>
    /// Create LineString from river cells
    /// </summary>
    public LineString RiverToLineString(River river, MapData map)
    {
        var coords = river.Cells
            .Select(cellId => map.Cells[cellId].Center)
            .Select(p => new Coordinate(p.X, p.Y))
            .ToArray();

        return _factory.CreateLineString(coords);
    }

    /// <summary>
    /// Create MultiLineString from all state borders
    /// </summary>
    public MultiLineString GetStateBorders(int stateId, MapData map)
    {
        var stateCells = map.GetStateCells(stateId);
        var cellSet = new HashSet<int>(stateCells.Select(c => c.Id));

        var borderSegments = new List<LineString>();

        foreach (var cell in stateCells)
        {
            for (int i = 0; i < cell.Vertices.Count; i++)
            {
                int v1 = cell.Vertices[i];
                int v2 = cell.Vertices[(i + 1) % cell.Vertices.Count];

                // Check if this edge is a border (neighbor not in state)
                var sharedNeighbor = cell.Neighbors
                    .FirstOrDefault(n => !cellSet.Contains(n));

                if (sharedNeighbor >= 0)
                {
                    var coords = new[]
                    {
                        new Coordinate(map.Vertices[v1].X, map.Vertices[v1].Y),
                        new Coordinate(map.Vertices[v2].X, map.Vertices[v2].Y)
                    };

                    borderSegments.Add(_factory.CreateLineString(coords));
                }
            }
        }

        return _factory.CreateMultiLineString(borderSegments.ToArray());
    }
}
```

---

## Common Operations

### 1. Border Smoothing (Morphological Operations)

**Problem**: Voronoi cells create jagged borders

**Solution**: Buffer + negative buffer (closing operation)

```csharp
public class BiomeSmoothing
{
    private readonly NtsGeometryAdapter _adapter;

    public BiomeSmoothing()
    {
        _adapter = new NtsGeometryAdapter();
    }

    public void SmoothBiomeBoundaries(MapData map, int biomeId, double smoothRadius)
    {
        // Get all cells of this biome
        var biomeCells = map.Cells.Where(c => c.Biome == biomeId).ToList();

        // Union into single region
        var region = _adapter.UnionCells(biomeCells, map.Vertices);

        // Smooth via morphological closing (buffer + negative buffer)
        var smoothed = region.Buffer(smoothRadius).Buffer(-smoothRadius);

        // Simplify to reduce vertex count
        smoothed = DouglasPeuckerSimplifier.Simplify(smoothed, smoothRadius * 0.1);

        // Update cells based on new boundary
        UpdateCellsFromGeometry(map, smoothed, biomeId);
    }

    private void UpdateCellsFromGeometry(
        MapData map,
        NetTopologySuite.Geometries.Geometry geometry,
        int biomeId)
    {
        foreach (var cell in map.Cells)
        {
            var cellPoly = _adapter.CellToPolygon(cell, map.Vertices);
            var cellCenter = cellPoly.Centroid;

            // If cell center is in smoothed geometry, assign biome
            if (geometry.Contains(cellCenter))
            {
                cell.Biome = biomeId;
            }
        }
    }
}
```

**Parameters**:
- `smoothRadius`: Larger = smoother borders (typical: 1-5% of map size)

---

### 2. State Boundary Generation

**Problem**: Need clean polygon boundaries for states

**Solution**: Union cells + simplify

```csharp
public class StateBoundaryGenerator
{
    private readonly NtsGeometryAdapter _adapter;

    public StateBoundaryGenerator()
    {
        _adapter = new NtsGeometryAdapter();
    }

    public NetTopologySuite.Geometries.Geometry GetStateBoundary(
        int stateId,
        MapData map,
        double simplificationTolerance = 0.5)
    {
        var stateCells = map.GetStateCells(stateId);

        // Union all cells into single geometry
        var boundary = _adapter.UnionCells(stateCells, map.Vertices);

        // Simplify to reduce complexity
        boundary = DouglasPeuckerSimplifier.Simplify(boundary, simplificationTolerance);

        // Optionally smooth
        boundary = boundary.Buffer(0.2).Buffer(-0.2);

        return boundary;
    }

    public Dictionary<int, NetTopologySuite.Geometries.Geometry> GetAllStateBoundaries(MapData map)
    {
        var boundaries = new Dictionary<int, NetTopologySuite.Geometries.Geometry>();

        foreach (var state in map.States)
        {
            boundaries[state.Id] = GetStateBoundary(state.Id, map);
        }

        return boundaries;
    }

    /// <summary>
    /// Export state boundaries as GeoJSON
    /// </summary>
    public string ExportAsGeoJson(Dictionary<int, NetTopologySuite.Geometries.Geometry> boundaries)
    {
        var writer = new NetTopologySuite.IO.GeoJsonWriter();

        var featureCollection = new
        {
            type = "FeatureCollection",
            features = boundaries.Select(kvp => new
            {
                type = "Feature",
                properties = new { stateId = kvp.Key },
                geometry = writer.Write(kvp.Value)
            })
        };

        return System.Text.Json.JsonSerializer.Serialize(featureCollection);
    }
}
```

---

### 3. Rain Shadow Calculation

**Problem**: Mountains block moisture, creating dry regions

**Solution**: Buffer mountain ridges, intersect with terrain

```csharp
public class RainShadowCalculator
{
    private readonly NtsGeometryAdapter _adapter;

    public RainShadowCalculator()
    {
        _adapter = new NtsGeometryAdapter();
    }

    public void ApplyRainShadows(MapData map, Point windDirection, double shadowDistance)
    {
        // Find ridge cells (local maxima)
        var ridgeCells = FindRidges(map);

        foreach (var ridge in ridgeCells)
        {
            var ridgePoly = _adapter.CellToPolygon(ridge, map.Vertices);

            // Create shadow cone in wind direction
            var shadowZone = CreateShadowCone(
                ridgePoly,
                windDirection,
                shadowDistance,
                ridge.Height);

            // Reduce moisture in shadow zone
            var affectedCells = _adapter.GetIntersectingCells(shadowZone, map);

            foreach (var cell in affectedCells)
            {
                // Reduce moisture proportional to distance from ridge
                var distanceRatio = DistanceToRidge(cell, ridgePoly) / shadowDistance;
                cell.Precipitation *= Math.Max(0.3, distanceRatio); // Min 30% moisture
            }
        }
    }

    private NetTopologySuite.Geometries.Geometry CreateShadowCone(
        Polygon ridge,
        Point windDirection,
        double distance,
        int height)
    {
        // Normalize wind direction
        var windLen = Math.Sqrt(windDirection.X * windDirection.X + windDirection.Y * windDirection.Y);
        var windX = windDirection.X / windLen;
        var windY = windDirection.Y / windLen;

        // Cast shadow downwind
        var centroid = ridge.Centroid;
        var shadowEnd = new Coordinate(
            centroid.X + windX * distance,
            centroid.Y + windY * distance);

        // Create cone (ridge buffered + line to shadow end)
        var ridgeBuffer = ridge.Buffer(height * 0.1); // Wider for taller ridges
        var shadowLine = new GeometryFactory().CreateLineString(new[]
        {
            centroid.Coordinate,
            shadowEnd
        });

        var shadowCone = shadowLine.Buffer(distance * 0.3); // Cone width

        return ridgeBuffer.Union(shadowCone);
    }

    private List<Cell> FindRidges(MapData map)
    {
        var ridges = new List<Cell>();

        foreach (var cell in map.Cells)
        {
            // Ridge = higher than all neighbors
            bool isRidge = cell.Neighbors.All(nId =>
                cell.Height > map.Cells[nId].Height);

            if (isRidge && cell.Height > 70) // Threshold
            {
                ridges.Add(cell);
            }
        }

        return ridges;
    }

    private double DistanceToRidge(Cell cell, Polygon ridge)
    {
        var cellCenter = new GeometryFactory().CreatePoint(
            new Coordinate(cell.Center.X, cell.Center.Y));

        return cellCenter.Distance(ridge);
    }
}
```

---

### 4. Spatial Indexing (Fast Queries)

**Problem**: Finding nearby cells is O(n)

**Solution**: STRtree for O(log n) spatial queries

```csharp
using NetTopologySuite.Index.Strtree;

public class SpatialMapData : MapData
{
    private STRtree<Cell> _cellIndex;
    private STRtree<Burg> _burgIndex;

    public void BuildSpatialIndex()
    {
        _cellIndex = new STRtree<Cell>();
        _burgIndex = new STRtree<Burg>();

        // Index cells by envelope (bounding box)
        foreach (var cell in Cells)
        {
            var envelope = GetCellEnvelope(cell);
            _cellIndex.Insert(envelope, cell);
        }

        // Index burgs by point
        foreach (var burg in Burgs)
        {
            var point = Cells[burg.Cell].Center;
            var envelope = new Envelope(point.X, point.X, point.Y, point.Y);
            _burgIndex.Insert(envelope, burg);
        }
    }

    public List<Cell> QueryCellsInRadius(Point center, double radius)
    {
        var envelope = new Envelope(
            center.X - radius,
            center.X + radius,
            center.Y - radius,
            center.Y + radius);

        return _cellIndex.Query(envelope)
            .Where(c => Distance(c.Center, center) <= radius)
            .ToList();
    }

    public List<Burg> QueryBurgsInRegion(NetTopologySuite.Geometries.Geometry region)
    {
        var envelope = region.EnvelopeInternal;

        var candidates = _burgIndex.Query(envelope);

        var adapter = new NtsGeometryAdapter();
        var factory = new GeometryFactory();

        return candidates
            .Where(b =>
            {
                var burgPoint = Cells[b.Cell].Center;
                var ntsPoint = factory.CreatePoint(new Coordinate(burgPoint.X, burgPoint.Y));
                return region.Contains(ntsPoint);
            })
            .ToList();
    }

    private Envelope GetCellEnvelope(Cell cell)
    {
        var xs = cell.Vertices.Select(i => Vertices[i].X);
        var ys = cell.Vertices.Select(i => Vertices[i].Y);

        return new Envelope(
            xs.Min(), xs.Max(),
            ys.Min(), ys.Max());
    }

    private double Distance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
```

**Usage**:
```csharp
var map = new SpatialMapData();
// ... generate map ...
map.BuildSpatialIndex();

// Fast radius query (O(log n) instead of O(n))
var nearby = map.QueryCellsInRadius(new Point(500, 500), 50);

// Fast region query
var stateBoundary = GetStateBoundary(stateId, map);
var burgsInState = map.QueryBurgsInRegion(stateBoundary);
```

**Performance**: For 10,000 cells, STRtree reduces query time from ~10ms (linear scan) to ~0.1ms.

---

### 5. Line Simplification (Douglas-Peucker)

**Problem**: Voronoi edges have many vertices, large export size

**Solution**: Simplify lines/polygons

```csharp
using NetTopologySuite.Simplify;

public class GeometrySimplifier
{
    /// <summary>
    /// Simplify coastlines to reduce vertex count
    /// </summary>
    public LineString SimplifyCoastline(MapData map, double tolerance)
    {
        // Get ocean cells
        var oceanCells = map.Cells.Where(c => c.Height == 0).ToList();

        // Get land cells
        var landCells = map.Cells.Where(c => c.Height > 0).ToList();

        var adapter = new NtsGeometryAdapter();

        // Union land into single geometry
        var land = adapter.UnionCells(landCells, map.Vertices);

        // Extract boundary (coastline)
        var coastline = land.Boundary;

        // Simplify
        var simplified = DouglasPeuckerSimplifier.Simplify(coastline, tolerance);

        return (LineString)simplified;
    }

    /// <summary>
    /// Simplify all state boundaries
    /// </summary>
    public Dictionary<int, NetTopologySuite.Geometries.Geometry> SimplifyStateBoundaries(
        MapData map,
        double tolerance)
    {
        var simplified = new Dictionary<int, NetTopologySuite.Geometries.Geometry>();

        foreach (var state in map.States)
        {
            var cells = map.GetStateCells(state.Id);
            var adapter = new NtsGeometryAdapter();
            var boundary = adapter.UnionCells(cells, map.Vertices);

            simplified[state.Id] = DouglasPeuckerSimplifier.Simplify(boundary, tolerance);
        }

        return simplified;
    }
}
```

**Tolerance**: Larger = fewer vertices (typical: 0.1-1.0% of map size)

---

## Advanced Use Cases

### 1. Territory Expansion (for Political Simulation)

```csharp
public class TerritoryExpansion
{
    private readonly NtsGeometryAdapter _adapter;

    public TerritoryExpansion()
    {
        _adapter = new NtsGeometryAdapter();
    }

    /// <summary>
    /// Expand state territory by a given distance
    /// </summary>
    public List<Cell> ExpandTerritory(
        int stateId,
        MapData map,
        double expansionDistance)
    {
        var currentCells = map.GetStateCells(stateId);
        var currentTerritory = _adapter.UnionCells(currentCells, map.Vertices);

        // Buffer to expand
        var expandedTerritory = currentTerritory.Buffer(expansionDistance);

        // Find cells in expanded region
        var newCells = _adapter.GetIntersectingCells(expandedTerritory, map);

        return newCells
            .Where(c => c.State != stateId) // Only new cells
            .Where(c => c.Height > 0)       // Must be land
            .ToList();
    }

    /// <summary>
    /// Grow state borders until they meet (Voronoi-style growth)
    /// </summary>
    public void GrowUntilMeet(MapData map)
    {
        const double stepSize = 1.0;
        const int maxSteps = 100;

        var stateTerritories = map.States
            .ToDictionary(s => s.Id, s => _adapter.UnionCells(
                map.GetStateCells(s.Id),
                map.Vertices));

        for (int step = 0; step < maxSteps; step++)
        {
            bool anyGrowth = false;

            foreach (var state in map.States)
            {
                var expanded = stateTerritories[state.Id].Buffer(stepSize);

                // Check for collisions with other states
                bool collision = map.States
                    .Where(s => s.Id != state.Id)
                    .Any(s => expanded.Intersects(stateTerritories[s.Id]));

                if (!collision)
                {
                    stateTerritories[state.Id] = expanded;
                    anyGrowth = true;
                }
            }

            if (!anyGrowth) break; // All states have met
        }

        // Update cell ownership
        foreach (var cell in map.Cells)
        {
            var cellPoly = _adapter.CellToPolygon(cell, map.Vertices);
            var cellCenter = cellPoly.Centroid;

            foreach (var state in map.States)
            {
                if (stateTerritories[state.Id].Contains(cellCenter))
                {
                    cell.State = state.Id;
                    break;
                }
            }
        }
    }
}
```

---

### 2. Watershed Analysis

```csharp
public class WatershedAnalyzer
{
    private readonly NtsGeometryAdapter _adapter;

    /// <summary>
    /// Identify watershed basins (drainage regions for each river mouth)
    /// </summary>
    public Dictionary<int, List<Cell>> CalculateWatersheds(MapData map)
    {
        var watersheds = new Dictionary<int, List<Cell>>();

        // For each river, trace upstream and collect drainage basin
        foreach (var river in map.Rivers)
        {
            var basin = new HashSet<Cell>();

            // Trace all tributaries
            TraceUpstream(map.Cells[river.Source], map, basin);

            watersheds[river.Id] = basin.ToList();
        }

        return watersheds;
    }

    private void TraceUpstream(Cell current, MapData map, HashSet<Cell> basin)
    {
        if (basin.Contains(current)) return;

        basin.Add(current);

        // Find cells that flow into current
        foreach (var neighbor in current.Neighbors)
        {
            var neighborCell = map.Cells[neighbor];

            // Check if neighbor flows into current (downhill)
            if (neighborCell.Height > current.Height)
            {
                TraceUpstream(neighborCell, map, basin);
            }
        }
    }

    /// <summary>
    /// Export watersheds as GeoJSON with colors
    /// </summary>
    public string ExportWatershedsGeoJson(
        Dictionary<int, List<Cell>> watersheds,
        MapData map)
    {
        var adapter = new NtsGeometryAdapter();
        var features = new List<object>();

        foreach (var (riverId, cells) in watersheds)
        {
            var geometry = adapter.UnionCells(cells, map.Vertices);

            features.Add(new
            {
                type = "Feature",
                properties = new
                {
                    riverId,
                    area = geometry.Area,
                    cells = cells.Count
                },
                geometry = new NetTopologySuite.IO.GeoJsonWriter().Write(geometry)
            });
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "FeatureCollection",
            features
        });
    }
}
```

---

## Testing

### Unit Tests

```csharp
using NetTopologySuite.Geometries;
using Xunit;

public class NtsGeometryAdapterTests
{
    [Fact]
    public void CellToPolygon_CreatesClosedRing()
    {
        var adapter = new NtsGeometryAdapter();

        var cell = new Cell
        {
            Id = 0,
            Vertices = new List<int> { 0, 1, 2 }
        };

        var vertices = new List<Point>
        {
            new Point(0, 0),
            new Point(10, 0),
            new Point(5, 10)
        };

        var polygon = adapter.CellToPolygon(cell, vertices);

        Assert.Equal(4, polygon.Coordinates.Length); // 3 vertices + 1 to close
        Assert.Equal(polygon.Coordinates[0], polygon.Coordinates[^1]);
    }

    [Fact]
    public void UnionCells_CombinesAdjacentCells()
    {
        var adapter = new NtsGeometryAdapter();

        // Two adjacent square cells
        var cell1 = new Cell { Id = 0, Vertices = new List<int> { 0, 1, 2, 3 } };
        var cell2 = new Cell { Id = 1, Vertices = new List<int> { 1, 4, 5, 2 } };

        var vertices = new List<Point>
        {
            new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10),
            new Point(20, 0), new Point(20, 10)
        };

        var union = adapter.UnionCells(new[] { cell1, cell2 }, vertices);

        // Union should create single polygon (not MultiPolygon)
        Assert.IsType<Polygon>(union);
    }

    [Fact]
    public void BufferAndNegativeBuffer_SmoothsBoundary()
    {
        var factory = new GeometryFactory();

        // Jagged polygon
        var jagged = factory.CreatePolygon(new[]
        {
            new Coordinate(0, 0),
            new Coordinate(10, 1),
            new Coordinate(9, 10),
            new Coordinate(1, 9),
            new Coordinate(0, 0)
        });

        // Smooth via morphological closing
        var smoothed = jagged.Buffer(2).Buffer(-2);

        // Smoothed should have fewer vertices
        Assert.True(smoothed.Coordinates.Length <= jagged.Coordinates.Length);
    }
}
```

---

## Performance Considerations

### 1. Geometry Validation

Validation is expensive - only validate when debugging:

```csharp
#if DEBUG
var validator = new IsValidOp(geometry);
if (!validator.IsValid)
{
    throw new InvalidOperationException($"Invalid geometry: {validator.ValidationError}");
}
#endif
```

### 2. Precision Model

For integer coordinates, use `PrecisionModel`:

```csharp
var precisionModel = new PrecisionModel(PrecisionModels.Fixed);
var factory = new GeometryFactory(precisionModel);
```

### 3. Caching

Cache expensive operations:

```csharp
private readonly Dictionary<int, NetTopologySuite.Geometries.Geometry> _stateBoundaryCache = new();

public NetTopologySuite.Geometries.Geometry GetStateBoundary(int stateId, MapData map)
{
    if (_stateBoundaryCache.TryGetValue(stateId, out var cached))
    {
        return cached;
    }

    var boundary = ComputeStateBoundary(stateId, map);
    _stateBoundaryCache[stateId] = boundary;
    return boundary;
}
```

---

## Integration Checklist

- [ ] Add `NetTopologySuite` NuGet package
- [ ] Create `src/FantasyMapGenerator.Core/Geometry/NtsGeometryAdapter.cs`
- [ ] Implement border smoothing in `BiomeGenerator`
- [ ] Add state boundary generation
- [ ] (Optional) Implement spatial indexing in `MapData`
- [ ] (Optional) Add rain shadow calculation
- [ ] Write unit tests for adapter
- [ ] Profile performance impact

---

## Resources

- [NetTopologySuite GitHub](https://github.com/NetTopologySuite/NetTopologySuite)
- [NTS Documentation](https://nettopologysuite.github.io/NetTopologySuite/)
- [JTS Developer Guide](https://locationtech.github.io/jts/jts-dev-guide/)
- [OGC Simple Features Spec](https://www.ogc.org/standards/sfa)

---

**Last Updated**: 2025-11-04
**Related**: library-adoption-roadmap.md, hydrology-implementation-guide.md
