using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// C# port of Voronoi diagram generation from the original JavaScript implementation
/// Uses Delaunator for triangulation and builds Voronoi cells from the triangulation
/// </summary>
public class Voronoi
{
    private readonly Delaunator _delaunay;
    private readonly Point[] _points;
    private readonly int _pointsN;
    
    // Voronoi cells data
    public VoronoiCells Cells { get; private set; }
    public VoronoiVertices Vertices { get; private set; }
    
    public Voronoi(Delaunator delaunay, Point[] points, int pointsN)
    {
        _delaunay = delaunay;
        _points = points;
        _pointsN = pointsN;
        
        Cells = new VoronoiCells();
        Vertices = new VoronoiVertices();
        
        BuildVoronoiDiagram();
    }
    
    /// <summary>
    /// Builds the Voronoi diagram from the Delaunay triangulation
    /// </summary>
    private void BuildVoronoiDiagram()
    {
        // Initialize arrays
        Cells.Vertices = new List<int>[_pointsN];
        Cells.Neighbors = new List<int>[_pointsN];
        Cells.IsBorder = new bool[_pointsN];
        
        Vertices.Coordinates = new List<Point>();
        Vertices.Cells = new List<int>[0]; // Will be resized as needed
        Vertices.Neighbors = new List<int>[0]; // Will be resized as needed
        
        // Process each half-edge in the triangulation
        for (int e = 0; e < _delaunay.Triangles.Length; e++)
        {
            // Get the point at the start of this half-edge
            int p = _delaunay.Triangles[NextHalfedge(e)];
            
            if (p < _pointsN && Cells.Vertices[p] == null)
            {
                // Get all edges around this point
                var edges = EdgesAroundPoint(e);
                
                // Build cell vertices from triangle centers
                Cells.Vertices[p] = edges.Select(edge => TriangleOfEdge(edge)).ToList();
                
                // Build cell neighbors from triangle points
                Cells.Neighbors[p] = edges
                    .Select(edge => _delaunay.Triangles[edge])
                    .Where(neighbor => neighbor < _pointsN)
                    .ToList();
                
                // Check if cell is on the border
                Cells.IsBorder[p] = edges.Count > Cells.Neighbors[p].Count;
            }
            
            // Get the triangle for this half-edge
            int t = TriangleOfEdge(e);
            if (Vertices.Coordinates.Count <= t)
            {
                // Resize arrays if needed
                int oldSize = Vertices.Coordinates.Count;
                var newCellsSize = Vertices.Cells?.Length ?? 0;
                var newNeighborsSize = Vertices.Neighbors?.Length ?? 0;
                
                if (newCellsSize <= t)
                {
                    var cellsArray = Vertices.Cells;
                    Array.Resize(ref cellsArray, t + 1);
                    Vertices.Cells = cellsArray;
                }
                if (newNeighborsSize <= t)
                {
                    var neighborsArray = Vertices.Neighbors;
                    Array.Resize(ref neighborsArray, t + 1);
                    Vertices.Neighbors = neighborsArray;
                }
                
                for (int i = oldSize; i <= t; i++)
                {
                    Vertices.Cells[i] = new List<int>();
                    Vertices.Neighbors[i] = new List<int>();
                }
            }
            
            if (Vertices.Coordinates.Count == t)
            {
                // Calculate triangle center (circumcenter)
                Vertices.Coordinates.Add(TriangleCenter(t));
                
                // Get adjacent triangles
                Vertices.Neighbors[t] = TrianglesAdjacentToTriangle(t).ToList();
                
                // Get triangle points (cells)
                Vertices.Cells[t] = PointsOfTriangle(t).ToList();
            }
        }
    }
    
    /// <summary>
    /// Gets the next half-edge in the triangle
    /// </summary>
    private static int NextHalfedge(int e)
    {
        return (e % 3 == 2) ? e - 2 : e + 1;
    }
    
    /// <summary>
    /// Gets the previous half-edge in the triangle
    /// </summary>
    private static int PrevHalfedge(int e)
    {
        return (e % 3 == 0) ? e + 2 : e - 1;
    }
    
    /// <summary>
    /// Gets the triangle index from a half-edge
    /// </summary>
    private static int TriangleOfEdge(int e)
    {
        return e / 3;
    }
    
    /// <summary>
    /// Gets all half-edges around a point
    /// </summary>
    private List<int> EdgesAroundPoint(int start)
    {
        var edges = new List<int>();
        int e = start;
        
        do
        {
            edges.Add(e);
            e = PrevHalfedge(e);
            int opposite = _delaunay.Halfedges[e];
            if (opposite == -1)
            {
                // We've hit the boundary
                break;
            }
            e = opposite;
        } while (e != start);
        
        return edges;
    }
    
    /// <summary>
    /// Gets the three points of a triangle
    /// </summary>
    private int[] PointsOfTriangle(int t)
    {
        return EdgesOfTriangle(t).Select(edge => _delaunay.Triangles[edge]).ToArray();
    }
    
    /// <summary>
    /// Gets the three half-edges of a triangle
    /// </summary>
    private static int[] EdgesOfTriangle(int t)
    {
        return new[] { 3 * t, 3 * t + 1, 3 * t + 2 };
    }
    
    /// <summary>
    /// Gets triangles adjacent to a given triangle
    /// </summary>
    private List<int> TrianglesAdjacentToTriangle(int t)
    {
        var adjacent = new List<int>();
        var edges = EdgesOfTriangle(t);
        
        foreach (var edge in edges)
        {
            int opposite = _delaunay.Halfedges[edge];
            if (opposite != -1)
            {
                adjacent.Add(TriangleOfEdge(opposite));
            }
        }
        
        return adjacent;
    }
    
    /// <summary>
    /// Calculates the circumcenter of a triangle
    /// </summary>
    private Point TriangleCenter(int t)
    {
        var edges = EdgesOfTriangle(t);
        var p1 = _points[_delaunay.Triangles[edges[0]]];
        var p2 = _points[_delaunay.Triangles[edges[1]]];
        var p3 = _points[_delaunay.Triangles[edges[2]]];
        
        // Calculate circumcenter using perpendicular bisectors
        double ax = p1.X, ay = p1.Y;
        double bx = p2.X, by = p2.Y;
        double cx = p3.X, cy = p3.Y;
        
        double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Math.Abs(d) < double.Epsilon)
        {
            // Degenerate triangle, return centroid
            return new Point((ax + bx + cx) / 3, (ay + by + cy) / 3);
        }
        
        double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
        double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
        
        return new Point(ux, uy);
    }
    
    /// <summary>
    /// Gets all cells that are on the border of the diagram
    /// </summary>
    public List<int> GetBorderCells()
    {
        var borderCells = new List<int>();
        for (int i = 0; i < _pointsN; i++)
        {
            if (Cells.IsBorder[i])
            {
                borderCells.Add(i);
            }
        }
        return borderCells;
    }
    
    /// <summary>
    /// Gets all cells that are not on the border
    /// </summary>
    public List<int> GetInteriorCells()
    {
        var interiorCells = new List<int>();
        for (int i = 0; i < _pointsN; i++)
        {
            if (!Cells.IsBorder[i])
            {
                interiorCells.Add(i);
            }
        }
        return interiorCells;
    }
    
    /// <summary>
    /// Gets the vertices of a specific cell as Point objects
    /// </summary>
    public List<Point> GetCellVertices(int cellId)
    {
        if (cellId < 0 || cellId >= Cells.Vertices.Length || Cells.Vertices[cellId] == null)
            return new List<Point>();
        
        return Cells.Vertices[cellId]
            .Select(vertexId => vertexId < Vertices.Coordinates.Count ? Vertices.Coordinates[vertexId] : Point.Zero)
            .ToList();
    }
    
    /// <summary>
    /// Gets the neighbors of a specific cell
    /// </summary>
    public List<int> GetCellNeighbors(int cellId)
    {
        if (cellId < 0 || cellId >= Cells.Neighbors.Length || Cells.Neighbors[cellId] == null)
            return new List<int>();
        
        return Cells.Neighbors[cellId];
    }
    
    /// <summary>
    /// Checks if a cell is on the border
    /// </summary>
    public bool IsCellBorder(int cellId)
    {
        return cellId >= 0 && cellId < Cells.IsBorder.Length && Cells.IsBorder[cellId];
    }
}

/// <summary>
/// Container for Voronoi cell data
/// </summary>
public class VoronoiCells
{
    public List<int>[] Vertices { get; set; } = null!;
    public List<int>[] Neighbors { get; set; } = null!;
    public bool[] IsBorder { get; set; } = null!;
}

/// <summary>
/// Container for Voronoi vertex data
/// </summary>
public class VoronoiVertices
{
    public List<Point> Coordinates { get; set; } = new();
    public List<int>[] Cells { get; set; } = null!;
    public List<int>[] Neighbors { get; set; } = null!;
}