using FantasyMapGenerator.Core.Models;
using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Smooth terrain renderer using contour tracing and interpolation
/// for artistic, flowing terrain visualization (suitable for GUI applications)
/// </summary>
public class SmoothTerrainRenderer : IDisposable
{
    private readonly MapRenderSettings _settings;
    private readonly Dictionary<string, SKPaint> _paintCache;

    // Heightmap interpolation grid
    private double[,]? _interpolatedHeights;
    private int _gridWidth;
    private int _gridHeight;
    private double _cellSize;

    public SmoothTerrainRenderer(MapRenderSettings? settings = null)
    {
        _settings = settings ?? new MapRenderSettings();
        _paintCache = new Dictionary<string, SKPaint>();
        InitializePaints();
    }

    /// <summary>
    /// Renders smooth, interpolated terrain to a surface
    /// </summary>
    public SKSurface RenderSmoothTerrain(MapData mapData, int width, int height)
    {
        var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Clear background
        canvas.Clear(_settings.BackgroundColor);

        // Build interpolated heightmap
        BuildInterpolatedHeightmap(mapData, width, height);

        // Render smooth terrain layers from low to high
        RenderTerrainLayer(canvas, 0, 20, _settings.WaterColor, "Deep Ocean");       // Deep water
        RenderTerrainLayer(canvas, 20, 25, _settings.BeachColor, "Shallow/Beach");   // Shallow/Beach
        RenderTerrainLayer(canvas, 25, 50, _settings.PlainsColor, "Plains");        // Plains
        RenderTerrainLayer(canvas, 50, 70, _settings.HillsColor, "Hills");          // Hills
        RenderTerrainLayer(canvas, 70, 100, _settings.MountainColor, "Mountains");  // Mountains

        return surface;
    }

    /// <summary>
    /// Builds an interpolated heightmap grid from cell data
    /// Uses inverse distance weighting (IDW) for smooth interpolation
    /// </summary>
    private void BuildInterpolatedHeightmap(MapData mapData, int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
        _interpolatedHeights = new double[height, width];

        // Calculate bounds
        var bounds = CalculateMapBounds(mapData);
        var scaleX = width / (bounds.maxX - bounds.minX);
        var scaleY = height / (bounds.maxY - bounds.minY);
        var scale = Math.Min(scaleX, scaleY) * 0.9;

        var offsetX = (width - (bounds.maxX - bounds.minX) * scale) / 2;
        var offsetY = (height - (bounds.maxY - bounds.minY) * scale) / 2;

        // Store cell size for later use
        _cellSize = scale;

        // For each pixel in the grid, interpolate height from nearby cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Convert pixel to world coordinates
                var worldX = (x - offsetX) / scale + bounds.minX;
                var worldY = (y - offsetY) / scale + bounds.minY;

                // Find nearest cells and interpolate
                _interpolatedHeights[y, x] = InterpolateHeight(mapData, worldX, worldY);
            }
        }
    }

    /// <summary>
    /// Interpolates height at a world position using inverse distance weighting
    /// </summary>
    private double InterpolateHeight(MapData mapData, double worldX, double worldY)
    {
        const int k = 4; // Number of nearest neighbors to consider
        const double power = 2.0; // IDW power parameter

        // Find k nearest cells
        var nearest = mapData.Cells
            .Select(cell => new
            {
                Cell = cell,
                Distance = Math.Sqrt(
                    Math.Pow(cell.Center.X - worldX, 2) +
                    Math.Pow(cell.Center.Y - worldY, 2))
            })
            .OrderBy(x => x.Distance)
            .Take(k)
            .ToList();

        // If we're very close to a cell center, use its exact height
        if (nearest[0].Distance < 0.1)
        {
            return nearest[0].Cell.Height;
        }

        // Inverse distance weighting
        double weightSum = 0;
        double heightSum = 0;

        foreach (var item in nearest)
        {
            var weight = 1.0 / Math.Pow(item.Distance, power);
            weightSum += weight;
            heightSum += weight * item.Cell.Height;
        }

        return heightSum / weightSum;
    }

    /// <summary>
    /// Renders a terrain layer using marching squares for smooth contours
    /// </summary>
    private void RenderTerrainLayer(SKCanvas canvas, double minHeight, double maxHeight, SKColor color, string layerName)
    {
        if (_interpolatedHeights == null) return;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color,
            IsAntialias = true
        };

        // Create contour path using marching squares
        using var path = new SKPath();

        bool[,] filled = new bool[_gridHeight, _gridWidth];

        // Mark all cells within height range
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                filled[y, x] = _interpolatedHeights[y, x] >= minHeight &&
                               _interpolatedHeights[y, x] < maxHeight;
            }
        }

        // Trace contours using marching squares
        TraceContours(filled, path, minHeight, maxHeight);

        // Fill the contoured region
        canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// Traces smooth contours using marching squares algorithm
    /// </summary>
    private void TraceContours(bool[,] filled, SKPath path, double minHeight, double maxHeight)
    {
        if (_interpolatedHeights == null) return;

        bool[,] visited = new bool[_gridHeight, _gridWidth];

        // Find all contour starting points
        for (int y = 0; y < _gridHeight - 1; y++)
        {
            for (int x = 0; x < _gridWidth - 1; x++)
            {
                if (filled[y, x] && !visited[y, x])
                {
                    // Start a new contour
                    FloodFillRegion(filled, visited, path, x, y);
                }
            }
        }
    }

    /// <summary>
    /// Flood fills a region and adds it to the path
    /// </summary>
    private void FloodFillRegion(bool[,] filled, bool[,] visited, SKPath path, int startX, int startY)
    {
        // Simple region filling approach
        // For each connected region, create a smooth boundary

        var stack = new Stack<(int x, int y)>();
        var regionPoints = new List<SKPoint>();

        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight) continue;
            if (visited[y, x] || !filled[y, x]) continue;

            visited[y, x] = true;
            regionPoints.Add(new SKPoint(x, y));

            // Add neighbors
            if (x > 0) stack.Push((x - 1, y));
            if (x < _gridWidth - 1) stack.Push((x + 1, y));
            if (y > 0) stack.Push((x, y - 1));
            if (y < _gridHeight - 1) stack.Push((x, y + 1));
        }

        if (regionPoints.Count > 2)
        {
            // Find boundary points
            var boundary = FindBoundaryPoints(regionPoints, filled);

            if (boundary.Count > 2)
            {
                // Create smooth path through boundary
                AddSmoothBoundary(path, boundary);
            }
        }
    }

    /// <summary>
    /// Finds boundary points of a region
    /// </summary>
    private List<SKPoint> FindBoundaryPoints(List<SKPoint> regionPoints, bool[,] filled)
    {
        var boundary = new List<SKPoint>();
        var pointSet = new HashSet<(int x, int y)>(regionPoints.Select(p => ((int)p.X, (int)p.Y)));

        foreach (var point in regionPoints)
        {
            int x = (int)point.X;
            int y = (int)point.Y;

            // Check if this point is on the boundary (has a neighbor outside the region)
            bool isBoundary = false;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= _gridWidth || ny < 0 || ny >= _gridHeight ||
                        !filled[ny, nx])
                    {
                        isBoundary = true;
                        break;
                    }
                }
                if (isBoundary) break;
            }

            if (isBoundary)
            {
                boundary.Add(point);
            }
        }

        return boundary;
    }

    /// <summary>
    /// Adds a smooth boundary to the path using spline interpolation
    /// </summary>
    private void AddSmoothBoundary(SKPath path, List<SKPoint> boundary)
    {
        if (boundary.Count < 3) return;

        // Sort boundary points by angle from centroid for proper ordering
        var centroid = new SKPoint(
            boundary.Average(p => p.X),
            boundary.Average(p => p.Y)
        );

        var sorted = boundary.OrderBy(p => Math.Atan2(p.Y - centroid.Y, p.X - centroid.X)).ToList();

        if (sorted.Count < 3) return;

        // Use the advanced curve smoothing utilities
        using var smoothPath = CurveSmoothing.CreateSmoothContour(sorted, closed: true);
        path.AddPath(smoothPath);
    }

    /// <summary>
    /// Calculates the bounding box of all cell centers
    /// </summary>
    private static (double minX, double minY, double maxX, double maxY) CalculateMapBounds(MapData mapData)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var cell in mapData.Cells)
        {
            minX = Math.Min(minX, cell.Center.X);
            minY = Math.Min(minY, cell.Center.Y);
            maxX = Math.Max(maxX, cell.Center.X);
            maxY = Math.Max(maxY, cell.Center.Y);
        }

        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Initializes paint cache with default styles
    /// </summary>
    private void InitializePaints()
    {
        _paintCache["default"] = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Gray,
            IsAntialias = true
        };
    }

    /// <summary>
    /// Disposes all resources
    /// </summary>
    public void Dispose()
    {
        foreach (var paint in _paintCache.Values)
        {
            paint.Dispose();
        }
        _paintCache.Clear();
    }
}
