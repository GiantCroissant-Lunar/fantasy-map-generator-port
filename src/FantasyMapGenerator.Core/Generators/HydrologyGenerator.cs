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

    public HydrologyGenerator(MapData map, IRandomSource random)
    {
        _map = map;
        _random = random;
        _flowDirection = new Dictionary<int, int>();
        _flowAccumulation = new Dictionary<int, int>();
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

        Console.WriteLine($"Generated {_map.Rivers.Count} rivers");
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
            if (cell.Height == 0 || cell.IsBorder)
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
    }

    /// <summary>
    /// Calculate steepest descent direction for each cell
    /// </summary>
    private void CalculateFlowDirections()
    {
        foreach (var cell in _map.Cells)
        {
            if (cell.Height == 0)
            {
                // Ocean cells don't flow anywhere
                _flowDirection[cell.Id] = -1;
                continue;
            }

            int steepestNeighbor = -1;
            int maxDrop = 0;

            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];
                int drop = cell.Height - neighbor.Height;

                if (drop > maxDrop)
                {
                    maxDrop = drop;
                    steepestNeighbor = neighborId;
                }
            }

            _flowDirection[cell.Id] = steepestNeighbor;
        }
    }

    /// <summary>
    /// Calculate flow accumulation (upstream drainage area)
    /// </summary>
    private void CalculateFlowAccumulation()
    {
        // Initialize all cells with 1 (self)
        foreach (var cell in _map.Cells)
        {
            _flowAccumulation[cell.Id] = 1;
        }

        // Process in topological order (highest to lowest)
        var sortedCells = TopologicalSort();

        foreach (var cellId in sortedCells)
        {
            var cell = _map.Cells[cellId];

            // Skip ocean cells
            if (cell.Height == 0)
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
            .Where(c => c.Height > 0)
            .OrderByDescending(c => c.Height)
            .ThenBy(c => c.Id) // Stable sort
            .Select(c => c.Id)
            .ToList();

        return sorted;
    }

    /// <summary>
    /// Generate rivers from high flow accumulation
    /// </summary>
    private void GenerateRivers()
    {
        _map.Rivers = new List<River>();

        // Threshold: minimum accumulation to form a river
        int threshold = (int)(_map.Cells.Count * 0.005); // ~0.5% of cells
        threshold = Math.Max(threshold, 5); // Lower threshold for test maps

        var visited = new HashSet<int>();

        // Find river sources (high accumulation cells)
        var riverSources = _flowAccumulation
            .Where(kvp => kvp.Value >= threshold)
            .Where(kvp => _map.Cells[kvp.Key].Height > 0) // Not ocean
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .Take(100); // Limit number of rivers

        foreach (var sourceId in riverSources)
        {
            if (visited.Contains(sourceId))
                continue;

            var river = TraceRiver(sourceId, visited);

            if (river != null && river.Cells.Count >= 3) // Minimum length
            {
                // Mark cells as having river only after accepting the river
                foreach (var cellId in river.Cells)
                {
                    _map.Cells[cellId].HasRiver = true;
                }
                _map.Rivers.Add(river);
            }
        }
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
            if (cell.Height == 0)
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
            if (cell.Height == 0 || cell.HasRiver)
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
}