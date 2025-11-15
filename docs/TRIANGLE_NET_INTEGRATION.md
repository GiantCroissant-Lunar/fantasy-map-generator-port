# Triangle.NET Integration

Triangle.NET has been successfully integrated into the Fantasy Map Generator to provide high-quality Delaunay triangulation and Voronoi diagram generation.

## What is Triangle.NET?

Triangle.NET is a robust 2D mesh generator that produces quality-constrained Delaunay triangulations. It's based on Jonathan Shewchuk's Triangle library and provides:

- High-quality Delaunay triangulation with angle constraints
- Voronoi diagram generation
- Mesh refinement capabilities
- Constrained triangulation support

## Integration Details

### Project Reference

Triangle.NET is included as a **project reference** from the local `ref-projects/Triangle.NET` directory:

```xml
<ProjectReference Include="..\..\ref-projects\Triangle.NET\src\Triangle\Triangle.csproj" />
```

**Why Project Reference?**
- The official `wo80/Triangle.NET` repository does not publish to NuGet
- We use the source code directly from the cloned repository
- This gives us full control and the latest version

### Adapter Class

The `TriangleNetAdapter` class (`src/FantasyMapGenerator.Core/Geometry/TriangleNetAdapter.cs`) provides a clean interface to Triangle.NET functionality:

```csharp
// Generate a high-quality Delaunay mesh
var mesh = TriangleNetAdapter.GenerateMesh(points, minAngle: 20);

// Generate Voronoi diagram from the mesh
var voronoi = TriangleNetAdapter.GenerateVoronoi(mesh);
```

## Benefits

1. **Improved Mesh Quality**: Triangle.NET produces higher quality triangulations with configurable minimum angle constraints (0-34 degrees)
2. **Better Voronoi Diagrams**: More accurate and stable Voronoi cell generation
3. **Mesh Refinement**: Ability to refine meshes by adding Steiner points
4. **Constrained Triangulation**: Support for boundary constraints and holes

## Usage Examples

### Basic Mesh Generation

```csharp
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;

var points = new List<Point>
{
    new Point(0, 0),
    new Point(100, 0),
    new Point(100, 100),
    new Point(0, 100)
};

// Generate mesh with 20-degree minimum angle constraint
var mesh = TriangleNetAdapter.GenerateMesh(points, minAngle: 20);
```

### Voronoi Diagram Generation

```csharp
// Generate Voronoi diagram from mesh
var voronoi = TriangleNetAdapter.GenerateVoronoi(mesh);

// Access Voronoi faces
foreach (var face in voronoi.Faces)
{
    // Process each Voronoi cell
    var generator = face.Generator; // Original point
    foreach (var edge in face.EnumerateEdges())
    {
        // Process cell edges
    }
}
```

### Constrained Mesh

```csharp
var boundaries = new List<List<Point>>
{
    new List<Point>
    {
        new Point(0, 0),
        new Point(100, 0),
        new Point(100, 100),
        new Point(0, 100)
    }
};

var mesh = TriangleNetAdapter.GenerateConstrainedMesh(points, boundaries, minAngle: 20);
```

## Future Enhancements

Potential future improvements using Triangle.NET:

1. **Lloyd Relaxation with Triangle.NET**: Use Triangle.NET's high-quality meshes for improved Lloyd relaxation
2. **Adaptive Mesh Refinement**: Refine meshes in areas requiring more detail
3. **Quality Metrics**: Analyze and report mesh quality statistics
4. **Custom Constraints**: Add support for holes and internal boundaries

## References

- [Triangle.NET GitHub](https://github.com/wo80/Triangle.NET)
- [Original Triangle Library](https://www.cs.cmu.edu/~quake/triangle.html)
- [Delaunay Triangulation](https://en.wikipedia.org/wiki/Delaunay_triangulation)
- [Voronoi Diagram](https://en.wikipedia.org/wiki/Voronoi_diagram)
