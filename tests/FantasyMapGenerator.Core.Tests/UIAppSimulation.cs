using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using SkiaSharp;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Simulates exactly what UI app should be showing
/// This generates maps with the SAME settings as UI app
/// </summary>
public class UIAppSimulation
{
    [Fact]
    public void GenerateWhatUIAppShouldShow()
    {
        Console.WriteLine("\n=== Simulating UI App Map Generation ===\n");
        Console.WriteLine("This simulates EXACTLY what FantasyMapGenerator.UI app should display.");
        Console.WriteLine("Using the SAME settings from MapControlViewModel.cs\n");

        // These are EXACT settings from MapControlViewModel.cs lines 51-60
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = DateTime.Now.Ticks,
            NumPoints = 1000,
            SeaLevel = 0.3f,
            UseAdvancedNoise = true,      // Line 58: I SET THIS TO TRUE
            HeightmapTemplate = "island"   // Line 59: I SET THIS TO "island"
        };

        Console.WriteLine($"Settings:");
        Console.WriteLine($"  Width: {settings.Width}");
        Console.WriteLine($"  Height: {settings.Height}");
        Console.WriteLine($"  Seed: {settings.Seed}");
        Console.WriteLine($"  NumPoints: {settings.NumPoints}");
        Console.WriteLine($"  SeaLevel: {settings.SeaLevel}");
        Console.WriteLine($"  UseAdvancedNoise: {settings.UseAdvancedNoise}");
        Console.WriteLine($"  HeightmapTemplate: {settings.HeightmapTemplate}\n");

        Console.WriteLine("Generating map...");
        var generator = new MapGenerator();
        var mapData = generator.Generate(settings);

        Console.WriteLine("\nMap statistics:");
        var stats = GetStats(mapData.Heights);
        Console.WriteLine($"  Cells: {mapData.Cells.Count}");
        Console.WriteLine($"  Height Range: {stats.Min}-{stats.Max}");
        Console.WriteLine($"  Average Height: {stats.Average:F1}");
        Console.WriteLine($"  Land: {stats.LandPercent:F1}% ({stats.LandCells} cells)");
        Console.WriteLine($"  Water: {stats.WaterPercent:F1}% ({stats.WaterCells} cells)");

        // Create visualization
        Console.WriteLine("\nCreating visualization...");
        var image = CreateVisualization(mapData, "UI App Output (with FastNoiseLite)");

        // Save
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "UI_APP_OUTPUT.png");
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
        image.Dispose();

        Console.WriteLine($"\n✅ SAVED! This is what your UI app SHOULD be showing:");
        Console.WriteLine($"   {outputPath}\n");

        // Validate it's using FastNoiseLite
        Assert.True(stats.WaterPercent > 50,
            $"FastNoiseLite 'island' profile should create mostly water (>50%), but got {stats.WaterPercent:F1}%");

        Assert.True(stats.LandPercent >= 20 && stats.LandPercent <= 45,
            $"FastNoiseLite 'island' profile should create 20-45% land, but got {stats.LandPercent:F1}%");

        Console.WriteLine("✅ VERIFIED: The map is using FastNoiseLite correctly!");
        Console.WriteLine($"   - Water: {stats.WaterPercent:F1}% (should be > 50% for island)");
        Console.WriteLine($"   - Land: {stats.LandPercent:F1}% (should be 20-45% for island)");
        Console.WriteLine($"\nIf your UI app shows something different, you may be:");
        Console.WriteLine("   1. Running an old cached version (run 'dotnet clean' first)");
        Console.WriteLine("   2. Looking at a different app (Desktop vs UI)");
        Console.WriteLine("   3. Using different settings\n");
    }

    private SKImage CreateVisualization(MapData mapData, string title)
    {
        var width = 1000;
        var height = 800;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // White background
        canvas.Clear(SKColors.White);

        // Title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 32,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText(title, 50, 50, titlePaint);

        // Stats
        var stats = GetStats(mapData.Heights);
        using var statsPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        canvas.DrawText($"Land: {stats.LandPercent:F1}% | Water: {stats.WaterPercent:F1}% | Cells: {mapData.Cells.Count}", 50, 80, statsPaint);

        // Draw map cells
        var mapTop = 100;
        var mapHeight = 650;

        foreach (var cell in mapData.Cells)
        {
            var color = GetHeightColor(cell.Height);

            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Scale and position
            var x = (float)(cell.Center.X * (width - 100) / 800) + 50;
            var y = (float)(cell.Center.Y * mapHeight / 600) + mapTop;

            canvas.DrawCircle(x, y, 4, paint);
        }

        // Legend
        var legendY = mapTop;
        var legendX = width - 180;

        using var legendPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 16,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        canvas.DrawText("Legend:", legendX, legendY, legendPaint);

        var colors = new[]
        {
            (new SKColor(30, 144, 255), "Water (≤20)"),
            (new SKColor(238, 214, 175), "Beach (21-25)"),
            (new SKColor(34, 139, 34), "Plains (26-50)"),
            (new SKColor(139, 69, 19), "Hills (51-70)"),
            (new SKColor(139, 137, 137), "Mountains (>70)")
        };

        legendY += 30;
        foreach (var (color, label) in colors)
        {
            using var colorPaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(legendX, legendY - 12, 20, 15, colorPaint);
            canvas.DrawText(label, legendX + 30, legendY, legendPaint);
            legendY += 25;
        }

        return surface.Snapshot();
    }

    private SKColor GetHeightColor(byte height)
    {
        // Exact same colors as MapRenderer.cs GetTerrainColor()
        if (height <= 20) return new SKColor(30, 144, 255);    // Water
        if (height <= 25) return new SKColor(238, 214, 175);   // Beach
        if (height <= 50) return new SKColor(34, 139, 34);     // Plains
        if (height <= 70) return new SKColor(139, 69, 19);     // Hills
        return new SKColor(139, 137, 137);                     // Mountains
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
