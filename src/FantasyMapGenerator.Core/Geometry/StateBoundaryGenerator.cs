using FantasyMapGenerator.Core.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// Generates clean state boundaries using NTS operations
/// </summary>
public class StateBoundaryGenerator
{
    private readonly NtsGeometryAdapter _adapter;
    private readonly Dictionary<int, NtsGeometry> _boundaryCache;

    public StateBoundaryGenerator()
    {
        _adapter = new NtsGeometryAdapter();
        _boundaryCache = new Dictionary<int, NtsGeometry>();
    }

    /// <summary>
    /// Get state boundary with optional simplification and smoothing
    /// </summary>
    /// <param name="stateId">The state ID</param>
    /// <param name="map">The map data</param>
    /// <param name="simplificationTolerance">Simplification tolerance (0 = no simplification)</param>
    /// <param name="smoothRadius">Smoothing radius (0 = no smoothing)</param>
    /// <returns>State boundary geometry</returns>
    public NtsGeometry GetStateBoundary(
        int stateId,
        MapData map,
        double simplificationTolerance = 0.5,
        double smoothRadius = 0.0)
    {
        // Check cache first
        if (_boundaryCache.TryGetValue(stateId, out var cached))
        {
            return cached;
        }

        var stateCells = map.GetStateCells(stateId).ToList();

        if (stateCells.Count == 0)
        {
            return new GeometryFactory().CreatePolygon();
        }

        // Union all cells into single geometry
        var boundary = _adapter.UnionCells(stateCells, map.Vertices);

        // Apply smoothing if requested
        if (smoothRadius > 0)
        {
            boundary = boundary.Buffer(smoothRadius).Buffer(-smoothRadius);
        }

        // Apply simplification if requested
        if (simplificationTolerance > 0)
        {
            boundary = DouglasPeuckerSimplifier.Simplify(boundary, simplificationTolerance);
        }

        // Cache result
        _boundaryCache[stateId] = boundary;
        return boundary;
    }

    /// <summary>
    /// Get all state boundaries
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="simplificationTolerance">Simplification tolerance</param>
    /// <param name="smoothRadius">Smoothing radius</param>
    /// <returns>Dictionary of state ID to boundary geometry</returns>
    public Dictionary<int, NtsGeometry> GetAllStateBoundaries(
        MapData map,
        double simplificationTolerance = 0.5,
        double smoothRadius = 0.0)
    {
        var boundaries = new Dictionary<int, NtsGeometry>();

        foreach (var state in map.States)
        {
            boundaries[state.Id] = GetStateBoundary(state.Id, map, simplificationTolerance, smoothRadius);
        }

        return boundaries;
    }

    /// <summary>
    /// Get state boundaries with adaptive parameters based on state size
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="baseSimplification">Base simplification tolerance</param>
    /// <param name="baseSmoothing">Base smoothing radius</param>
    /// <returns>Dictionary of state ID to boundary geometry</returns>
    public Dictionary<int, NtsGeometry> GetAllStateBoundariesAdaptive(
        MapData map,
        double baseSimplification = 0.5,
        double baseSmoothing = 0.2)
    {
        var boundaries = new Dictionary<int, NtsGeometry>();
        var totalCells = map.Cells.Count;

        foreach (var state in map.States)
        {
            var stateCells = map.GetStateCells(state.Id).ToList();
            if (stateCells.Count == 0) continue;

            // Calculate adaptive parameters based on state size
            var stateSizeRatio = (double)stateCells.Count / totalCells;

            // Larger states get more simplification and smoothing
            var adaptiveSimplification = baseSimplification * Math.Sqrt(stateSizeRatio) * 2;
            var adaptiveSmoothing = baseSmoothing * Math.Sqrt(stateSizeRatio);

            boundaries[state.Id] = GetStateBoundary(
                state.Id,
                map,
                adaptiveSimplification,
                adaptiveSmoothing);
        }

        return boundaries;
    }

    /// <summary>
    /// Get state border lines (not filled polygons)
    /// </summary>
    /// <param name="stateId">The state ID</param>
    /// <param name="map">The map data</param>
    /// <returns>MultiLineString representing state borders</returns>
    public MultiLineString GetStateBorders(int stateId, MapData map)
    {
        return _adapter.GetStateBorders(stateId, map);
    }

    /// <summary>
    /// Get all state borders as a single MultiLineString
    /// </summary>
    /// <param name="map">The map data</param>
    /// <returns>MultiLineString containing all state borders</returns>
    public MultiLineString GetAllStateBorders(MapData map)
    {
        var allBorders = new List<LineString>();

        foreach (var state in map.States)
        {
            var stateBorders = GetStateBorders(state.Id, map);
            allBorders.AddRange(stateBorders.Geometries.Cast<LineString>());
        }

        return new GeometryFactory().CreateMultiLineString(allBorders.ToArray());
    }

    /// <summary>
    /// Calculate state statistics
    /// </summary>
    /// <param name="map">The map data</param>
    /// <returns>Dictionary of state ID to statistics</returns>
    public Dictionary<int, StateStatistics> CalculateStateStatistics(MapData map)
    {
        var statistics = new Dictionary<int, StateStatistics>();

        foreach (var state in map.States)
        {
            var boundary = GetStateBoundary(state.Id, map);
            var stateCells = map.GetStateCells(state.Id).ToList();

            var stats = new StateStatistics
            {
                StateId = state.Id,
                StateName = state.Name,
                Area = boundary.Area,
                Perimeter = boundary.Length,
                CellCount = stateCells.Count,
                LandCells = stateCells.Count(c => c.IsLand),
                OceanCells = stateCells.Count(c => c.IsOcean),
                AverageElevation = stateCells.Average(c => c.Height),
                TotalPopulation = stateCells.Sum(c => c.Population),
                BurgCount = stateCells.Count(c => c.Burg >= 0),
                HasCoastline = stateCells.Any(c => c.IsLand &&
                    c.Neighbors.Any(n => n >= 0 && map.Cells[n].IsOcean))
            };

            statistics[state.Id] = stats;
        }

        return statistics;
    }

    /// <summary>
    /// Optimize state boundaries for performance
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="maxVertices">Maximum vertices per boundary</param>
    /// <returns>Dictionary of state ID to optimized boundary</returns>
    public Dictionary<int, NtsGeometry> OptimizeBoundaries(MapData map, int maxVertices = 1000)
    {
        var optimized = new Dictionary<int, NtsGeometry>();

        foreach (var state in map.States)
        {
            var boundary = GetStateBoundary(state.Id, map);

            // Calculate required simplification
            var currentVertices = CountVertices(boundary);
            if (currentVertices <= maxVertices)
            {
                optimized[state.Id] = boundary;
                continue;
            }

            // Binary search for appropriate tolerance
            var tolerance = FindOptimalTolerance(boundary, maxVertices);
            var simplified = DouglasPeuckerSimplifier.Simplify(boundary, tolerance);

            optimized[state.Id] = simplified;
        }

        return optimized;
    }

    /// <summary>
    /// Clear boundary cache
    /// </summary>
    public void ClearCache()
    {
        _boundaryCache.Clear();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        return new CacheStatistics
        {
            CachedBoundaries = _boundaryCache.Count,
            MemoryUsage = EstimateMemoryUsage()
        };
    }

    private int CountVertices(NtsGeometry geometry)
    {
        return geometry.NumPoints;
    }

    private double FindOptimalTolerance(NtsGeometry geometry, int maxVertices)
    {
        double minTolerance = 0.0;
        double maxTolerance = 10.0;
        double tolerance = 1.0;

        for (int i = 0; i < 10; i++) // Max 10 iterations
        {
            var simplified = DouglasPeuckerSimplifier.Simplify(geometry, tolerance);
            var vertices = CountVertices(simplified);

            if (vertices <= maxVertices)
            {
                maxTolerance = tolerance;
            }
            else
            {
                minTolerance = tolerance;
            }

            tolerance = (minTolerance + maxTolerance) / 2;
        }

        return maxTolerance;
    }

    private long EstimateMemoryUsage()
    {
        // Rough estimate: each boundary ~1KB
        return _boundaryCache.Count * 1024;
    }
}

/// <summary>
/// Statistics for a state
/// </summary>
public class StateStatistics
{
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public double Area { get; set; }
    public double Perimeter { get; set; }
    public int CellCount { get; set; }
    public int LandCells { get; set; }
    public int OceanCells { get; set; }
    public double AverageElevation { get; set; }
    public int TotalPopulation { get; set; }
    public int BurgCount { get; set; }
    public bool HasCoastline { get; set; }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public int CachedBoundaries { get; set; }
    public long MemoryUsage { get; set; }
}
