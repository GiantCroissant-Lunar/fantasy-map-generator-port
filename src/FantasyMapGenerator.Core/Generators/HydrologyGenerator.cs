using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates rivers, lakes, and hydrology features using flow accumulation algorithms
/// </summary>
public class HydrologyGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;

    // Flow direction: cell ID → downhill neighbor ID (-1 if ocean/pit)
    private readonly Dictionary<int, int> _flowDirection;

    // Flow accumulation: cell ID → upstream cell count
    private readonly Dictionary<int, int> _flowAccumulation;
    private int _lakesFilled;
    private int _riverThreshold;
    private int _sourcesCount;
    private int _rejectedShort;
    // Tuning
    private double _precipScale = 50.0;
    private int _minFlux = 30;
    private int _minRiverLength = 3;
    private bool _autoAdjust = true;
    private int _targetRivers = 10;
    private int _minThreshold = 8;

    public HydrologyGenerator(MapData map, IRandomSource random)
    {
        _map = map;
        _random = random;
        _flowDirection = new Dictionary<int, int>();
        _flowAccumulation = new Dictionary<int, int>();
    }

    public void SetOptions(double precipScale, int minFlux, int minRiverLength, bool autoAdjust, int targetRivers, int minThreshold)
    {
        _precipScale = precipScale;
        _minFlux = minFlux;
        _minRiverLength = Math.Max(1, minRiverLength);
        _autoAdjust = autoAdjust;
        _targetRivers = Math.Max(0, targetRivers);
        _minThreshold = Math.Max(1, minThreshold);
    }

    /// <summary>
    /// Generate complete hydrology system for the map
    /// </summary>
    public void Generate()
    {
        Console.WriteLine("Generating hydrology...");

        // Step 1: Fill pits to ensure proper drainage
        FillPits();

        // Step 2: Calculate flow directions
        CalculateFlowDirections();

        // Step 3: Calculate flow accumulation
        CalculateFlowAccumulation();

        // Step 4: Generate rivers
        GenerateRivers();

        // Step 5: Identify lakes
        IdentifyLakes();

        // Step 6: Calculate river widths
        CalculateRiverWidths();

        // Step 7: Generate advanced features
        GenerateDeltas();
        IdentifySeasonalRivers();
        GenerateRiverNames(_random);

        Console.WriteLine($"Generated {_map.Rivers.Count} rivers");

        // Build diagnostics report
        try
        {
            var report = new HydrologyReport();
            report.RiverFormationThreshold = _riverThreshold;
            report.CandidateSources = _sourcesCount;
            report.RiversGenerated = _map.Rivers.Count;
            report.RiversRejectedTooShort = _rejectedShort;
            report.LakesFilled = _lakesFilled;
            // Direction assignment stats
            int land = _map.Cells.Count(c => c.IsLand);
            int withDownhill = _flowDirection.Count(kv => kv.Value >= 0 && _map.Cells[kv.Key].Height > 0);
            report.TotalLandCells = land;
            report.DownhillAssigned = withDownhill;

            // Accumulation quantiles for land cells only
            var acc = _flowAccumulation
                .Where(kv => _map.Cells[kv.Key].Height > 0)
                .Select(kv => kv.Value)
                .OrderBy(v => v)
                .ToArray();
            if (acc.Length > 0)
            {
                int Q(double p) => acc[(int)Math.Clamp(Math.Round(p * (acc.Length - 1)), 0, acc.Length - 1)];
                report.AccumulationQuantiles = new[] { Q(0.05), Q(0.25), Q(0.50), Q(0.75), Q(0.95) };
            }

            // Top sources (by accumulation)
            var top = _flowAccumulation
                .OrderByDescending(kv => kv.Value)
                .Take(20)
                .Select(kv => (kv.Key, kv.Value))
                .ToList();
            foreach (var (cid, val) in top)
                report.TopSources.Add((cid, val));

            // Per-river basics
            foreach (var river in _map.Rivers)
            {
                int maxAcc = river.Cells.Select(id => _flowAccumulation.GetValueOrDefault(id, 1)).DefaultIfEmpty(1).Max();
                report.Rivers.Add((river.Id, river.Cells?.Count ?? 0, maxAcc));
            }

            _map.Hydrology = report;
        }
        catch { }
    }

    /// <summary>
    /// Fill pits to ensure proper drainage (Priority Flood algorithm)
    /// Creates lakes at appropriate depressions
    /// </summary>
    private void FillPits()
    {
        // Priority queue: process lowest elevations first
        var queue = new PriorityQueue<int, int>();
        var processed = new HashSet<int>();

        // Seed with ocean and map border cells
        foreach (var cell in _map.Cells)
        {
            if (cell.IsOcean || cell.IsBorder)
            {
                queue.Enqueue(cell.Id, cell.Height);
            }
        }

        int lakesFilled = 0;

        while (queue.TryDequeue(out var cellId, out var elevation))
        {
            if (processed.Contains(cellId))
                continue;

            processed.Add(cellId);

            var cell = _map.Cells[cellId];

            // Ensure cell is at least as high as its processed neighbors
            if (cell.Height < elevation)
            {
                // This is a pit - fill it to create a lake
                int originalHeight = cell.Height;
                cell.Height = (byte)elevation;

                if (originalHeight < elevation - 2)
                {
                    // Significant depression - mark as lake
                    lakesFilled++;
                }
            }

            // Process neighbors
            foreach (var neighborId in cell.Neighbors)
            {
                if (!processed.Contains(neighborId))
                {
                    var neighbor = _map.Cells[neighborId];
                    queue.Enqueue(neighborId, Math.Max(neighbor.Height, cell.Height));
                }
            }
        }

        Console.WriteLine($"Filled {lakesFilled} pits/lakes");
        _lakesFilled = lakesFilled;
    }

    /// <summary>
    /// Calculate steepest descent direction for each cell
    /// </summary>
    private void CalculateFlowDirections()
    {
        foreach (var cell in _map.Cells)
        {
            if (cell.IsOcean)
            {
                // Ocean cells don't flow anywhere
                _flowDirection[cell.Id] = -1;
                continue;
            }

            int steepestNeighbor = -1;
            int maxDrop = 0;
            int minNeighborId = -1;
            int minNeighborHeight = int.MaxValue;

            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];
                int drop = cell.Height - neighbor.Height;

                if (drop > maxDrop)
                {
                    maxDrop = drop;
                    steepestNeighbor = neighborId;
                }

                if (neighbor.Height < minNeighborHeight)
                {
                    minNeighborHeight = neighbor.Height;
                    minNeighborId = neighborId;
                }
            }

            if (steepestNeighbor >= 0)
            {
                _flowDirection[cell.Id] = steepestNeighbor;
            }
            else
            {
                // Flat or uphill: choose a neighbor that is most likely to lead downhill
                int fallback = -1;
                foreach (var neighborId in cell.Neighbors)
                {
                    var n = _map.Cells[neighborId];
                    if (n.Height == cell.Height)
                    {
                        // Prefer flats that are adjacent to any lower neighbor
                        bool leadsDown = false;
                        foreach (var nnId in n.Neighbors)
                        {
                            if (_map.Cells[nnId].Height < n.Height) { leadsDown = true; break; }
                        }
                        if (leadsDown) { fallback = neighborId; break; }
                    }
                }

                if (fallback < 0)
                {
                    // Otherwise pick the overall lowest neighbor
                    fallback = minNeighborId;
                }

                _flowDirection[cell.Id] = fallback; // may remain -1 on isolated plateau
            }
        }
    }

    /// <summary>
    /// Calculate flow accumulation (upstream drainage area)
    /// </summary>
    private void CalculateFlowAccumulation()
    {
        // Initialize base flux from precipitation for land cells, 0 for ocean
        foreach (var cell in _map.Cells)
        {
            if (cell.IsLand)
            {
                int baseFlux = (int)Math.Max(1, Math.Round(Math.Max(0.0, cell.Precipitation) * _precipScale));
                _flowAccumulation[cell.Id] = baseFlux;
            }
            else
            {
                _flowAccumulation[cell.Id] = 0;
            }
        }

        // Process in topological order (highest to lowest)
        var sortedCells = TopologicalSort();

        foreach (var cellId in sortedCells)
        {
            var cell = _map.Cells[cellId];

            // Skip ocean cells
            if (cell.IsOcean)
                continue;

            // Get downhill neighbor
            if (_flowDirection.TryGetValue(cellId, out var downhillId) && downhillId >= 0)
            {
                // Add this cell's accumulation to downhill neighbor
                _flowAccumulation[downhillId] += _flowAccumulation[cellId];
            }
        }
    }

    /// <summary>
    /// Sort cells in topological order (highest to lowest elevation)
    /// Ensures we process upstream cells before downstream
    /// </summary>
    private List<int> TopologicalSort()
    {
        var sorted = _map.Cells
            .Where(c => c.IsLand)
            .OrderByDescending(c => c.Height)
            .ThenBy(c => c.Id) // Stable sort
            .Select(c => c.Id)
            .ToList();

        return sorted;
    }

    /// <summary>
    /// Generate rivers from high flow accumulation
    /// Uses threshold matching the original Fantasy Map Generator
    /// </summary>
    private void GenerateRivers()
    {
        _map.Rivers = new List<River>();

        // Original FMG uses MIN_FLUX_TO_FORM_RIVER = 30
        // with flux modified by: (cellCount / 10000) ** 0.25
        // This gives us the effective threshold in cell-accumulation units
        double cellsNumberModifier = Math.Pow(_map.Cells.Count / 10000.0, 0.25);

        // The original accumulates precipitation (typically 20-60 range) divided by modifier
        // So threshold ~30 in flux units translates to roughly:
        // 30 * modifier / avgPrecipitation ≈ cells needed to accumulate enough water
        // For simplicity, we use a scaled threshold based on cell count
        int thresholdBase = _minFlux;
        int threshold = (int)(thresholdBase * cellsNumberModifier);

        // Lower bound for very small maps
        threshold = Math.Max(threshold, 10);

        Console.WriteLine($"River formation threshold: {threshold} (cells: {_map.Cells.Count}, modifier: {cellsNumberModifier:F2})");
        _riverThreshold = threshold;
        int attempts = 0;
        while (true)
        {
            var visited = new HashSet<int>();
            _map.Rivers.Clear();
            foreach (var c in _map.Cells) c.HasRiver = false;

            // Find river sources (high accumulation cells)
            var riverSources = _flowAccumulation
                .Where(kvp => kvp.Value >= threshold)
                .Where(kvp => _map.Cells[kvp.Key].IsLand)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Take(500);
            _sourcesCount = riverSources.Count();

            _rejectedShort = 0;
            foreach (var sourceId in riverSources)
            {
                if (visited.Contains(sourceId)) continue;
                var river = TraceRiver(sourceId, visited);
                if (river != null && river.Cells.Count >= _minRiverLength)
                {
                    foreach (var cellId in river.Cells) _map.Cells[cellId].HasRiver = true;
                    _map.Rivers.Add(river);
                }
                else
                {
                    _rejectedShort++;
                }
            }

            if (!_autoAdjust) break;
            if (_map.Rivers.Count >= _targetRivers) break;
            if (threshold <= _minThreshold) break;
            threshold = Math.Max(_minThreshold, (int)Math.Floor(threshold * 0.8));
            attempts++;
            if (attempts > 5) break;
        }
        _riverThreshold = threshold;
    }

    /// <summary>
    /// Trace a river from source to mouth
    /// </summary>
    private River? TraceRiver(int sourceId, HashSet<int> visited)
    {
        var river = new River
        {
            Id = _map.Rivers.Count,
            Cells = new List<int>(),
            Source = sourceId
        };

        int current = sourceId;
        int maxLength = 1000; // Prevent infinite loops
        int length = 0;

        while (current >= 0 && length < maxLength)
        {
            // Check if already visited
            if (visited.Contains(current))
            {
                // Merge into existing river
                return null;
            }

            var cell = _map.Cells[current];

            // Stop at ocean
            if (cell.IsOcean)
            {
                river.Mouth = current;
                break;
            }

            // Add to river
            river.Cells.Add(current);
            visited.Add(current);
            // Don't mark HasRiver yet - wait until river is accepted

            // Move to downhill neighbor
            if (_flowDirection.TryGetValue(current, out var downhill))
            {
                current = downhill;
            }
            else
            {
                break;
            }

            length++;
        }

        // Set mouth (last cell before ocean)
        if (river.Cells.Count > 0 && river.Mouth == 0)
        {
            river.Mouth = river.Cells[^1];
        }

        return river;
    }

    /// <summary>
    /// Identify lake cells (filled depressions)
    /// </summary>
    private void IdentifyLakes()
    {
        // Lakes are flat regions surrounded by higher terrain
        var lakeCandidates = new Dictionary<int, List<int>>(); // elevation → cells

        foreach (var cell in _map.Cells)
        {
            if (cell.IsOcean || cell.HasRiver)
                continue;

            // Check if surrounded by higher or equal terrain
            bool isPotentialLake = cell.Neighbors.All(nId =>
                _map.Cells[nId].Height >= cell.Height);

            if (isPotentialLake)
            {
                if (!lakeCandidates.ContainsKey(cell.Height))
                {
                    lakeCandidates[cell.Height] = new List<int>();
                }

                lakeCandidates[cell.Height].Add(cell.Id);
            }
        }

        // Group connected lake cells at same elevation
        var lakes = new List<List<int>>();

        foreach (var (elevation, cells) in lakeCandidates)
        {
            var remaining = new HashSet<int>(cells);

            while (remaining.Count > 0)
            {
                var seed = remaining.First();
                var lake = FloodFillLake(seed, elevation, remaining);

                if (lake.Count >= 3) // Minimum lake size
                {
                    lakes.Add(lake);

                    // Mark cells as lake
                    foreach (var cellId in lake)
                    {
                        _map.Cells[cellId].Feature = -1; // Special value for lake
                    }
                }
            }
        }

        Console.WriteLine($"Identified {lakes.Count} lakes");
    }

    /// <summary>
    /// Flood fill to find connected lake cells at same elevation
    /// </summary>
    private List<int> FloodFillLake(int seedId, int elevation, HashSet<int> remaining)
    {
        var lake = new List<int>();
        var queue = new Queue<int>();

        queue.Enqueue(seedId);
        remaining.Remove(seedId);

        while (queue.Count > 0)
        {
            var cellId = queue.Dequeue();
            lake.Add(cellId);

            var cell = _map.Cells[cellId];

            foreach (var neighborId in cell.Neighbors)
            {
                if (remaining.Contains(neighborId))
                {
                    var neighbor = _map.Cells[neighborId];

                    if (neighbor.Height == elevation)
                    {
                        queue.Enqueue(neighborId);
                        remaining.Remove(neighborId);
                    }
                }
            }
        }

        return lake;
    }

    /// <summary>
    /// Calculate river width based on flow accumulation
    /// </summary>
    private void CalculateRiverWidths()
    {
        foreach (var river in _map.Rivers)
        {
            // Get max accumulation along river
            int maxAccumulation = river.Cells
                .Select(id => _flowAccumulation.GetValueOrDefault(id, 1))
                .Max();

            // Logarithmic scaling for width
            // Small streams: 1-2 units
            // Large rivers: 10-20 units
            double width = Math.Log10(maxAccumulation + 1) * 5;
            river.Width = (int)Math.Clamp(width, 1, 20);

            // Calculate length (approximate)
            river.Length = river.Cells.Count;

            // Determine river type based on width
            river.Type = river.Width switch
            {
                <= 2 => RiverType.Stream,
                <= 8 => RiverType.River,
                _ => RiverType.MajorRiver
            };
        }
    }

    /// <summary>
    /// Create river deltas at mouths (split into multiple channels)
    /// </summary>
    private void GenerateDeltas()
    {
        foreach (var river in _map.Rivers)
        {
            var mouthCell = _map.Cells[river.Mouth];

            // Only large rivers form deltas
            int mouthAccumulation = _flowAccumulation.GetValueOrDefault(river.Mouth, 0);
            if (mouthAccumulation < 500)
                continue;

            // Find coastal cells near mouth
            var coastalCells = FindCoastalCells(mouthCell, radius: 5);

            // Create 2-4 distributary channels
            int channelCount = Math.Min(coastalCells.Count, 2 + mouthAccumulation / 1000);

            for (int i = 0; i < channelCount; i++)
            {
                if (i < coastalCells.Count)
                {
                    var channelEnd = coastalCells[i];
                    CreateDeltaChannel(mouthCell, channelEnd);
                }
            }
        }
    }

    /// <summary>
    /// Find coastal cells near a center point
    /// </summary>
    private List<Cell> FindCoastalCells(Cell center, int radius)
    {
        var coastal = new List<Cell>();
        var visited = new HashSet<int>();
        var queue = new Queue<int>();

        queue.Enqueue(center.Id);
        visited.Add(center.Id);

        for (int depth = 0; depth < radius && queue.Count > 0; depth++)
        {
            int count = queue.Count;

            for (int i = 0; i < count; i++)
            {
                var cellId = queue.Dequeue();
                var cell = _map.Cells[cellId];

                // Check if coastal (land with ocean neighbor)
                if (cell.Height > 0 && cell.Neighbors.Any(n => _map.Cells[n].Height == 0))
                {
                    coastal.Add(cell);
                }

                // Expand search
                foreach (var neighborId in cell.Neighbors)
                {
                    if (!visited.Contains(neighborId))
                    {
                        queue.Enqueue(neighborId);
                        visited.Add(neighborId);
                    }
                }
            }
        }

        return coastal;
    }

    /// <summary>
    /// Create a delta channel between two points
    /// </summary>
    private void CreateDeltaChannel(Cell start, Cell end)
    {
        // Simple implementation: mark cells along a straight path
        // In a more sophisticated implementation, this would trace a realistic path
        var current = start;
        var target = end;
        var visited = new HashSet<int>();

        while (current.Id != target.Id && visited.Count < 20)
        {
            visited.Add(current.Id);
            current.HasRiver = true;

            // Find neighbor closest to target
            Cell? nextNeighbor = null;
            double minDistance = double.MaxValue;

            foreach (var neighborId in current.Neighbors)
            {
                if (visited.Contains(neighborId))
                    continue;

                var neighbor = _map.Cells[neighborId];
                var distance = Math.Sqrt(
                    Math.Pow(neighbor.Center.X - target.Center.X, 2) +
                    Math.Pow(neighbor.Center.Y - target.Center.Y, 2));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextNeighbor = neighbor;
                }
            }

            if (nextNeighbor == null)
                break;

            current = nextNeighbor;
        }

        // Mark the end cell
        if (target.Height > 0)
        {
            target.HasRiver = true;
        }
    }

    /// <summary>
    /// Mark rivers as seasonal based on climate
    /// </summary>
    private void IdentifySeasonalRivers()
    {
        foreach (var river in _map.Rivers)
        {
            // Calculate average precipitation along river
            double avgPrecipitation = river.Cells
                .Select(id => _map.Cells[id].Precipitation)
                .Average();

            // Low precipitation = seasonal river
            river.IsSeasonal = avgPrecipitation < 30;

            // Adjust width for seasonal rivers
            if (river.IsSeasonal)
            {
                river.Width = (int)(river.Width * 0.5);
            }
        }
    }

    /// <summary>
    /// Generate names for major rivers
    /// </summary>
    private void GenerateRiverNames(IRandomSource random)
    {
        // Name prefixes/suffixes
        var prefixes = new[] { "River", "Great", "Little", "North", "South", "East", "West" };
        var names = new[] { "Alder", "Birch", "Cedar", "Dale", "Elm", "Fern", "Glen", "Hazel" };
        var suffixes = new[] { "water", "stream", "flow", "rush", "brook" };

        // Sort rivers by length (name longest first)
        var sortedRivers = _map.Rivers
            .OrderByDescending(r => r.Length)
            .ThenByDescending(r => r.Width)
            .ToList();

        for (int i = 0; i < Math.Min(sortedRivers.Count, 20); i++)
        {
            var river = sortedRivers[i];

            if (river.Width >= 5)
            {
                // Major river: "Great River" or "Riverdale"
                river.Name = $"{prefixes[random.Next(prefixes.Length)]} {names[random.Next(names.Length)]}";
            }
            else
            {
                // Minor river: "Aldwater" or "Fernbrook"
                river.Name = $"{names[random.Next(names.Length)]}{suffixes[random.Next(suffixes.Length)]}";
            }
        }
    }
}

