using FantasyMapGenerator.Core.Models;
using SkiaSharp;
using System.Collections.Concurrent;

namespace FantasyMapGenerator.Rendering.Tiles;

/// <summary>
/// Simple tile provider for fantasy maps
/// Framework-agnostic - can be used with any UI (Avalonia, WPF, Web, TUI, etc.)
/// Not tied to BruTile or any specific mapping framework
/// </summary>
public class FantasyMapTileSource : IDisposable
{
    private readonly FantasyMapTileProvider _provider;
    private readonly TileSchema _schema;

    public FantasyMapTileSource(
        MapData mapData,
        TerrainColorScheme? colorScheme = null,
        float blurSigma = 2.0f,
        int tileSize = 256,
        int minZoomLevel = 0,
        int maxZoomLevel = 5)
    {
        _provider = new FantasyMapTileProvider(mapData, colorScheme, blurSigma, tileSize);

        // Create schema for the tile source
        _schema = new TileSchema(mapData.Width, mapData.Height, tileSize, minZoomLevel, maxZoomLevel);
    }

    public TileSchema Schema => _schema;

    /// <summary>
    /// Gets a tile as PNG bytes
    /// </summary>
    public byte[]? GetTile(int zoom, int col, int row)
    {
        return _provider.GetTile(zoom, col, row);
    }

    /// <summary>
    /// Gets a tile as SKImage
    /// </summary>
    public SKImage? GetTileImage(int zoom, int col, int row)
    {
        var bytes = GetTile(zoom, col, row);
        if (bytes == null) return null;

        using var data = SKData.CreateCopy(bytes);
        return SKImage.FromEncodedData(data);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}

/// <summary>
/// Tile provider that generates tiles on-demand from the fantasy map
/// Uses RasterBlurRenderer under the hood
/// </summary>
internal class FantasyMapTileProvider : IDisposable
{
    private readonly MapData _mapData;
    private readonly TerrainColorScheme _colorScheme;
    private readonly float _blurSigma;
    private readonly int _tileSize;
    private readonly ConcurrentDictionary<string, byte[]> _cache;
    private readonly RasterBlurRenderer _renderer;

    public FantasyMapTileProvider(
        MapData mapData,
        TerrainColorScheme? colorScheme = null,
        float blurSigma = 2.0f,
        int tileSize = 256)
    {
        _mapData = mapData;
        _colorScheme = colorScheme ?? TerrainColorSchemes.Classic;
        _blurSigma = blurSigma;
        _tileSize = tileSize;
        _cache = new ConcurrentDictionary<string, byte[]>();
        _renderer = new RasterBlurRenderer(_colorScheme, _blurSigma, antiAlias: true);
    }

    public byte[]? GetTile(int zoom, int col, int row)
    {
        try
        {
            var key = $"{zoom}_{col}_{row}";

            // Check cache first
            if (_cache.TryGetValue(key, out var cachedTile))
            {
                return cachedTile;
            }

            // Generate tile
            var tile = GenerateTile(zoom, col, row);

            // Cache it
            if (tile != null)
            {
                _cache[key] = tile;
            }

            return tile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TileProvider] Error generating tile {zoom}/{col}/{row}: {ex.Message}");
            return null;
        }
    }

    private byte[]? GenerateTile(int zoom, int col, int row)
    {
        // At zoom level 0, the entire map fits in one tile
        // At zoom level 1, the map is split into 2x2 tiles
        // At zoom level 2, the map is split into 4x4 tiles, etc.
        int tilesPerSide = 1 << zoom;  // 2^zoom

        // Calculate the portion of the map to render
        int mapTileWidth = _mapData.Width / tilesPerSide;
        int mapTileHeight = _mapData.Height / tilesPerSide;

        int startX = col * mapTileWidth;
        int startY = row * mapTileHeight;

        // Render this region of the map
        using var tileSurface = RenderMapRegion(startX, startY, mapTileWidth, mapTileHeight, _tileSize, _tileSize);

        // Encode as PNG
        using var image = tileSurface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private SKSurface RenderMapRegion(int startX, int startY, int width, int height, int outputWidth, int outputHeight)
    {
        // Create a temporary map data with only the cells in this region
        // For simplicity, we'll render the full map and crop
        // TODO: Optimize by only rendering cells in the region

        using var fullSurface = _renderer.RenderMap(_mapData, _mapData.Width, _mapData.Height);
        using var fullImage = fullSurface.Snapshot();

        // Crop to the tile region
        var sourceRect = new SKRectI(startX, startY, Math.Min(startX + width, _mapData.Width), Math.Min(startY + height, _mapData.Height));
        using var croppedImage = fullImage.Subset(sourceRect);

        // Scale to output size
        var imageInfo = new SKImageInfo(outputWidth, outputHeight);
        var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;

        var destRect = new SKRect(0, 0, outputWidth, outputHeight);
        canvas.DrawImage(croppedImage, destRect);

        return surface;
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _cache.Clear();
    }
}

/// <summary>
/// Simple tile schema describing the tile structure
/// </summary>
public class TileSchema
{
    public TileSchema(int mapWidth, int mapHeight, int tileSize = 256, int minZoom = 0, int maxZoom = 5)
    {
        MapWidth = mapWidth;
        MapHeight = mapHeight;
        TileSize = tileSize;
        MinZoom = minZoom;
        MaxZoom = maxZoom;
    }

    public int MapWidth { get; }
    public int MapHeight { get; }
    public int TileSize { get; }
    public int MinZoom { get; }
    public int MaxZoom { get; }

    /// <summary>
    /// Gets the number of tiles per side at a given zoom level
    /// </summary>
    public int GetTilesPerSide(int zoom)
    {
        return 1 << zoom;  // 2^zoom
    }

    /// <summary>
    /// Gets tile dimensions in map coordinates
    /// </summary>
    public (int width, int height) GetTileDimensions(int zoom)
    {
        int tilesPerSide = GetTilesPerSide(zoom);
        return (MapWidth / tilesPerSide, MapHeight / tilesPerSide);
    }
}

