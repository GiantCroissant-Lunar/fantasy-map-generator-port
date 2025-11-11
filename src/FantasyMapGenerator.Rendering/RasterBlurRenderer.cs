using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using SkiaSharp;
using System.Collections.Concurrent;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Renders fantasy maps using the proven raster-blur approach:
/// 1. Render Voronoi cells to raster (pixel-by-pixel)
/// 2. Apply Gaussian blur filter
/// 3. Result: smooth terrain transitions like the original JavaScript version
/// </summary>
public class RasterBlurRenderer : IDisposable
{
    private readonly TerrainColorScheme _colorScheme;
    private readonly float _blurSigma;
    private readonly bool _antiAlias;
    private CellSpatialIndex? _spatialIndex;

    public RasterBlurRenderer(
        TerrainColorScheme? colorScheme = null,
        float blurSigma = 2.0f,
        bool antiAlias = true)
    {
        _colorScheme = colorScheme ?? TerrainColorSchemes.Classic;
        _blurSigma = blurSigma;
        _antiAlias = antiAlias;
    }

    /// <summary>
    /// Renders the complete map using raster blur approach
    /// </summary>
    public SKSurface RenderMap(MapData mapData, int width, int height)
    {
        Console.WriteLine($"[RasterBlur] Starting render: {width}x{height}, blur={_blurSigma}");

        // Step 1: Build spatial index for fast cell lookup
        Console.WriteLine("[RasterBlur] Building spatial index...");
        _spatialIndex = new CellSpatialIndex(mapData);

        // Step 2: Render Voronoi cells to raster
        Console.WriteLine("[RasterBlur] Rasterizing Voronoi cells...");
        using var bitmap = RenderVoronoiToRaster(mapData, width, height);

        // Step 3: Apply Gaussian blur
        Console.WriteLine("[RasterBlur] Applying Gaussian blur...");
        var surface = ApplyBlur(bitmap, width, height);

        Console.WriteLine("[RasterBlur] Render complete!");
        return surface;
    }

    /// <summary>
    /// Renders Voronoi cells to a raster bitmap
    /// For each pixel, find the nearest cell and set its color
    /// </summary>
    private SKBitmap RenderVoronoiToRaster(MapData mapData, int width, int height)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);

        // Calculate world-to-pixel transform
        double scaleX = (double)width / mapData.Width;
        double scaleY = (double)height / mapData.Height;

        // Parallel rendering for performance
        var rowPartitioner = Partitioner.Create(0, height);

        Parallel.ForEach(rowPartitioner, (range) =>
        {
            for (int y = range.Item1; y < range.Item2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Convert pixel to world coordinates
                    double worldX = x / scaleX;
                    double worldY = y / scaleY;

                    // Find nearest cell
                    int cellIndex = _spatialIndex!.FindNearest(new Point(worldX, worldY));

                    if (cellIndex >= 0 && cellIndex < mapData.Cells.Count)
                    {
                        var cell = mapData.Cells[cellIndex];
                        var color = _colorScheme.GetColorForHeight(cell.Height);
                        bitmap.SetPixel(x, y, color);
                    }
                    else
                    {
                        // Fallback for edge cases
                        bitmap.SetPixel(x, y, SKColors.Black);
                    }
                }
            }
        });

        return bitmap;
    }

    /// <summary>
    /// Applies Gaussian blur to the bitmap using SkiaSharp's hardware-accelerated filters
    /// </summary>
    private SKSurface ApplyBlur(SKBitmap bitmap, int width, int height)
    {
        // Create blur filter (similar to Canvas blur in original JS)
        using var blurFilter = SKImageFilter.CreateBlur(_blurSigma, _blurSigma);

        var paint = new SKPaint
        {
            ImageFilter = blurFilter,
            IsAntialias = _antiAlias
        };

        // Create output surface
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;

        // Draw blurred bitmap
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(bitmap, 0, 0, paint);

        paint.Dispose();

        return surface;
    }

    /// <summary>
    /// Renders the map directly to a stream
    /// </summary>
    public void RenderToStream(MapData mapData, int width, int height, Stream stream, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
    {
        using var surface = RenderMap(mapData, width, height);
        using var image = surface.Snapshot();
        using var data = image.Encode(format, quality);
        data.SaveTo(stream);
    }

    /// <summary>
    /// Renders the map to a file
    /// </summary>
    public void RenderToFile(MapData mapData, int width, int height, string filePath)
    {
        using var stream = File.Create(filePath);
        var format = Path.GetExtension(filePath).ToLower() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
        RenderToStream(mapData, width, height, stream, format);
    }

    public void Dispose()
    {
        _spatialIndex?.Dispose();
    }
}

/// <summary>
/// Spatial index for fast nearest-cell lookup using a simple grid-based approach
/// For large maps, this could be upgraded to KD-Tree, but grid is fast enough for most cases
/// </summary>
public class CellSpatialIndex : IDisposable
{
    private readonly MapData _mapData;
    private readonly Dictionary<(int, int), List<int>> _grid;
    private readonly double _cellSize;
    private readonly int _gridWidth;
    private readonly int _gridHeight;

    public CellSpatialIndex(MapData mapData, int gridResolution = 50)
    {
        _mapData = mapData;

        // Calculate grid parameters
        _cellSize = Math.Max(mapData.Width, mapData.Height) / (double)gridResolution;
        _gridWidth = (int)Math.Ceiling(mapData.Width / _cellSize);
        _gridHeight = (int)Math.Ceiling(mapData.Height / _cellSize);

        // Build spatial grid
        _grid = new Dictionary<(int, int), List<int>>();

        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            var gridX = (int)(cell.Center.X / _cellSize);
            var gridY = (int)(cell.Center.Y / _cellSize);
            var key = (gridX, gridY);

            if (!_grid.ContainsKey(key))
            {
                _grid[key] = new List<int>();
            }
            _grid[key].Add(i);
        }

        Console.WriteLine($"[SpatialIndex] Built grid: {_gridWidth}x{_gridHeight} cells, {_grid.Count} buckets");
    }

    /// <summary>
    /// Finds the nearest cell to a given point
    /// Uses grid-based spatial partitioning for O(1) average case
    /// </summary>
    public int FindNearest(Point point)
    {
        var gridX = (int)(point.X / _cellSize);
        var gridY = (int)(point.Y / _cellSize);

        // Search in expanding radius from the target grid cell
        for (int radius = 0; radius <= 2; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    // Only check cells at the current radius (not interior)
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;

                    var checkX = gridX + dx;
                    var checkY = gridY + dy;

                    if (checkX < 0 || checkX >= _gridWidth || checkY < 0 || checkY >= _gridHeight)
                        continue;

                    var key = (checkX, checkY);
                    if (_grid.TryGetValue(key, out var cellIndices))
                    {
                        if (cellIndices.Count > 0)
                        {
                            // Find closest cell in this grid bucket
                            var closest = FindClosestInList(point, cellIndices);
                            if (closest >= 0)
                                return closest;
                        }
                    }
                }
            }
        }

        // Fallback: brute force search (should rarely happen)
        return FindClosestBruteForce(point);
    }

    /// <summary>
    /// Finds closest cell from a list of candidates
    /// </summary>
    private int FindClosestInList(Point point, List<int> candidates)
    {
        if (candidates.Count == 0)
            return -1;

        int closest = candidates[0];
        double minDist = DistanceSquared(point, _mapData.Cells[closest].Center);

        for (int i = 1; i < candidates.Count; i++)
        {
            var cellIndex = candidates[i];
            var dist = DistanceSquared(point, _mapData.Cells[cellIndex].Center);
            if (dist < minDist)
            {
                minDist = dist;
                closest = cellIndex;
            }
        }

        return closest;
    }

    /// <summary>
    /// Brute force fallback for edge cases
    /// </summary>
    private int FindClosestBruteForce(Point point)
    {
        if (_mapData.Cells.Count == 0)
            return -1;

        int closest = 0;
        double minDist = DistanceSquared(point, _mapData.Cells[0].Center);

        for (int i = 1; i < _mapData.Cells.Count; i++)
        {
            var dist = DistanceSquared(point, _mapData.Cells[i].Center);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }

    /// <summary>
    /// Calculates squared distance (faster than actual distance)
    /// </summary>
    private static double DistanceSquared(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return dx * dx + dy * dy;
    }

    public void Dispose()
    {
        _grid.Clear();
    }
}
