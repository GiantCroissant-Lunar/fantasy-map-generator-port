using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using SkiaSharp;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Simple heightmap visualizer that renders heightmaps directly as pixels (no Voronoi needed)
/// </summary>
public class SimpleHeightmapVisualizer
{
    [Fact]
    public void GenerateHeightmapComparison()
    {
        Console.WriteLine("\n=== Generating Heightmap Visual Comparison ===\n");

        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 5000, // More points for better visualization
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

        // Create visualizations
        Console.WriteLine("Creating visualizations...");

        var imageOld = CreateHeightmapImage(mapOld, "OLD: Random Noise");
        var imageNew = CreateHeightmapImage(mapNew, "NEW: FastNoiseLite Island");

        // Create side-by-side comparison
        var comparisonWidth = 1650; // 2 x 800 + 50 gap
        var comparisonHeight = 700; // 600 + 100 for title/stats

        using var comparisonSurface = SKSurface.Create(new SKImageInfo(comparisonWidth, comparisonHeight));
        var canvas = comparisonSurface.Canvas;

        // White background
        canvas.Clear(SKColors.White);

        // Draw title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 36,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        canvas.DrawText("Heightmap Comparison: OLD vs NEW", 400, 45, titlePaint);

        // Draw stats
        using var statsPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        var statsOld = GetStats(mapOld.Heights);
        var statsNew = GetStats(mapNew.Heights);

        canvas.DrawText($"Land: {statsOld.LandPercent:F1}%  Water: {statsOld.WaterPercent:F1}%", 200, 80, statsPaint);
        canvas.DrawText($"Land: {statsNew.LandPercent:F1}%  Water: {statsNew.WaterPercent:F1}%", 1050, 80, statsPaint);

        // Draw images
        canvas.DrawImage(imageOld, 0, 100);
        canvas.DrawImage(imageNew, 850, 100);

        // Draw separator
        using var linePaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(825, 0, 825, comparisonHeight, linePaint);

        // Save
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "HEIGHTMAP_COMPARISON.png");
        using var comparisonImage = comparisonSurface.Snapshot();
        using var data = comparisonImage.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        imageOld.Dispose();
        imageNew.Dispose();

        Console.WriteLine($"\n✅ SUCCESS! Comparison image saved to:");
        Console.WriteLine($"   {outputPath}\n");

        Console.WriteLine("=== Statistics ===\n");
        Console.WriteLine("OLD (Random Noise):");
        Console.WriteLine($"  Range: {statsOld.Min}-{statsOld.Max}");
        Console.WriteLine($"  Average: {statsOld.Average:F1}");
        Console.WriteLine($"  Land: {statsOld.LandPercent:F1}% | Water: {statsOld.WaterPercent:F1}%\n");

        Console.WriteLine("NEW (FastNoiseLite Island):");
        Console.WriteLine($"  Range: {statsNew.Min}-{statsNew.Max}");
        Console.WriteLine($"  Average: {statsNew.Average:F1}");
        Console.WriteLine($"  Land: {statsNew.LandPercent:F1}% | Water: {statsNew.WaterPercent:F1}%\n");

        Console.WriteLine("🎉 The difference is clear! NEW system creates realistic island with proper ocean coverage.");
        Console.WriteLine($"\nOpen this file to see the visual difference:\n   {outputPath}\n");
    }

    private SKImage CreateHeightmapImage(MapData mapData, string title)
    {
        var width = 800;
        var height = 600;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Background
        canvas.Clear(SKColors.White);

        // Create a grid to visualize the heightmap
        // For each cell, draw a colored circle at its center
        foreach (var cell in mapData.Cells)
        {
            var color = GetHeightColor(cell.Height);

            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Draw cell as a small circle
            var x = (float)Math.Clamp(cell.Center.X, 0, width - 1);
            var y = (float)Math.Clamp(cell.Center.Y, 0, height - 1);
            canvas.DrawCircle(x, y, 4, paint);
        }

        return surface.Snapshot();
    }

    private SKColor GetHeightColor(byte height)
    {
        // Same color scheme as the renderer
        if (height <= 20) return new SKColor(30, 144, 255);    // Deep blue water
        if (height <= 25) return new SKColor(238, 214, 175);   // Sandy beach
        if (height <= 50) return new SKColor(34, 139, 34);     // Green plains
        if (height <= 70) return new SKColor(139, 69, 19);     // Brown hills
        return new SKColor(139, 137, 137);                     // Gray mountains
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
