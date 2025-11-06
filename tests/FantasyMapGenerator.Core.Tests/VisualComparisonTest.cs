using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Rendering;
using SkiaSharp;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Generates side-by-side comparison images of OLD vs NEW heightmap generation
/// </summary>
public class VisualComparisonTest
{
    [Fact]
    public void GenerateSideBySideComparison()
    {
        Console.WriteLine("\n=== Generating Visual Comparison ===\n");

        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 1000,
            SeaLevel = 0.3f
        };

        var generator = new MapGenerator();

        // Generate OLD map
        Console.WriteLine("Generating OLD map (random noise)...");
        settings.UseAdvancedNoise = false;
        settings.HeightmapTemplate = null;
        var mapOld = generator.Generate(settings);

        // Generate NEW map
        Console.WriteLine("Generating NEW map (FastNoiseLite island)...");
        settings.UseAdvancedNoise = true;
        settings.HeightmapTemplate = "island";
        var mapNew = generator.Generate(settings);

        // Render both maps
        Console.WriteLine("Rendering maps...");
        var renderSettings = new MapRenderSettings
        {
            ShowTerrain = true,
            ShowCoastlines = false,
            ShowBorders = false,
            ShowCities = false,
            ShowLabels = false
        };

        var renderer = new MapRenderer(renderSettings);

        // Render OLD map
        using var surfaceOld = renderer.RenderMap(mapOld, 800, 600);
        using var imageOld = surfaceOld.Snapshot();

        // Render NEW map
        using var surfaceNew = renderer.RenderMap(mapNew, 800, 600);
        using var imageNew = surfaceNew.Snapshot();

        // Create side-by-side comparison image
        var comparisonWidth = 1600 + 50; // 2 maps + gap
        var comparisonHeight = 600 + 100; // maps + header

        using var comparisonSurface = SKSurface.Create(new SKImageInfo(comparisonWidth, comparisonHeight));
        var canvas = comparisonSurface.Canvas;

        // White background
        canvas.Clear(SKColors.White);

        // Draw title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 32,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        canvas.DrawText("OLD (Random Noise)", 200, 40, titlePaint);
        canvas.DrawText("NEW (FastNoiseLite Island)", 900, 40, titlePaint);

        // Draw stats
        using var statsPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        var statsOld = GetStats(mapOld.Heights);
        var statsNew = GetStats(mapNew.Heights);

        canvas.DrawText($"Land: {statsOld.LandPercent:F1}%", 250, 70, statsPaint);
        canvas.DrawText($"Water: {statsOld.WaterPercent:F1}%", 450, 70, statsPaint);

        canvas.DrawText($"Land: {statsNew.LandPercent:F1}%", 950, 70, statsPaint);
        canvas.DrawText($"Water: {statsNew.WaterPercent:F1}%", 1150, 70, statsPaint);

        // Draw maps
        canvas.DrawImage(imageOld, 0, 100);
        canvas.DrawImage(imageNew, 850, 100);

        // Draw separator line
        using var linePaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(825, 0, 825, comparisonHeight, linePaint);

        // Save comparison image
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "heightmap_comparison.png");
        using var comparisonImage = comparisonSurface.Snapshot();
        using var data = comparisonImage.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine($"\n✅ Comparison image saved to:");
        Console.WriteLine($"   {outputPath}\n");

        // Also save individual maps
        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "map_old.png");
        var newPath = Path.Combine(Directory.GetCurrentDirectory(), "map_new.png");

        using (var oldData = imageOld.Encode(SKEncodedImageFormat.Png, 100))
        using (var oldStream = File.OpenWrite(oldPath))
        {
            oldData.SaveTo(oldStream);
        }

        using (var newData = imageNew.Encode(SKEncodedImageFormat.Png, 100))
        using (var newStream = File.OpenWrite(newPath))
        {
            newData.SaveTo(newStream);
        }

        Console.WriteLine($"Individual maps also saved:");
        Console.WriteLine($"   OLD: {oldPath}");
        Console.WriteLine($"   NEW: {newPath}\n");

        Console.WriteLine("=== Comparison Complete ===\n");

        // Print detailed stats
        Console.WriteLine("OLD (Random Noise):");
        Console.WriteLine($"  Range: {statsOld.Min}-{statsOld.Max}");
        Console.WriteLine($"  Average: {statsOld.Average:F1}");
        Console.WriteLine($"  Land: {statsOld.LandPercent:F1}% ({statsOld.LandCells} cells)");
        Console.WriteLine($"  Water: {statsOld.WaterPercent:F1}% ({statsOld.WaterCells} cells)\n");

        Console.WriteLine("NEW (FastNoiseLite Island):");
        Console.WriteLine($"  Range: {statsNew.Min}-{statsNew.Max}");
        Console.WriteLine($"  Average: {statsNew.Average:F1}");
        Console.WriteLine($"  Land: {statsNew.LandPercent:F1}% ({statsNew.LandCells} cells)");
        Console.WriteLine($"  Water: {statsNew.WaterPercent:F1}% ({statsNew.WaterCells} cells)\n");
    }

    private class HeightmapStats
    {
        public byte Min { get; set; }
        public byte Max { get; set; }
        public double Average { get; set; }
        public int LandCells { get; set; }
        public int WaterCells { get; set; }
        public double LandPercent { get; set; }
        public double WaterPercent { get; set; }
    }

    private HeightmapStats GetStats(byte[] heights)
    {
        var landCells = heights.Count(h => h > 30);
        var waterCells = heights.Count(h => h <= 30);

        return new HeightmapStats
        {
            Min = heights.Min(),
            Max = heights.Max(),
            Average = heights.Average(h => (double)h),
            LandCells = landCells,
            WaterCells = waterCells,
            LandPercent = (landCells * 100.0) / heights.Length,
            WaterPercent = (waterCells * 100.0) / heights.Length
        };
    }
}
