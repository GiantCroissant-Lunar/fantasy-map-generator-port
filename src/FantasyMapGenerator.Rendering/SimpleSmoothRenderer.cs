using FantasyMapGenerator.Core.Models;
using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Simple, robust smooth terrain renderer using direct interpolation
/// without complex contour tracing (which can create artifacts)
/// </summary>
public class SimpleSmoothRenderer : IDisposable
{
    private readonly SmoothTerrainRenderSettings _settings;
    private double[,]? _interpolatedHeights;
    private int _gridWidth;
    private int _gridHeight;

    public SimpleSmoothRenderer(SmoothTerrainRenderSettings? settings = null)
    {
        _settings = settings ?? new SmoothTerrainRenderSettings();
    }

    /// <summary>
    /// Renders a smooth terrain map using pixel-perfect gradient interpolation
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

        // Render using direct gradient coloring
        RenderGradientTerrain(canvas);

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
        var bounds = CalculateMapBounds(mapData);
        var scaleX = width / (bounds.maxX - bounds.minX);
        var scaleY = height / (bounds.maxY - bounds.minY);
        var scale = Math.Min(scaleX, scaleY) * 0.9;

        var offsetX = (width - (bounds.maxX - bounds.minX) * scale) / 2;
        var offsetY = (height - (bounds.maxY - bounds.minY) * scale) / 2;

        // Interpolate heights for each pixel
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var worldX = (x - offsetX) / scale + bounds.minX;
                var worldY = (y - offsetY) / scale + bounds.minY;

                _interpolatedHeights[y, x] = InterpolateHeight(mapData, worldX, worldY);
            }
        }
    }

    /// <summary>
    /// Interpolates height using inverse distance weighting with proper clamping
    /// </summary>
    private double InterpolateHeight(MapData mapData, double worldX, double worldY)
    {
        const int k = 3; // Use 3 nearest neighbors for smooth but not over-blurred results
        const double power = 2.0; // Standard IDW power

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

        if (nearest.Count == 0)
            return 0;

        // If very close to a cell center, use exact value
        if (nearest[0].Distance < 1.0)
            return nearest[0].Cell.Height;

        // Inverse distance weighting
        double weightSum = 0;
        double heightSum = 0;

        foreach (var item in nearest)
        {
            var weight = 1.0 / Math.Pow(item.Distance, power);
            weightSum += weight;
            heightSum += weight * item.Cell.Height;
        }

        var interpolated = heightSum / weightSum;

        // CRITICAL: Clamp to valid range to prevent color lookup failures
        return Math.Clamp(interpolated, 0, 100);
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
        // Nothing to dispose in this simple version
    }
}
