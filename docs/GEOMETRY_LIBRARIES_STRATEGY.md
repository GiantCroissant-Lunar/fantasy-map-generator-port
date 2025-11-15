# Geometry Libraries Strategy

## Overview

The Fantasy Map Generator uses **both** Triangle.NET and NetTopologySuite because they serve **complementary** purposes with minimal overlap (~10%).

## Library Roles

### Triangle.NET (Mesh Generation)
**Package:** `Triangle` v0.0.6-beta3  
**Size:** ~500KB  
**Purpose:** Generate high-quality Voronoi meshes

**Responsibilities:**
- ✅ Delaunay triangulation with quality constraints
- ✅ Voronoi diagram generation
- ✅ Mesh refinement (adding Steiner points)
- ✅ Constrained triangulation (boundaries, holes)
- ✅ Minimum angle constraints (20-34 degrees)
- ✅ Maximum area constraints

**Used For:**
- Initial map cell generation
- Creating uniform Voronoi cells
- Ensuring no sliver triangles
- Adaptive mesh density

### NetTopologySuite (Geometry Operations)
**Package:** `NetTopologySuite` v2.6.0  
**Size:** ~2MB  
**Purpose:** Perform spatial operations on geometries

**Responsibilities:**
- ✅ Union operations (combine cells into regions)
- ✅ Intersection/Difference (boolean operations)
- ✅ Buffer operations (expand/shrink geometries)
- ✅ Spatial queries (point-in-polygon, nearest neighbor)
- ✅ Border generation (state boundaries)
- ✅ Geometry simplification (Douglas-Peucker)
- ✅ Geometry validation and repair
- ✅ GeoJSON import/export

**Used For:**
- Combining cells into states/provinces
- Generating state borders
- Spatial queries (which cells in a region?)
- Exporting maps to GeoJSON
- Validating and fixing geometries

## Workflow Integration

```
┌─────────────────────────────────────────────────────────────┐
│                    Map Generation Flow                       │
└─────────────────────────────────────────────────────────────┘

1. Generate Points
   └─> GeometryUtils.GeneratePoissonDiskPoints()

2. Create Mesh (Triangle.NET)
   └─> TriangleNetAdapter.GenerateMesh(points, minAngle: 20)
       ├─> High-quality Delaunay triangulation
       ├─> No sliver triangles
       └─> Uniform cell sizes

3. Generate Voronoi (Triangle.NET)
   └─> TriangleNetAdapter.GenerateVoronoi(mesh)
       └─> Accurate Voronoi cells

4. Assign Properties
   └─> Heights, biomes, temperatures, etc.

5. Create Regions (NetTopologySuite)
   └─> NtsGeometryAdapter.UnionCells(stateCells)
       ├─> Combine cells into states
       ├─> Generate state borders
       └─> Validate geometries

6. Spatial Operations (NetTopologySuite)
   └─> NtsGeometryAdapter.GetStateBorder(stateId)
       ├─> Extract boundaries
       ├─> Simplify borders
       └─> Buffer operations

7. Export (NetTopologySuite)
   └─> GeoJsonExporter.ExportToGeoJson(map)
       └─> Standard GeoJSON format
```

## Overlap Analysis

### ~10% Overlap: Basic Voronoi/Delaunay

Both libraries can generate Voronoi diagrams and Delaunay triangulations, but:

**Triangle.NET Advantages:**
- Quality constraints (minimum angle)
- Mesh refinement
- Constrained triangulation
- Better for initial mesh generation

**NetTopologySuite Advantages:**
- Integration with other spatial operations
- GeoJSON support
- Geometry validation
- Better for post-processing

**Decision:** Use Triangle.NET for initial mesh, NTS for everything else.

### 90% Complementary: Unique Features

**Triangle.NET Only:**
- Minimum angle constraints
- Maximum area constraints
- Mesh refinement with Steiner points
- Constrained Delaunay triangulation

**NetTopologySuite Only:**
- Union/Intersection/Difference
- Buffer operations
- Spatial indexing (STRtree)
- GeoJSON import/export
- Geometry validation/repair
- Douglas-Peucker simplification

## Code Examples

### Example 1: Generate Initial Mesh (Triangle.NET)

```csharp
using FantasyMapGenerator.Core.Geometry;

// Generate high-quality mesh
var points = GeometryUtils.GeneratePoissonDiskPoints(width, height, spacing);
var mesh = TriangleNetAdapter.GenerateMesh(points, minAngle: 20);
var voronoi = TriangleNetAdapter.GenerateVoronoi(mesh);

// Result: Uniform, high-quality Voronoi cells
```

### Example 2: Create State Regions (NetTopologySuite)

```csharp
using FantasyMapGenerator.Core.Geometry;

// Combine cells into state
var stateCells = map.Cells.Where(c => c.StateId == stateId).ToList();
var stateGeometry = NtsGeometryAdapter.UnionCells(stateCells, map);

// Generate border
var border = NtsGeometryAdapter.GetStateBorder(stateId, map);

// Simplify for rendering
var simplified = border.Simplify(tolerance: 2.0);
```

### Example 3: Spatial Queries (NetTopologySuite)

```csharp
// Find cells within radius
var nearbyBurgs = spatialData.QueryBurgsInRadius(center, radius);

// Find cells in rectangle
var cellsInView = spatialData.QueryCellsInRectangle(bounds);

// Point-in-polygon test
bool isInState = stateGeometry.Contains(point);
```

### Example 4: Export to GeoJSON (NetTopologySuite)

```csharp
// Export entire map
var geoJson = GeoJsonExporter.ExportToGeoJson(map);

// Export specific features
var statesGeoJson = GeoJsonExporter.ExportStates(map);
var riversGeoJson = GeoJsonExporter.ExportRivers(map);
```

## Performance Considerations

### Triangle.NET
- **When:** Once at map generation start
- **Cost:** O(n log n) for n points
- **Typical:** 10,000 points = ~100ms

### NetTopologySuite
- **When:** Throughout map generation and rendering
- **Cost:** Varies by operation
  - Union: O(n log n)
  - Spatial query: O(log n) with index
  - Buffer: O(n)
- **Typical:** State union = ~50ms per state

### Total Overhead
- Triangle.NET: ~500KB
- NetTopologySuite: ~2MB
- **Combined:** ~2.5MB for complete solution

## Benefits of Using Both

1. **Best Tool for Each Job**
   - Triangle.NET: Superior mesh generation
   - NTS: Superior spatial operations

2. **Quality Meshes**
   - No sliver triangles
   - Uniform cell sizes
   - Configurable quality

3. **Powerful Operations**
   - Union cells into regions
   - Generate clean borders
   - Spatial queries
   - GeoJSON export

4. **Industry Standard**
   - Both are widely used
   - Well-documented
   - Battle-tested

5. **Maintainability**
   - Clear separation of concerns
   - Each library does what it's best at
   - Easier to understand and maintain

## Alternative Considered: NTS Only

**Why not use only NetTopologySuite?**

NTS can generate Voronoi diagrams, but:
- ❌ No quality constraints
- ❌ No mesh refinement
- ❌ No constrained triangulation
- ❌ Less control over mesh quality
- ❌ More sliver triangles

**Result:** Lower quality meshes, more irregular cells

## Alternative Considered: Triangle.NET Only

**Why not use only Triangle.NET?**

Triangle.NET can generate meshes, but:
- ❌ No union operations
- ❌ No spatial queries
- ❌ No GeoJSON export
- ❌ No geometry validation
- ❌ No buffer operations

**Result:** Would need to implement all spatial operations manually

## Conclusion

**Strategy:** Use both libraries for their strengths

- **Triangle.NET:** Generate high-quality initial mesh
- **NetTopologySuite:** Perform all spatial operations

**Benefits:**
- ✅ Best quality meshes
- ✅ Powerful spatial operations
- ✅ Industry-standard tools
- ✅ Only 2.5MB total
- ✅ Clear separation of concerns
- ✅ Maintainable codebase

**Cost:**
- 2.5MB package size (acceptable)
- Two dependencies (manageable)

**Verdict:** The complementary nature and clear benefits justify using both libraries.
