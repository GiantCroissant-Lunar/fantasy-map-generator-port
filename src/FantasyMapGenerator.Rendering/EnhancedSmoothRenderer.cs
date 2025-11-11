using FantasyMapGenerator.Core.Models;
using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Enhanced smooth terrain renderer with advanced color schemes and rendering features
/// Combines smooth interpolation, contour tracing, and artistic color palettes
/// </summary>
public class EnhancedSmoothRenderer : IDisposable
{
    private readonly SmoothTerrainRenderSettings _settings;
    private readonly Dictionary<string, SKPaint> _paintCache;

    // Heightmap interpolation grid
    private double[,]? _interpolatedHeights;
    private int _gridWidth;
    private int _gridHeight;
    private (double minX, double minY, double maxX, double maxY) _bounds;
    private double _scale;
    private double _offsetX;
    private double _offsetY;

    public EnhancedSmoothRenderer(SmoothTerrainRenderSettings? settings = null)
    {
        _settings = settings ?? new SmoothTerrainRenderSettings();
        _paintCache = new Dictionary<string, SKPaint>();
    }

    /// <summary>
    /// Renders a complete smooth terrain map with all features
    /// </summary>
    public SKSurface RenderMap(MapData mapData, int width, int height)
    {
        var surface = SKSurface.Create(new SKImageInfo(width, height,
            SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        // Clear background
        canvas.Clear(_settings.BackgroundColor);

        // Build interpolated heightmap
        BuildInterpolatedHeightmap(mapData, width, height);

        // Render terrain with smooth color scheme
        if (_settings.UseGradients)
        {
            RenderGradientTerrain(canvas);
        }
        else
        {
            RenderLayeredTerrain(canvas);
        }

        // Optional: Add contour lines for topographic effect
        // RenderContourLines(canvas);

        return surface;
    }

    /// <summary>
    /// Builds an interpolated heightmap grid using inverse distance weighting
    /// </summary>
    private void BuildInterpolatedHeightmap(MapData mapData, int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
        _interpolatedHeights = new double[height, width];

        // Calculate bounds
        _bounds = CalculateMapBounds(mapData);
        var scaleX = width / (_bounds.maxX - _bounds.minX);
        var scaleY = height / (_bounds.maxY - _bounds.minY);
        _scale = Math.Min(scaleX, scaleY) * 0.9;

        _offsetX = (width - (_bounds.maxX - _bounds.minX) * _scale) / 2;
        _offsetY = (height - (_bounds.maxY - _bounds.minY) * _scale) / 2;

        // Interpolate heights for each pixel
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var worldX = (x - _offsetX) / _scale + _bounds.minX;
                var worldY = (y - _offsetY) / _scale + _bounds.minY;

                _interpolatedHeights[y, x] = InterpolateHeight(mapData, worldX, worldY);
            }
        }
    }

    /// <summary>
    /// Interpolates height using inverse distance weighting
    /// </summary>
    private double InterpolateHeight(MapData mapData, double worldX, double worldY)
    {
        const int k = 6; // Number of nearest neighbors
        const double power = 2.5; // IDW power parameter for smoother blending

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

        if (nearest[0].Distance < 0.1)
            return nearest[0].Cell.Height;

        double weightSum = 0;
        double heightSum = 0;

        foreach (var item in nearest)
        {
            var weight = 1.0 / Math.Pow(item.Distance + 0.001, power);
            weightSum += weight;
            heightSum += weight * item.Cell.Height;
        }

        return heightSum / weightSum;
    }

    /// <summary>
    /// Renders terrain using smooth gradients (pixel-by-pixel coloring)
    /// </summary>
    private void RenderGradientTerrain(SKCanvas canvas)
    {
        if (_interpolatedHeights == null) return;

        // Create a bitmap for direct pixel manipulation
        var bitmap = new SKBitmap(_gridWidth, _gridHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

        unsafe
        {
            var pixels = (uint*)bitmap.GetPixels().ToPointer();

            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var height = _interpolatedHeights[y, x];
                    var color = _settings.ColorScheme.GetColorForHeight(height);
                    pixels[y * _gridWidth + x] = (uint)color;
                }
            }
        }

        canvas.DrawBitmap(bitmap, 0, 0);
        bitmap.Dispose();
    }

    /// <summary>
    /// Renders terrain in distinct layers with smooth boundaries
    /// </summary>
    private void RenderLayeredTerrain(SKCanvas canvas)
    {
        if (_interpolatedHeights == null) return;

        var layers = _settings.ColorScheme.Layers.OrderBy(l => l.heightThreshold).ToList();

        // Render each layer from bottom to top
        for (int i = 0; i < layers.Count - 1; i++)
        {
            var minHeight = layers[i].heightThreshold;
            var maxHeight = layers[i + 1].heightThreshold;
            var color = layers[i].color;

            RenderTerrainLayer(canvas, minHeight, maxHeight, color);
        }
    }

    /// <summary>
    /// Renders a single terrain layer with smooth contours
    /// </summary>
    private void RenderTerrainLayer(SKCanvas canvas, double minHeight, double maxHeight, SKColor color)
    {
        if (_interpolatedHeights == null) return;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = color,
            IsAntialias = _settings.AntiAlias
        };

        // Create mask of pixels in height range
        bool[,] mask = new bool[_gridHeight, _gridWidth];
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                var h = _interpolatedHeights[y, x];
                mask[y, x] = h >= minHeight && h < maxHeight;
            }
        }

        // Trace and render contours
        var path = TraceContours(mask);
        canvas.DrawPath(path, paint);
        path.Dispose();
    }

    /// <summary>
    /// Traces smooth contours from a boolean mask
    /// </summary>
    private SKPath TraceContours(bool[,] mask)
    {
        var path = new SKPath();
        bool[,] visited = new bool[_gridHeight, _gridWidth];

        // Find all regions and trace their boundaries
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                if (mask[y, x] && !visited[y, x])
                {
                    var region = FloodFill(mask, visited, x, y);
                    var boundary = FindBoundary(region, mask);

                    if (boundary.Count > 2)
                    {
                        // Sort boundary points
                        boundary = SortBoundaryPoints(boundary);

                        // Create smooth contour
                        using var regionPath = CurveSmoothing.CreateSmoothContour(boundary, closed: true);
                        path.AddPath(regionPath);
                    }
                }
            }
        }

        return path;
    }

    /// <summary>
    /// Flood fills a region and returns all points
    /// </summary>
    private List<SKPoint> FloodFill(bool[,] mask, bool[,] visited, int startX, int startY)
    {
        var points = new List<SKPoint>();
        var stack = new Stack<(int x, int y)>();
        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight) continue;
            if (visited[y, x] || !mask[y, x]) continue;

            visited[y, x] = true;
            points.Add(new SKPoint(x, y));

            // 4-connected neighbors
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }

        return points;
    }

    /// <summary>
    /// Finds boundary points of a region
    /// </summary>
    private List<SKPoint> FindBoundary(List<SKPoint> region, bool[,] mask)
    {
        var boundary = new HashSet<SKPoint>();

        foreach (var point in region)
        {
            int x = (int)point.X;
            int y = (int)point.Y;

            // Check 8-connected neighbors
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= _gridWidth || ny < 0 || ny >= _gridHeight || !mask[ny, nx])
                    {
                        boundary.Add(point);
                        break;
                    }
                }
            }
        }

        return boundary.ToList();
    }

    /// <summary>
    /// Sorts boundary points by angle from centroid
    /// </summary>
    private List<SKPoint> SortBoundaryPoints(List<SKPoint> boundary)
    {
        if (boundary.Count < 2) return boundary;

        var centroid = new SKPoint(
            boundary.Average(p => p.X),
            boundary.Average(p => p.Y)
        );

        return boundary
            .OrderBy(p => Math.Atan2(p.Y - centroid.Y, p.X - centroid.X))
            .ToList();
    }

    /// <summary>
    /// Calculates map bounds
    /// </summary>
    private static (double minX, double minY, double maxX, double maxY) CalculateMapBounds(MapData mapData)
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

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
    /// Disposes resources
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
