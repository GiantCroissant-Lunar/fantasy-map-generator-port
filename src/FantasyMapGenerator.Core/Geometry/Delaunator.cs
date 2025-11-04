using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// C# port of Delaunator - JavaScript library for Delaunay triangulation of 2D points
/// Based on https://github.com/mapbox/delaunator
/// </summary>
public class Delaunator
{
    private readonly double[] _coords;
    private readonly int _hashSize;
    private readonly int[] _hullPrev;
    private readonly int[] _hullNext;
    private readonly int[] _hullTri;
    private readonly int[] _hullHash;
    
    public int[] Triangles { get; private set; }
    public int[] Halfedges { get; private set; }
    public int HullStart { get; private set; }
    public int HullSize { get; private set; }
    
    public Delaunator(double[] coords)
    {
        if (coords.Length % 2 != 0)
            throw new ArgumentException("Coordinates length must be even");
        
        _coords = coords;
        int n = coords.Length >> 1;
        
        // arrays that will store the triangulation graph
        int maxTriangles = Math.Max(2 * n - 5, 0);
        Triangles = new int[maxTriangles * 3];
        Halfedges = new int[maxTriangles * 3];
        
        // temporary arrays for tracking the edges of the advancing convex hull
        _hashSize = (int)Math.Ceiling(Math.Sqrt(n));
        _hullPrev = new int[n];
        _hullNext = new int[n];
        _hullTri = new int[n];
        _hullHash = new int[_hashSize];
        
        // populate an array of point indices; calculate input data bbox
        int[] ids = new int[n];
        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;
        
        for (int i = 0; i < n; i++)
        {
            double x = coords[2 * i];
            double y = coords[2 * i + 1];
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
            ids[i] = i;
        }
        
        double cx = (minX + maxX) / 2;
        double cy = (minY + maxY) / 2;
        
        double minDist = double.PositiveInfinity;
        int i0 = 0, i1 = 0, i2 = 0;
        
        // pick a seed point close to the centroid
        for (int i = 0; i < n; i++)
        {
            double d = Dist(cx, cy, coords[2 * i], coords[2 * i + 1]);
            if (d < minDist)
            {
                i0 = i;
                minDist = d;
            }
        }
        
        minDist = double.PositiveInfinity;
        
        // find the point closest to the seed
        for (int i = 0; i < n; i++)
        {
            if (i == i0) continue;
            double d = Dist(coords[2 * i0], coords[2 * i0 + 1], coords[2 * i], coords[2 * i + 1]);
            if (d < minDist && d > 0)
            {
                i1 = i;
                minDist = d;
            }
        }
        
        double minRadius = double.PositiveInfinity;
        
        // find the third point which forms the smallest circumcircle with the first two
        for (int i = 0; i < n; i++)
        {
            if (i == i0 || i == i1) continue;
            
            double r = Circumradius(
                coords[2 * i0], coords[2 * i0 + 1],
                coords[2 * i1], coords[2 * i1 + 1],
                coords[2 * i], coords[2 * i + 1]);
            
            if (r < minRadius)
            {
                i2 = i;
                minRadius = r;
            }
        }
        
        if (minRadius == double.PositiveInfinity)
        {
            // order the collinear points by distance (for the hull)
            Array.Sort(ids, (a, b) => 
            {
                double da = coords[2 * a] - coords[2 * b];
                double db = coords[2 * a + 1] - coords[2 * b + 1];
                return (da * da + db * db).CompareTo(0);
            });
            
            i0 = ids[0];
            i1 = ids[1];
            i2 = ids[2];
            
            // create a "degenerate" triangle
            Triangles[0] = i0;
            Triangles[1] = i1;
            Triangles[2] = i2;
            Halfedges[0] = -1;
            Halfedges[1] = -1;
            Halfedges[2] = -1;
            
            HullStart = i0;
            HullSize = 3;
            
            _hullNext[i0] = _hullPrev[i2] = i1;
            _hullNext[i1] = _hullPrev[i0] = i2;
            _hullNext[i2] = _hullPrev[i1] = i0;
            
            return;
        }
        
        // swap the order of the seed points for counter-clockwise orientation
        if (Orient(
            coords[2 * i0], coords[2 * i0 + 1],
            coords[2 * i1], coords[2 * i1 + 1],
            coords[2 * i2], coords[2 * i2 + 1]) < 0)
        {
            int temp = i1;
            i1 = i2;
            i2 = temp;
        }
        
        // create a "supertriangle" that encompasses all points
        double dist = minRadius * 3;
        double x0 = cx - dist;
        double y0 = cy - dist;
        double x1 = cx + dist;
        double y1 = cy + dist;
        
        int i3 = n;
        int i4 = n + 1;
        int i5 = n + 2;
        
        Array.Resize(ref _coords, coords.Length + 6);
        _coords[2 * i3] = x0;
        _coords[2 * i3 + 1] = y0;
        _coords[2 * i4] = x1;
        _coords[2 * i4 + 1] = y0;
        _coords[2 * i5] = x1;
        _coords[2 * i5 + 1] = y1;
        
        // initialize the triangulation
        int trianglesLen = 0;
        AddTriangle(i0, i1, i2, -1, -1, -1, ref trianglesLen);
        
        int[] hull = new int[3];
        int hullLen = 0;
        
        // associate each point with the triangle that contains it
        int[] points = new int[n];
        for (int i = 0; i < n; i++)
        {
            points[i] = i;
        }
        
        // sort the points by distance from the first seed point
        Array.Sort(points, (a, b) => 
        {
            double da = coords[2 * a] - coords[2 * b];
            double db = coords[2 * a + 1] - coords[2 * b + 1];
            double dc = coords[2 * b] - coords[2 * a];
            double dd = coords[2 * b + 1] - coords[2 * a + 1];
            return (da * da + db * db).CompareTo(dc * dc + dd * dd);
        });
        
        // incrementally add points to the triangulation
        for (int k = 0; k < n; k++)
        {
            int i = points[k];
            double x = coords[2 * i];
            double y = coords[2 * i + 1];
            
            // skip duplicate points
            if (x == coords[2 * i0] && y == coords[2 * i0 + 1] ||
                x == coords[2 * i1] && y == coords[2 * i1 + 1] ||
                x == coords[2 * i2] && y == coords[2 * i2 + 1])
                continue;
            
            // find the first triangle that contains the point
            int start = 0;
            for (int j = 0; j < trianglesLen; j++)
            {
                if (InTriangle(coords, i, Triangles[j * 3], Triangles[j * 3 + 1], Triangles[j * 3 + 2]))
                {
                    start = j;
                    break;
                }
            }
            
            // walk through the triangulation to find the containing triangle
            int edge = start;
            while (true)
            {
                int e = edge * 3;
                int a = Triangles[e];
                int b = Triangles[e + 1];
                int c = Triangles[e + 2];
                
                if (Orient(x, y, coords[2 * a], coords[2 * a + 1], coords[2 * b], coords[2 * b + 1]) >= 0 &&
                    Orient(x, y, coords[2 * b], coords[2 * b + 1], coords[2 * c], coords[2 * c + 1]) >= 0 &&
                    Orient(x, y, coords[2 * c], coords[2 * c + 1], coords[2 * a], coords[2 * a + 1]) >= 0)
                {
                    break;
                }
                
                edge = Halfedges[e];
                if (edge == -1)
                {
                    // point is on the convex hull
                    edge = start;
                    while (true)
                    {
                        e = edge * 3;
                        int next = Halfedges[e + 2];
                        if (next == -1) break;
                        edge = next;
                    }
                    break;
                }
            }
            
            // remove triangles that contain the point
            int[] edges = new int[32];
            int edgesLen = 0;
            
            int currentEdge = edge;
            while (true)
            {
                int e = currentEdge * 3;
                int a = Triangles[e];
                int b = Triangles[e + 1];
                int c = Triangles[e + 2];
                
                if (Orient(x, y, coords[2 * a], coords[2 * a + 1], coords[2 * b], coords[2 * b + 1]) < 0)
                {
                    if (edgesLen == edges.Length) Array.Resize(ref edges, edgesLen * 2);
                    edges[edgesLen++] = e;
                    currentEdge = Halfedges[e + 2];
                }
                else if (Orient(x, y, coords[2 * b], coords[2 * b + 1], coords[2 * c], coords[2 * c + 1]) < 0)
                {
                    if (edgesLen == edges.Length) Array.Resize(ref edges, edgesLen * 2);
                    edges[edgesLen++] = e + 1;
                    currentEdge = Halfedges[e];
                }
                else if (Orient(x, y, coords[2 * c], coords[2 * c + 1], coords[2 * a], coords[2 * a + 1]) < 0)
                {
                    if (edgesLen == edges.Length) Array.Resize(ref edges, edgesLen * 2);
                    edges[edgesLen++] = e + 2;
                    currentEdge = Halfedges[e + 1];
                }
                else
                {
                    break;
                }
            }
            
            // remove the triangles
            for (int j = 0; j < edgesLen; j++)
            {
                int e = edges[j];
                int opposite = Halfedges[e];
                if (opposite != -1)
                {
                    int oe = opposite;
                    Halfedges[oe] = -1;
                }
                Halfedges[e] = -1;
            }
            
            // create new triangles
            int first = edges[0];
            int last = edges[edgesLen - 1];
            
            for (int j = 0; j < edgesLen; j++)
            {
                int e = edges[j];
                int a = Triangles[e];
                int b = Triangles[(e + 1) % 3 + (e / 3) * 3];
                
                int t = AddTriangle(i, a, b, -1, -1, -1, ref trianglesLen);
                
                if (j > 0)
                {
                    int prev = edges[j - 1];
                    int prevT = Triangles[prev];
                    Halfedges[t] = prev;
                    Halfedges[prev] = t;
                }
                
                if (j < edgesLen - 1)
                {
                    int next = edges[j + 1];
                    int nextT = Triangles[next];
                    Halfedges[t + 2] = next;
                    Halfedges[next] = t + 2;
                }
            }
            
            // link the first and last triangles
            Halfedges[first] = last;
            Halfedges[last] = first;
        }
        
        // remove the supertriangle
        for (int i = 0; i < trianglesLen; i++)
        {
            int e = i * 3;
            if (Triangles[e] >= n || Triangles[e + 1] >= n || Triangles[e + 2] >= n)
            {
                RemoveTriangle(i, ref trianglesLen);
            }
        }
        
        // build the convex hull
        Array.Fill(_hullHash, -1);
        hullLen = 0;
        
        for (int i = 0; i < trianglesLen; i++)
        {
            int e = i * 3;
            for (int j = 0; j < 3; j++)
            {
                if (Halfedges[e + j] == -1)
                {
                    int a = Triangles[e + j];
                    int b = Triangles[e + (j + 1) % 3];
                    
                    if (hullLen == hull.Length) Array.Resize(ref hull, hullLen * 2);
                    hull[hullLen++] = a;
                    
                    _hullNext[a] = b;
                    _hullPrev[b] = a;
                    _hullTri[a] = i;
                    
                    int hash = HashPoint(coords[2 * a], coords[2 * a + 1]);
                    _hullHash[hash] = a;
                }
            }
        }
        
        HullStart = hull[0];
        HullSize = hullLen;
    }
    
    private int AddTriangle(int i0, int i1, int i2, int a, int b, int c, ref int trianglesLen)
    {
        int t = trianglesLen;
        
        Triangles[t * 3] = i0;
        Triangles[t * 3 + 1] = i1;
        Triangles[t * 3 + 2] = i2;
        
        Halfedges[t * 3] = a;
        Halfedges[t * 3 + 1] = b;
        Halfedges[t * 3 + 2] = c;
        
        trianglesLen++;
        return t;
    }
    
    private void RemoveTriangle(int t, ref int trianglesLen)
    {
        int e = t * 3;
        
        for (int i = 0; i < 3; i++)
        {
            int opposite = Halfedges[e + i];
            if (opposite != -1)
            {
                Halfedges[opposite] = -1;
            }
            Halfedges[e + i] = -1;
        }
        
        // move the last triangle to the removed position
        int last = trianglesLen - 1;
        if (t != last)
        {
            int le = last * 3;
            for (int i = 0; i < 3; i++)
            {
                Triangles[e + i] = Triangles[le + i];
                Halfedges[e + i] = Halfedges[le + i];
                
                int opposite = Halfedges[le + i];
                if (opposite != -1)
                {
                    Halfedges[opposite] = e + i;
                }
            }
        }
        
        trianglesLen--;
    }
    
    private static double Dist(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx;
        double dy = ay - by;
        return dx * dx + dy * dy;
    }
    
    private static double Orient(double px, double py, double qx, double qy, double rx, double ry)
    {
        return (qy - py) * (rx - qx) - (qx - px) * (ry - qy);
    }
    
    private static double Circumradius(double ax, double ay, double bx, double by, double cx, double cy)
    {
        double dx = bx - ax;
        double dy = by - ay;
        double ex = cx - ax;
        double ey = cy - ay;
        
        double bl = dx * dx + dy * dy;
        double cl = ex * ex + ey * ey;
        double d = dx * ey - dy * ex;
        
        double x = (ey * bl - dy * cl) * 0.5 / d;
        double y = (dx * cl - ex * bl) * 0.5 / d;
        
        return x * x + y * y;
    }
    
    private static bool InTriangle(double[] coords, int px, int ax, int bx, int cx)
    {
        double py = coords[2 * px + 1];
        double ay = coords[2 * ax + 1];
        double by = coords[2 * bx + 1];
        double cy = coords[2 * cx + 1];
        
        return (ay > py) != (by > py) && (ay > py) != (cy > py) &&
               (ax - px) * (by - ay) < (bx - ax) * (py - ay) &&
               (bx - px) * (cy - by) < (cx - bx) * (py - by);
    }
    
    private int HashPoint(double x, double y)
    {
        int hash = (int)(Math.Floor(x) * 73856093) ^ (int)(Math.Floor(y) * 19349663);
        return hash % _hashSize;
    }
}