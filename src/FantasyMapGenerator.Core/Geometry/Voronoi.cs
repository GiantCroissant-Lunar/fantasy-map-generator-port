using System.Linq;
using FantasyMapGenerator.Core.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsPoint = NetTopologySuite.Geometries.Point;
using Point = FantasyMapGenerator.Core.Models.Point;

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

        // Only build if we have a valid delaunator (not using NTS path)
        if (_delaunay != null)
        {
            BuildVoronoiDiagram();
        }
    }

    /// <summary>
    /// Creates a Voronoi diagram directly from points using NetTopologySuite.
    /// This bypasses the buggy Delaunator implementation entirely.
    /// </summary>
    public static Voronoi FromPoints(Point[] points, int pointsN, double width, double height)
    {
        var voronoi = new Voronoi(null!, points, pointsN);

        // Convert to NTS coordinates
        var coordinates = new Coordinate[pointsN];
        for (int i = 0; i < pointsN; i++)
        {
            coordinates[i] = new Coordinate(points[i].X, points[i].Y);
        }

        // Create clipping envelope (map bounds with small margin)
        var gf = new GeometryFactory();
        double margin = Math.Max(width, height) * 0.01; // 1% margin
        var clipEnvelope = new Envelope(-margin, width + margin, -margin, height + margin);
        var clipRect = gf.ToGeometry(clipEnvelope);

        // Use NTS VoronoiDiagramBuilder to get Voronoi polygons directly
        var voronoiBuilder = new VoronoiDiagramBuilder();
        voronoiBuilder.SetSites(coordinates);
        voronoiBuilder.ClipEnvelope = clipEnvelope; // Set clipping envelope as property
        var diagram = voronoiBuilder.GetDiagram(gf);

        // Build coordinate to index map for lookups
        var coordToIndex = new Dictionary<Coordinate, int>(new CoordinateEqualityComparer());
        for (int i = 0; i < pointsN; i++)
        {
            coordToIndex[coordinates[i]] = i;
        }

        // Extract Voronoi cells from the diagram
        voronoi.BuildFromNtsDiagram(diagram, coordinates, coordToIndex);

        return voronoi;
    }

    private static int FindClosestIndex(Coordinate target, Coordinate[] allCoords)
    {
        double minDist = double.MaxValue;
        int closestIdx = 0;
        for (int i = 0; i < allCoords.Length; i++)
        {
            double dist = target.Distance(allCoords[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closestIdx = i;
            }
        }
        return closestIdx;
    }

    // Helper class for coordinate comparison
    private class CoordinateEqualityComparer : IEqualityComparer<Coordinate>
    {
        public bool Equals(Coordinate? x, Coordinate? y)
        {
            if (x == null || y == null) return false;
            return x.Distance(y) < 0.0001;
        }

        public int GetHashCode(Coordinate obj)
        {
            return HashCode.Combine((int)(obj.X * 1000), (int)(obj.Y * 1000));
        }
    }

    /// <summary>
    /// Builds Voronoi cells directly from NTS VoronoiDiagramBuilder output
    /// Populates vertices and neighbor lists.
    /// </summary>
    private void BuildFromNtsDiagram(NtsGeometry diagram, Coordinate[] siteCoords, Dictionary<Coordinate, int> coordToIndex)
    {
        // Initialize cell arrays
        Cells.Vertices = new List<int>[_pointsN];
        Cells.Neighbors = new List<int>[_pointsN];
        Cells.IsBorder = new bool[_pointsN];

        for (int i = 0; i < _pointsN; i++)
        {
            Cells.Vertices[i] = new List<int>();
            Cells.Neighbors[i] = new List<int>();
            Cells.IsBorder[i] = false;
        }

        // Vertices list (Voronoi vertices are polygon vertices)
        Vertices.Coordinates = new List<Point>();
        var vertexToIndex = new Dictionary<Coordinate, int>(new CoordinateEqualityComparer());

        // Edge ownership map for adjacency detection (by vertex indices, order-independent)
        var edgeOwner = new Dictionary<(int a, int b), int>();

        // Use diagram envelope to detect border cells
        var env = diagram.EnvelopeInternal;
        double minX = env.MinX, minY = env.MinY, maxX = env.MaxX, maxY = env.MaxY;
        const double eps = 1e-6;

        void Process(NtsGeometry geom)
        {
            if (geom is not Polygon poly) return;

            // Associate polygon to nearest site via centroid
            var centroid = poly.Centroid.Coordinate;
            int siteIdx = FindClosestIndex(centroid, siteCoords);

            // Extract vertices and build edges
            var ring = poly.ExteriorRing.Coordinates;
            int prevVertexIdx = -1;
            for (int i = 0; i < ring.Length - 1; i++) // skip last (duplicate of first)
            {
                var coord = ring[i];

                // Add vertex if new
                if (!vertexToIndex.TryGetValue(coord, out int vertexIdx))
                {
                    vertexIdx = Vertices.Coordinates.Count;
                    Vertices.Coordinates.Add(new Point(coord.X, coord.Y));
                    vertexToIndex[coord] = vertexIdx;
                }

                Cells.Vertices[siteIdx].Add(vertexIdx);

                // Border detection: vertex near clip envelope boundary
                if (Math.Abs(coord.X - minX) < eps || Math.Abs(coord.X - maxX) < eps ||
                    Math.Abs(coord.Y - minY) < eps || Math.Abs(coord.Y - maxY) < eps)
                {
                    Cells.IsBorder[siteIdx] = true;
                }

                // Build undirected edge with previous vertex
                if (prevVertexIdx >= 0)
                {
                    var key = prevVertexIdx < vertexIdx ? (prevVertexIdx, vertexIdx) : (vertexIdx, prevVertexIdx);
                    if (edgeOwner.TryGetValue(key, out var otherSite))
                    {
                        if (otherSite != siteIdx)
                        {
                            // Add mutual neighbors (avoid duplicates)
                            if (!Cells.Neighbors[siteIdx].Contains(otherSite)) Cells.Neighbors[siteIdx].Add(otherSite);
                            if (!Cells.Neighbors[otherSite].Contains(siteIdx)) Cells.Neighbors[otherSite].Add(siteIdx);
                        }
                    }
                    else
                    {
                        edgeOwner[key] = siteIdx;
                    }
                }

                prevVertexIdx = vertexIdx;
            }

            // Close ring edge (last to first)
            if (Cells.Vertices[siteIdx].Count > 1)
            {
                int first = Cells.Vertices[siteIdx][0];
                int last = Cells.Vertices[siteIdx][Cells.Vertices[siteIdx].Count - 1];
                var key = first < last ? (first, last) : (last, first);
                if (edgeOwner.TryGetValue(key, out var otherSite))
                {
                    if (otherSite != siteIdx)
                    {
                        if (!Cells.Neighbors[siteIdx].Contains(otherSite)) Cells.Neighbors[siteIdx].Add(otherSite);
                        if (!Cells.Neighbors[otherSite].Contains(siteIdx)) Cells.Neighbors[otherSite].Add(siteIdx);
                    }
                }
                else
                {
                    edgeOwner[key] = siteIdx;
                }
            }
        }

        if (diagram is GeometryCollection collection)
        {
            foreach (NtsGeometry geom in collection.Geometries)
            {
                Process(geom);
            }
        }
        else
        {
            Process(diagram);
        }

        // Stats
        int neighborPairs = Cells.Neighbors.Sum(n => n.Count) / 2;
        Console.WriteLine($"Built Voronoi from NTS: {_pointsN} sites, {Vertices.Coordinates.Count} vertices, ~{neighborPairs} adjacencies");
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

        // Process each half-edge in the triangulation (build triangle circumcenters and adjacency)
        for (int e = 0; e < _delaunay.Triangles.Length; e++)
        {
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

        // Build cells (vertices and neighbors) per site using robust per-point edge search
        BuildCellsFromHalfedges();
    }

    /// <summary>
    /// Sorts each cell's vertex indices by polar angle around the site, to form a proper ring.
    /// </summary>
    private void OrderCellVertices()
    {
        for (int p = 0; p < _pointsN; p++)
        {
            var vlist = Cells.Vertices[p];
            if (vlist == null || vlist.Count == 0) continue;

            // Remove invalid indices and duplicates
            var filtered = new List<int>(vlist.Count);
            var seen = new HashSet<int>();
            foreach (var vid in vlist)
            {
                if (vid >= 0 && vid < Vertices.Coordinates.Count && seen.Add(vid))
                    filtered.Add(vid);
            }
            if (filtered.Count < 2) { Cells.Vertices[p] = filtered; continue; }

            var center = _points[p];
            filtered.Sort((a, b) =>
            {
                var va = Vertices.Coordinates[a];
                var vb = Vertices.Coordinates[b];
                double aa = Math.Atan2(va.Y - center.Y, va.X - center.X);
                double ab = Math.Atan2(vb.Y - center.Y, vb.X - center.X);
                return aa.CompareTo(ab);
            });

            Cells.Vertices[p] = filtered;
        }
    }

    /// <summary>
    /// For each site p, find an incident half-edge and walk around it to collect edges, then
    /// derive Voronoi cell vertices (triangle indices) and neighbor sites.
    /// </summary>
    private void BuildCellsFromHalfedges()
    {
        Cells.Vertices = new List<int>[_pointsN];
        Cells.Neighbors = new List<int>[_pointsN];
        Cells.IsBorder = new bool[_pointsN];

        for (int p = 0; p < _pointsN; p++)
        {
            int start = -1;
            // Find a half-edge whose next endpoint is p
            for (int e = 0; e < _delaunay.Triangles.Length; e++)
            {
                if (_delaunay.Triangles[NextHalfedge(e)] == p)
                {
                    start = e;
                    break;
                }
            }
            if (start == -1)
            {
                Cells.Vertices[p] = new List<int>();
                Cells.Neighbors[p] = new List<int>();
                Cells.IsBorder[p] = true;
                continue;
            }

            var edges = EdgesAroundPoint(start);
            var vlist = new List<int>(edges.Count);
            var nset = new HashSet<int>();
            bool border = false;

            foreach (var e in edges)
            {
                int t = TriangleOfEdge(e);
                if (t >= 0 && t < Vertices.Coordinates.Count)
                    vlist.Add(t);

                int n = _delaunay.Triangles[e];
                if (n >= 0 && n < _pointsN && n != p) nset.Add(n);

                if (_delaunay.Halfedges[e] == -1) border = true;
            }

            Cells.Vertices[p] = vlist;
            Cells.Neighbors[p] = nset.ToList();
            Cells.IsBorder[p] = border;
        }

        OrderCellVertices();
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
