using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Main map renderer using SkiaSharp for drawing fantasy maps
/// </summary>
public class MapRenderer : IDisposable
{
    private readonly MapRenderSettings _settings;
    private readonly Dictionary<string, SKPaint> _paintCache;
    private Voronoi? _voronoi;
    
    public MapRenderer(MapRenderSettings? settings = null)
    {
        _settings = settings ?? new MapRenderSettings();
        _paintCache = new Dictionary<string, SKPaint>();
        InitializePaints();
    }
    
    /// <summary>
    /// Renders the complete map to an SKSurface
    /// </summary>
    public SKSurface RenderMap(MapData mapData, int width, int height)
    {
        var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        // Generate Voronoi diagram if needed
        if (_voronoi == null)
        {
            GenerateVoronoi(mapData);
        }
        
        // Clear background
        canvas.Clear(_settings.BackgroundColor);
        
        // Calculate scale to fit map in surface
        var bounds = CalculateMapBounds(mapData);
        var scale = Math.Min(width / bounds.Width, height / bounds.Height) * 0.9;
        var offset = new SKPoint((float)((width - bounds.Width * scale) / 2), (float)((height - bounds.Height * scale) / 2));
        
        // Apply transformation
        canvas.Translate(offset.X, offset.Y);
        canvas.Scale((float)scale, (float)scale);
        canvas.Translate(-bounds.Left, -bounds.Top);
        
        // Render layers in order
        RenderLayer(canvas, mapData, MapLayer.Terrain);
        RenderLayer(canvas, mapData, MapLayer.Coastline);
        RenderLayer(canvas, mapData, MapLayer.Rivers);
        RenderLayer(canvas, mapData, MapLayer.Borders);
        RenderLayer(canvas, mapData, MapLayer.Cities);
        RenderLayer(canvas, mapData, MapLayer.Labels);
        
        return surface;
    }
    
    /// <summary>
    /// Generates Voronoi diagram for the map
    /// </summary>
    private void GenerateVoronoi(MapData mapData)
    {
        var points = mapData.Points.ToArray();
        var delaunay = new Delaunator(points.Select(p => new[] { p.X, p.Y }).SelectMany(arr => arr).ToArray());
        _voronoi = new Voronoi(delaunay, points, points.Length);
    }
    
    /// <summary>
    /// Renders a specific layer of the map
    /// </summary>
    private void RenderLayer(SKCanvas canvas, MapData mapData, MapLayer layer)
    {
        switch (layer)
        {
            case MapLayer.Terrain:
                RenderTerrain(canvas, mapData);
                break;
            case MapLayer.Coastline:
                RenderCoastline(canvas, mapData);
                break;
            case MapLayer.Rivers:
                RenderRivers(canvas, mapData);
                break;
            case MapLayer.Borders:
                RenderBorders(canvas, mapData);
                break;
            case MapLayer.Cities:
                RenderCities(canvas, mapData);
                break;
            case MapLayer.Labels:
                RenderLabels(canvas, mapData);
                break;
        }
    }
    
    /// <summary>
    /// Renders terrain based on heightmap and biomes
    /// </summary>
    private void RenderTerrain(SKCanvas canvas, MapData mapData)
    {
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            
            var vertices = _voronoi?.GetCellVertices(i) ?? new List<Point>();
            if (vertices.Count < 3) continue;
            
            var path = CreatePolygonPath(vertices);
            var paint = GetTerrainPaint(cell.Height, GetBiomeById(mapData, cell.Biome));
            
            canvas.DrawPath(path, paint);
        }
    }
    
    /// <summary>
    /// Renders coastlines and shorelines
    /// </summary>
    private void RenderCoastline(SKCanvas canvas, MapData mapData)
    {
        var coastlinePaint = GetPaint("coastline");
        
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            
            // Check if this cell is on the coast (land adjacent to water)
            bool isCoast = cell.Height > 20 && HasWaterNeighbor(mapData, i);
            if (!isCoast) continue;
            
            var vertices = _voronoi?.GetCellVertices(i) ?? new List<Point>();
            if (vertices.Count < 3) continue;
            
            var path = CreatePolygonPath(vertices);
            canvas.DrawPath(path, coastlinePaint);
        }
    }
    
    /// <summary>
    /// Renders rivers on the map
    /// </summary>
    private void RenderRivers(SKCanvas canvas, MapData mapData)
    {
        foreach (var river in mapData.Rivers)
        {
            // Build path from cell centers
            using var path = new SKPath();

            for (int i = 0; i < river.Cells.Count; i++)
            {
                var cell = mapData.Cells[river.Cells[i]];

                if (i == 0)
                {
                    path.MoveTo((float)cell.Center.X, (float)cell.Center.Y);
                }
                else
                {
                    path.LineTo((float)cell.Center.X, (float)cell.Center.Y);
                }
            }

            // Draw river
            using var paint = new SKPaint
            {
                Color = river.IsSeasonal
                    ? SKColors.LightBlue.WithAlpha(150)
                    : SKColors.Blue,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = river.Width,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            canvas.DrawPath(path, paint);

            // Draw river name (if major river)
            if (!string.IsNullOrEmpty(river.Name) && river.Width >= 5)
            {
                DrawRiverName(canvas, river.Name, river, mapData);
            }
        }
    }

    /// <summary>
    /// Draws river name at the middle of the river
    /// </summary>
    private void DrawRiverName(SKCanvas canvas, string text, River river, MapData mapData)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic)
        };

        // Draw text at the middle of the river
        if (river.Cells.Count > 0)
        {
            var middleIndex = river.Cells.Count / 2;
            var middleCell = mapData.Cells[river.Cells[middleIndex]];
            var position = new SKPoint((float)middleCell.Center.X, (float)middleCell.Center.Y);
            
            canvas.DrawText(text, position.X, position.Y, paint);
        }
    }
    
    /// <summary>
    /// Renders political borders between states
    /// </summary>
    private void RenderBorders(SKCanvas canvas, MapData mapData)
    {
        var borderPaint = GetPaint("border");
        
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            if (cell.State < 0) continue;
            
            var neighbors = _voronoi?.GetCellNeighbors(i) ?? new List<int>();
            foreach (var neighborId in neighbors)
            {
                if (neighborId >= 0 && neighborId < mapData.Cells.Count)
                {
                    var neighbor = mapData.Cells[neighborId];
                    if (neighbor.State >= 0 && neighbor.State != cell.State)
                    {
                        // Draw border between these cells
                        DrawCellBorder(canvas, mapData, i, neighborId, borderPaint);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Renders cities and towns on the map
    /// </summary>
    private void RenderCities(SKCanvas canvas, MapData mapData)
    {
        foreach (var burg in mapData.Burgs)
        {
            var position = new SKPoint((float)burg.X, (float)burg.Y);
            var paint = GetCityPaint(burg);
            
            // Draw city circle
            var citySize = GetCitySize(burg);
            canvas.DrawCircle(position, citySize * 2, paint);
            
            // Draw city icon if capital
            if (burg.IsCapital)
            {
                var capitalPaint = GetPaint("capital");
                canvas.DrawCircle(position, citySize * 3, capitalPaint);
            }
        }
    }
    
    /// <summary>
    /// Renders labels for cities, states, and features
    /// </summary>
    private void RenderLabels(SKCanvas canvas, MapData mapData)
    {
        var labelPaint = GetPaint("label");
        
        // City labels
        foreach (var burg in mapData.Burgs)
        {
            var position = new SKPoint((float)burg.X, (float)burg.Y);
            var citySize = GetCitySize(burg);
            canvas.DrawText(burg.Name, position.X, position.Y - citySize * 4, labelPaint);
        }
        
        // State labels would go here when implemented
    }
    
    /// <summary>
    /// Creates a polygon path from a list of vertices
    /// </summary>
    private static SKPath CreatePolygonPath(List<Point> vertices)
    {
        var path = new SKPath();
        if (vertices.Count == 0) return path;
        
        path.MoveTo((float)vertices[0].X, (float)vertices[0].Y);
        for (int i = 1; i < vertices.Count; i++)
        {
            path.LineTo((float)vertices[i].X, (float)vertices[i].Y);
        }
        path.Close();
        
        return path;
    }
    
    /// <summary>
    /// Draws a border between two adjacent cells
    /// </summary>
    private void DrawCellBorder(SKCanvas canvas, MapData mapData, int cell1, int cell2, SKPaint paint)
    {
        var vertices1 = _voronoi?.GetCellVertices(cell1) ?? new List<Point>();
        var vertices2 = _voronoi?.GetCellVertices(cell2) ?? new List<Point>();
        
        // Find shared edge between cells
        var sharedEdge = FindSharedEdge(vertices1, vertices2);
        if (sharedEdge.Length >= 2)
        {
            var path = new SKPath();
            path.MoveTo((float)sharedEdge[0].X, (float)sharedEdge[0].Y);
            path.LineTo((float)sharedEdge[1].X, (float)sharedEdge[1].Y);
            canvas.DrawPath(path, paint);
        }
    }
    
    /// <summary>
    /// Finds the shared edge between two cells
    /// </summary>
    private static Point[] FindSharedEdge(List<Point> vertices1, List<Point> vertices2)
    {
        // Simple implementation - find two consecutive vertices that are shared
        for (int i = 0; i < vertices1.Count; i++)
        {
            var v1 = vertices1[i];
            var v2 = vertices1[(i + 1) % vertices1.Count];
            
            if (vertices2.Contains(v1) && vertices2.Contains(v2))
            {
                return new[] { v1, v2 };
            }
        }
        
        return Array.Empty<Point>();
    }
    
    /// <summary>
    /// Checks if a cell has water neighbors
    /// </summary>
    private bool HasWaterNeighbor(MapData mapData, int cellId)
    {
        var neighbors = _voronoi?.GetCellNeighbors(cellId) ?? new List<int>();
        foreach (var neighborId in neighbors)
        {
            if (neighborId >= 0 && neighborId < mapData.Cells.Count)
            {
                var neighbor = mapData.Cells[neighborId];
                if (neighbor.Height <= 20)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Calculates the bounding box of the map
    /// </summary>
    private static SKRect CalculateMapBounds(MapData mapData)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        
        foreach (var cell in mapData.Cells)
        {
            minX = Math.Min(minX, cell.Center.X);
            minY = Math.Min(minY, cell.Center.Y);
            maxX = Math.Max(maxX, cell.Center.X);
            maxY = Math.Max(maxY, cell.Center.Y);
        }
        
        return new SKRect((float)minX, (float)minY, (float)maxX, (float)maxY);
    }
    
    /// <summary>
    /// Gets terrain paint based on height and biome
    /// </summary>
    private SKPaint GetTerrainPaint(byte height, Biome? biome)
    {
        var key = $"terrain_{height}_{biome?.Id ?? -1}";
        if (_paintCache.TryGetValue(key, out var paint))
            return paint;
        
        paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetTerrainColor(height, biome)
        };
        
        _paintCache[key] = paint;
        return paint;
    }
    
    /// <summary>
    /// Gets city paint based on burg type
    /// </summary>
    private SKPaint GetCityPaint(Burg burg)
    {
        var key = $"city_{burg.Type}";
        if (_paintCache.TryGetValue(key, out var paint))
            return paint;
        
        paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = burg.Type == BurgType.City ? SKColors.Black : SKColors.DarkGray
        };
        
        _paintCache[key] = paint;
        return paint;
    }
    
    /// <summary>
    /// Gets city size based on burg type and population
    /// </summary>
    private static float GetCitySize(Burg burg)
    {
        return burg.Type switch
        {
            BurgType.Village => 2f,
            BurgType.Town => 3f,
            BurgType.City => 4f,
            BurgType.Capital => 5f,
            BurgType.Port => 4f,
            _ => 3f
        };
    }
    
    /// <summary>
    /// Gets biome by ID from map data
    /// </summary>
    private static Biome? GetBiomeById(MapData mapData, int biomeId)
    {
        return biomeId >= 0 && biomeId < mapData.Biomes.Count ? mapData.Biomes[biomeId] : null;
    }
    
    /// <summary>
    /// Gets a cached paint by key
    /// </summary>
    private SKPaint GetPaint(string key)
    {
        return _paintCache.TryGetValue(key, out var paint) ? paint : _paintCache["default"];
    }
    
    /// <summary>
    /// Gets terrain color based on height and biome
    /// </summary>
    private SKColor GetTerrainColor(byte height, Biome? biome)
    {
        if (height <= 20) return _settings.WaterColor;        // Water
        if (height <= 25) return _settings.BeachColor;        // Beach
        if (height <= 50) return _settings.PlainsColor;       // Plains
        if (height <= 70) return _settings.HillsColor;        // Hills
        return _settings.MountainColor;                        // Mountains
    }
    
    /// <summary>
    /// Initializes the paint cache with common paints
    /// </summary>
    private void InitializePaints()
    {
        // Default paint
        _paintCache["default"] = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Gray
        };
        
        // Coastline
        _paintCache["coastline"] = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkBlue,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        // Border
        _paintCache["border"] = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 1,
            IsAntialias = true
        };
        
        // River
        _paintCache["river"] = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Blue,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        // Capital
        _paintCache["capital"] = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        // Label
        _paintCache["label"] = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextSize = 12,
            Typeface = SKTypeface.Default
        };
    }
    
    /// <summary>
    /// Renders the map directly to a stream
    /// </summary>
    public void RenderToStream(MapData mapData, MapRenderSettings settings, Stream stream)
    {
        var surface = RenderMap(mapData, mapData.Width, mapData.Height);
        var image = surface.Snapshot();
        var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        surface.Dispose();
        image.Dispose();
        data.Dispose();
    }
    
    /// <summary>
    /// Disposes all paint resources
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