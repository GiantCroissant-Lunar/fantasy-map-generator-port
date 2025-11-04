using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using FantasyMapGenerator.Core.Models;
using FmgPoint = FantasyMapGenerator.Core.Models.Point;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// Extended MapData with spatial indexing for fast queries
/// </summary>
public class SpatialMapData : MapData
{
    private STRtree<Cell>? _cellIndex;
    private STRtree<Burg>? _burgIndex;
    private STRtree<River>? _riverIndex;
    private STRtree<State>? _stateIndex;
    private bool _indexBuilt = false;

    public SpatialMapData(int width, int height, int cellsDesired) 
        : base(width, height, cellsDesired)
    {
    }

    /// <summary>
    /// Build spatial indexes for fast queries
    /// </summary>
    public void BuildSpatialIndex()
    {
        _cellIndex = new STRtree<Cell>();
        _burgIndex = new STRtree<Burg>();
        _riverIndex = new STRtree<River>();
        _stateIndex = new STRtree<State>();

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

        // Index rivers by their bounding boxes
        foreach (var river in Rivers)
        {
            var envelope = GetRiverEnvelope(river);
            _riverIndex.Insert(envelope, river);
        }

        // Index states by their bounding boxes
        foreach (var state in States)
        {
            var stateCells = GetStateCells(state.Id).ToList();
            if (stateCells.Count == 0) continue;
            
            var envelope = GetCellsEnvelope(stateCells);
            _stateIndex.Insert(envelope, state);
        }

        _indexBuilt = true;
    }

    /// <summary>
    /// Query cells within a radius of a point
    /// </summary>
    /// <param name="center">Center point</param>
    /// <param name="radius">Search radius</param>
    /// <returns>Cells within radius</returns>
    public List<Cell> QueryCellsInRadius(FmgPoint center, double radius)
    {
        EnsureIndexBuilt();
        
        var envelope = new Envelope(
            center.X - radius,
            center.X + radius,
            center.Y - radius,
            center.Y + radius);

        var candidates = _cellIndex!.Query(envelope);
        
        return candidates
            .Where(c => Distance(c.Center, center) <= radius)
            .ToList();
    }

    /// <summary>
    /// Query cells within a rectangular region
    /// </summary>
    /// <param name="minX">Minimum X coordinate</param>
    /// <param name="minY">Minimum Y coordinate</param>
    /// <param name="maxX">Maximum X coordinate</param>
    /// <param name="maxY">Maximum Y coordinate</param>
    /// <returns>Cells within rectangle</returns>
    public List<Cell> QueryCellsInRectangle(double minX, double minY, double maxX, double maxY)
    {
        EnsureIndexBuilt();
        
        var envelope = new Envelope(minX, maxX, minY, maxY);
        return _cellIndex!.Query(envelope).ToList();
    }

    /// <summary>
    /// Query cells that intersect a geometry
    /// </summary>
    /// <param name="geometry">Geometry to test intersection</param>
    /// <returns>Cells that intersect the geometry</returns>
    public List<Cell> QueryCellsIntersecting(NtsGeometry geometry)
    {
        EnsureIndexBuilt();
        
        var envelope = geometry.EnvelopeInternal;
        var candidates = _cellIndex!.Query(envelope);
        
        var adapter = new NtsGeometryAdapter();
        return candidates
            .Where(c =>
            {
                var cellPoly = adapter.CellToPolygon(c, Vertices);
                return geometry.Intersects(cellPoly);
            })
            .ToList();
    }

    /// <summary>
    /// Query burgs within a radius of a point
    /// </summary>
    /// <param name="center">Center point</param>
    /// <param name="radius">Search radius</param>
    /// <returns>Burgs within radius</returns>
    public List<Burg> QueryBurgsInRadius(FmgPoint center, double radius)
    {
        EnsureIndexBuilt();
        
        var envelope = new Envelope(
            center.X - radius,
            center.X + radius,
            center.Y - radius,
            center.Y + radius);

        var candidates = _burgIndex!.Query(envelope);
        
        return candidates
            .Where(b =>
            {
                var burgPoint = Cells[b.Cell].Center;
                return Distance(burgPoint, center) <= radius;
            })
            .ToList();
    }

    /// <summary>
    /// Query burgs within a geometry
    /// </summary>
    /// <param name="geometry">Geometry to test containment</param>
    /// <returns>Burgs contained within geometry</returns>
    public List<Burg> QueryBurgsInGeometry(NtsGeometry geometry)
    {
        EnsureIndexBuilt();
        
        var envelope = geometry.EnvelopeInternal;
        var candidates = _burgIndex!.Query(envelope);
        
        var factory = new GeometryFactory();
        return candidates
            .Where(b =>
            {
                var burgPoint = Cells[b.Cell].Center;
                var ntsPoint = factory.CreatePoint(new Coordinate(burgPoint.X, burgPoint.Y));
                return geometry.Contains(ntsPoint);
            })
            .ToList();
    }

    /// <summary>
    /// Query rivers that intersect a geometry
    /// </summary>
    /// <param name="geometry">Geometry to test intersection</param>
    /// <returns>Rivers that intersect the geometry</returns>
    public List<River> QueryRiversIntersecting(NtsGeometry geometry)
    {
        EnsureIndexBuilt();
        
        var envelope = geometry.EnvelopeInternal;
        var candidates = _riverIndex!.Query(envelope);
        
        var adapter = new NtsGeometryAdapter();
        return candidates
            .Where(r =>
            {
                var riverLine = adapter.RiverToLineString(r, this);
                return geometry.Intersects(riverLine);
            })
            .ToList();
    }

    /// <summary>
    /// Query states that intersect a geometry
    /// </summary>
    /// <param name="geometry">Geometry to test intersection</param>
    /// <returns>States that intersect the geometry</returns>
    public List<State> QueryStatesIntersecting(NtsGeometry geometry)
    {
        EnsureIndexBuilt();
        
        var envelope = geometry.EnvelopeInternal;
        var candidates = _stateIndex!.Query(envelope);
        
        var boundaryGenerator = new StateBoundaryGenerator();
        return candidates
            .Where(s =>
            {
                var boundary = boundaryGenerator.GetStateBoundary(s.Id, this);
                return geometry.Intersects(boundary);
            })
            .ToList();
    }

    /// <summary>
    /// Find nearest cell to a point
    /// </summary>
    /// <param name="point">Query point</param>
    /// <param name="maxDistance">Maximum search distance</param>
    /// <returns>Nearest cell or null if none found</returns>
    public Cell? FindNearestCell(FmgPoint point, double maxDistance = double.MaxValue)
    {
        EnsureIndexBuilt();
        
        var envelope = new Envelope(
            point.X - maxDistance,
            point.X + maxDistance,
            point.Y - maxDistance,
            point.Y + maxDistance);

        var candidates = _cellIndex!.Query(envelope);
        
        Cell? nearest = null;
        var minDistance = maxDistance;

        foreach (var cell in candidates)
        {
            var distance = Distance(cell.Center, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = cell;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Find nearest burg to a point
    /// </summary>
    /// <param name="point">Query point</param>
    /// <param name="maxDistance">Maximum search distance</param>
    /// <returns>Nearest burg or null if none found</returns>
    public Burg? FindNearestBurg(FmgPoint point, double maxDistance = double.MaxValue)
    {
        EnsureIndexBuilt();
        
        var envelope = new Envelope(
            point.X - maxDistance,
            point.X + maxDistance,
            point.Y - maxDistance,
            point.Y + maxDistance);

        var candidates = _burgIndex!.Query(envelope);
        
        Burg? nearest = null;
        var minDistance = maxDistance;

        foreach (var burg in candidates)
        {
            var burgPoint = Cells[burg.Cell].Center;
            var distance = Distance(burgPoint, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = burg;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Get spatial index statistics
    /// </summary>
    public SpatialIndexStatistics GetIndexStatistics()
    {
        if (!_indexBuilt)
        {
            return new SpatialIndexStatistics
            {
                IndexBuilt = false,
                IndexedCells = 0,
                IndexedBurgs = 0,
                IndexedRivers = 0,
                IndexedStates = 0
            };
        }

        return new SpatialIndexStatistics
        {
            IndexBuilt = true,
            IndexedCells = Cells.Count,
            IndexedBurgs = Burgs.Count,
            IndexedRivers = Rivers.Count,
            IndexedStates = States.Count,
            MemoryUsage = EstimateMemoryUsage()
        };
    }

    /// <summary>
    /// Clear spatial indexes
    /// </summary>
    public void ClearSpatialIndex()
    {
        _cellIndex = null;
        _burgIndex = null;
        _riverIndex = null;
        _stateIndex = null;
        _indexBuilt = false;
    }

    /// <summary>
    /// Check if spatial index is built
    /// </summary>
    public bool IsIndexBuilt => _indexBuilt;

    private void EnsureIndexBuilt()
    {
        if (!_indexBuilt)
        {
            throw new InvalidOperationException("Spatial index not built. Call BuildSpatialIndex() first.");
        }
    }

    private Envelope GetCellEnvelope(Cell cell)
    {
        if (cell.Vertices.Count == 0)
        {
            // Fallback to cell center with small buffer
            return new Envelope(
                cell.Center.X - 0.1, cell.Center.X + 0.1,
                cell.Center.Y - 0.1, cell.Center.Y + 0.1);
        }

        var xs = cell.Vertices.Select(i => Vertices[i].X);
        var ys = cell.Vertices.Select(i => Vertices[i].Y);

        return new Envelope(
            xs.Min(), xs.Max(),
            ys.Min(), ys.Max());
    }

    private Envelope GetRiverEnvelope(River river)
    {
        if (river.Cells.Count == 0)
        {
            return new Envelope();
        }

        var xs = river.Cells.Select(cellId => Cells[cellId].Center.X);
        var ys = river.Cells.Select(cellId => Cells[cellId].Center.Y);

        return new Envelope(
            xs.Min(), xs.Max(),
            ys.Min(), ys.Max());
    }

    private Envelope GetCellsEnvelope(IEnumerable<Cell> cells)
    {
        var cellList = cells.ToList();
        if (cellList.Count == 0)
        {
            return new Envelope();
        }

        // Filter out cells with no vertices
        var cellsWithVertices = cellList.Where(c => c.Vertices.Count > 0).ToList();
        if (cellsWithVertices.Count == 0)
        {
            // Fallback to cell centers
            var centerXs = cellList.Select(c => c.Center.X);
            var centerYs = cellList.Select(c => c.Center.Y);
            return new Envelope(
                centerXs.Min(), centerXs.Max(),
                centerYs.Min(), centerYs.Max());
        }

        var vertexXs = cellsWithVertices.SelectMany(c => c.Vertices.Select(v => Vertices[v].X));
        var vertexYs = cellsWithVertices.SelectMany(c => c.Vertices.Select(v => Vertices[v].Y));

        return new Envelope(
            vertexXs.Min(), vertexXs.Max(),
            vertexYs.Min(), vertexYs.Max());
    }

    private double Distance(FmgPoint p1, FmgPoint p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private long EstimateMemoryUsage()
    {
        // Rough estimate: each item in index ~100 bytes
        long memory = 0;
        if (_cellIndex != null) memory += Cells.Count * 100;
        if (_burgIndex != null) memory += Burgs.Count * 100;
        if (_riverIndex != null) memory += Rivers.Count * 100;
        if (_stateIndex != null) memory += States.Count * 100;
        
        return memory;
    }
}

/// <summary>
/// Statistics for spatial indexes
/// </summary>
public class SpatialIndexStatistics
{
    public bool IndexBuilt { get; set; }
    public int IndexedCells { get; set; }
    public int IndexedBurgs { get; set; }
    public int IndexedRivers { get; set; }
    public int IndexedStates { get; set; }
    public long MemoryUsage { get; set; }
}