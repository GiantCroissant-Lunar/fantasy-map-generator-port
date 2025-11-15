using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Handles exporting maps to various file formats
/// </summary>
public class MapExporter : IDisposable
{
    private readonly MapRenderer _renderer;
    
    public MapExporter(MapRenderer? renderer = null)
    {
        _renderer = renderer ?? new MapRenderer();
    }
    
    /// <summary>
    /// Exports map to PNG file
    /// </summary>
    public async Task ExportToPngAsync(MapData mapData, string filePath, int width, int height, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var surface = _renderer.RenderMap(mapData, width, height);
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }, cancellationToken);
    }
    
    /// <summary>
    /// Exports map to JPEG file
    /// </summary>
    public async Task ExportToJpegAsync(MapData mapData, string filePath, int width, int height, int quality = 90, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var surface = _renderer.RenderMap(mapData, width, height);
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }, cancellationToken);
    }
    
    /// <summary>
    /// Exports map to SVG file
    /// </summary>
    public async Task ExportToSvgAsync(MapData mapData, string filePath, int width, int height, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var svgContent = GenerateSvgContent(mapData, width, height);
            File.WriteAllText(filePath, svgContent);
        }, cancellationToken);
    }
    
    /// <summary>
    /// Exports map to multiple formats
    /// </summary>
    public async Task ExportMultipleFormatsAsync(MapData mapData, string baseFilePath, int width, int height, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            ExportToPngAsync(mapData, $"{baseFilePath}.png", width, height, cancellationToken),
            ExportToJpegAsync(mapData, $"{baseFilePath}.jpg", width, height, 90, cancellationToken),
            ExportToSvgAsync(mapData, $"{baseFilePath}.svg", width, height, cancellationToken)
        };
        
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Generates SVG content for the map
    /// </summary>
    private string GenerateSvgContent(MapData mapData, int width, int height)
    {
        var svg = new System.Text.StringBuilder();
        
        // SVG header
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        svg.AppendLine($"<svg width=\"{width}\" height=\"{height}\" xmlns=\"http://www.w3.org/2000/svg\">");
        svg.AppendLine("<style>");
        svg.AppendLine(".water { fill: #40A4DF; }");
        svg.AppendLine(".beach { fill: #EECBAD; }");
        svg.AppendLine(".plains { fill: #86A85F; }");
        svg.AppendLine(".hills { fill: #9E9A87; }");
        svg.AppendLine(".mountain { fill: #8B8989; }");
        svg.AppendLine(".coastline { stroke: #003366; stroke-width: 2; fill: none; }");
        svg.AppendLine(".border { stroke: #000000; stroke-width: 1; fill: none; }");
        svg.AppendLine(".river { stroke: #0066CC; stroke-width: 2; fill: none; }");
        svg.AppendLine(".city { fill: #000000; }");
        svg.AppendLine(".capital { stroke: #FF0000; stroke-width: 2; fill: none; }");
        svg.AppendLine("</style>");
        
        // Calculate bounds and scale
        var bounds = CalculateMapBounds(mapData);
        var scale = Math.Min(width / bounds.Width, height / bounds.Height) * 0.9;
        var offsetX = (width - bounds.Width * scale) / 2 - bounds.Left * scale;
        var offsetY = (height - bounds.Height * scale) / 2 - bounds.Top * scale;
        
        // Create transformation group
        svg.AppendLine($"<g transform=\"translate({offsetX:F2}, {offsetY:F2}) scale({scale:F2})\">");
        
        // Generate Voronoi for SVG export
        var points = mapData.Points.ToArray();
        var delaunay = new Delaunator(points.Select(p => new[] { p.X, p.Y }).SelectMany(arr => arr).ToArray());
        var voronoi = new Voronoi(delaunay, points, points.Length);
        
        // Render terrain
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            
            var vertices = voronoi.GetCellVertices(i);
            if (vertices.Count < 3) continue;
            
            var className = GetTerrainClassName(cell.Height, GetBiomeById(mapData, cell.Biome));
            var pointsStr = string.Join(" ", vertices.Select(v => $"{v.X:F2},{v.Y:F2}"));
            
            svg.AppendLine($"<polygon points=\"{pointsStr}\" class=\"{className}\" />");
        }
        
        // Render coastlines
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            
            bool isCoast = cell.Height > 20 && HasWaterNeighbor(mapData, i, voronoi);
            if (!isCoast) continue;
            
            var vertices = voronoi.GetCellVertices(i);
            if (vertices.Count < 3) continue;
            
            var pointsStr = string.Join(" ", vertices.Select(v => $"{v.X:F2},{v.Y:F2}"));
            svg.AppendLine($"<polygon points=\"{pointsStr}\" class=\"coastline\" />");
        }
        
        // Render cities
        foreach (var burg in mapData.Burgs)
        {
            var citySize = GetCitySize(burg);
            svg.AppendLine($"<circle cx=\"{burg.X:F2}\" cy=\"{burg.Y:F2}\" r=\"{citySize * 2}\" class=\"city\" />");
            
            if (burg.IsCapital)
            {
                svg.AppendLine($"<circle cx=\"{burg.X:F2}\" cy=\"{burg.Y:F2}\" r=\"{citySize * 3}\" class=\"capital\" />");
            }
        }
        
        svg.AppendLine("</g>");
        svg.AppendLine("</svg>");
        
        return svg.ToString();
    }
    
    /// <summary>
    /// Gets CSS class name for terrain type
    /// </summary>
    private static string GetTerrainClassName(byte height, Biome? biome)
    {
        if (height <= 20) return "water";
        if (height <= 25) return "beach";
        if (height <= 50) return "plains";
        if (height <= 70) return "hills";
        return "mountain";
    }
    
    /// <summary>
    /// Gets city size based on burg type and population
    /// </summary>
    private static float GetCitySize(Burg burg)
    {
        // Map new BurgType to sizes
        if (burg.IsCapital) return 5f;
        if (burg.IsPort) return 4f;
        
        return burg.Type switch
        {
            BurgType.Highland => 3f,
            BurgType.River => 3.5f,
            BurgType.Naval => 4f,
            BurgType.Lake => 3f,
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
    /// Checks if a cell has water neighbors
    /// </summary>
    private bool HasWaterNeighbor(MapData mapData, int cellId, Voronoi voronoi)
    {
        var neighbors = voronoi.GetCellNeighbors(cellId);
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
    /// Calculates bounding box of map
    /// </summary>
    private static (double Left, double Top, double Width, double Height) CalculateMapBounds(MapData mapData)
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
        
        return (minX, minY, maxX - minX, maxY - minY);
    }
    
    /// <summary>
    /// Exports map to file with format determined by file extension
    /// </summary>
    public void ExportMap(MapData mapData, MapRenderSettings settings, string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var width = mapData.Width;
        var height = mapData.Height;
        
        switch (extension)
        {
            case ".png":
                ExportToPngAsync(mapData, filePath, width, height).GetAwaiter().GetResult();
                break;
            case ".jpg":
            case ".jpeg":
                ExportToJpegAsync(mapData, filePath, width, height).GetAwaiter().GetResult();
                break;
            case ".svg":
                ExportToSvgAsync(mapData, filePath, width, height).GetAwaiter().GetResult();
                break;
            default:
                throw new NotSupportedException($"File format '{extension}' is not supported. Use .png, .jpg, or .svg");
        }
    }
    
    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        _renderer?.Dispose();
    }
}