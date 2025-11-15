using FantasyMapGenerator.Core.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using FmgPoint = FantasyMapGenerator.Core.Models.Point;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
using NtsPoint = NetTopologySuite.Geometries.Point;

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
    /// Convert a FMG Point to NTS Coordinate
    /// </summary>
    public static Coordinate ToCoordinate(FmgPoint p) => new(p.X, p.Y);

    /// <summary>
    /// Convert NTS Coordinate to FMG Point
    /// </summary>
    public static FmgPoint ToPoint(Coordinate c) => new(c.X, c.Y);

    /// <summary>
    /// Convert a FMG cell to NTS polygon
    /// </summary>
    public Polygon CellToPolygon(Cell cell, List<FmgPoint> vertices)
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
    public MultiPolygon CellsToMultiPolygon(IEnumerable<Cell> cells, List<FmgPoint> vertices)
    {
        var polygons = cells
            .Select(c => CellToPolygon(c, vertices))
            .ToArray();

        return _factory.CreateMultiPolygon(polygons);
    }

    /// <summary>
    /// Union all cells into a single geometry (may be MultiPolygon)
    /// </summary>
    public NtsGeometry UnionCells(IEnumerable<Cell> cells, List<FmgPoint> vertices)
    {
        var geometries = cells
            .Select(c => (NtsGeometry)CellToPolygon(c, vertices))
            .ToList();

        return UnaryUnionOp.Union(geometries);
    }

    /// <summary>
    /// Get cells that intersect a geometry
    /// </summary>
    public List<Cell> GetIntersectingCells(NtsGeometry geometry, MapData map)
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
    /// Get cells that are completely contained within a geometry
    /// </summary>
    public List<Cell> GetContainedCells(NtsGeometry geometry, MapData map)
    {
        var contained = new List<Cell>();

        foreach (var cell in map.Cells)
        {
            var cellPoly = CellToPolygon(cell, map.Vertices);

            if (geometry.Contains(cellPoly))
            {
                contained.Add(cell);
            }
        }

        return contained;
    }

    /// <summary>
    /// Get cells whose center is contained within a geometry
    /// </summary>
    public List<Cell> GetCenterContainedCells(NtsGeometry geometry, MapData map)
    {
        var contained = new List<Cell>();

        foreach (var cell in map.Cells)
        {
            var cellPoly = CellToPolygon(cell, map.Vertices);
            var cellCenter = cellPoly.Centroid;

            if (geometry.Contains(cellCenter))
            {
                contained.Add(cell);
            }
        }

        return contained;
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
        var stateCells = map.GetStateCells(stateId).ToList();
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
                    .FirstOrDefault(n => n >= 0 && !cellSet.Contains(n));

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

    /// <summary>
    /// Create a point geometry from a FMG Point
    /// </summary>
    public NtsPoint CreatePoint(FmgPoint point)
    {
        return _factory.CreatePoint(new Coordinate(point.X, point.Y));
    }

    /// <summary>
    /// Create a buffer around a geometry
    /// </summary>
    public NtsGeometry Buffer(NtsGeometry geometry, double distance)
    {
        return geometry.Buffer(distance);
    }

    /// <summary>
    /// Simplify a geometry using Douglas-Peucker algorithm
    /// </summary>
    public NtsGeometry Simplify(NtsGeometry geometry, double tolerance)
    {
        return NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(geometry, tolerance);
    }

    /// <summary>
    /// Validate geometry and optionally fix it
    /// </summary>
    public bool IsValid(NtsGeometry geometry)
    {
        return geometry.IsValid;
    }

    /// <summary>
    /// Fix invalid geometry
    /// </summary>
    public NtsGeometry FixGeometry(NtsGeometry geometry)
    {
        if (geometry.IsValid) return geometry;

        return NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(geometry);
    }

    /// <summary>
    /// Get the envelope (bounding box) of a geometry
    /// </summary>
    public Envelope GetEnvelope(NtsGeometry geometry)
    {
        return geometry.EnvelopeInternal;
    }

    /// <summary>
    /// Calculate distance between two geometries
    /// </summary>
    public double Distance(NtsGeometry geom1, NtsGeometry geom2)
    {
        return geom1.Distance(geom2);
    }

    /// <summary>
    /// Check if two geometries intersect
    /// </summary>
    public bool Intersects(NtsGeometry geom1, NtsGeometry geom2)
    {
        return geom1.Intersects(geom2);
    }

    /// <summary>
    /// Check if one geometry contains another
    /// </summary>
    public bool Contains(NtsGeometry container, NtsGeometry contained)
    {
        return container.Contains(contained);
    }

    /// <summary>
    /// Get the intersection of two geometries
    /// </summary>
    public NtsGeometry Intersection(NtsGeometry geom1, NtsGeometry geom2)
    {
        return geom1.Intersection(geom2);
    }

    /// <summary>
    /// Get the union of two geometries
    /// </summary>
    public NtsGeometry Union(NtsGeometry geom1, NtsGeometry geom2)
    {
        return geom1.Union(geom2);
    }

    /// <summary>
    /// Get the difference of two geometries (geom1 - geom2)
    /// </summary>
    public NtsGeometry Difference(NtsGeometry geom1, NtsGeometry geom2)
    {
        return geom1.Difference(geom2);
    }

    /// <summary>
    /// Get symmetric difference of two geometries
    /// </summary>
    public NtsGeometry SymDifference(NtsGeometry geom1, NtsGeometry geom2)
    {
        return geom1.SymmetricDifference(geom2);
    }
}
