using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Tests;

/// <summary>
/// Quick test to compare old vs new heightmap generation
/// Run with: dotnet run TestHeightmapComparison.cs
/// </summary>
class TestHeightmapComparison
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Heightmap Comparison Test ===\n");

        // Create test map
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 1000,
            SeaLevel = 0.3f
        };

        Console.WriteLine("Generating map with 1000 cells...\n");

        // Test OLD heightmap (template-based with blobs/lines)
        Console.WriteLine("--- OLD Heightmap (Template-based) ---");
        settings.UseAdvancedNoise = false;
        settings.HeightmapTemplate = "island";

        var generator1 = new MapGenerator();
        var map1 = generator1.Generate(settings);

        PrintHeightmapStats("OLD (Blob/Line)", map1.Heights);

        // Test NEW heightmap (FastNoiseLite)
        Console.WriteLine("\n--- NEW Heightmap (FastNoiseLite) ---");
        settings.UseAdvancedNoise = true;
        settings.HeightmapTemplate = "island";

        var generator2 = new MapGenerator();
        var map2 = generator2.Generate(settings);

        PrintHeightmapStats("NEW (FastNoise)", map2.Heights);

        Console.WriteLine("\n=== Comparison Complete ===");
        Console.WriteLine("The NEW heightmap should show:");
        Console.WriteLine("- Smoother transitions (lower stddev)");
        Console.WriteLine("- More realistic distribution");
        Console.WriteLine("- Better land/water ratio for island profile");
    }

    static void PrintHeightmapStats(string name, byte[] heights)
    {
        var avg = heights.Average(h => (double)h);
        var min = heights.Min();
        var max = heights.Max();
        var variance = heights.Average(h => Math.Pow(h - avg, 2));
        var stddev = Math.Sqrt(variance);

        var land = heights.Count(h => h > 30); // ~30% sea level
        var water = heights.Count(h => h <= 30);
        var landPercent = (land * 100.0) / heights.Length;

        Console.WriteLine($"{name}:");
        Console.WriteLine($"  Range: {min} - {max}");
        Console.WriteLine($"  Average: {avg:F2}");
        Console.WriteLine($"  Std Dev: {stddev:F2}");
        Console.WriteLine($"  Land: {landPercent:F1}% ({land} cells)");
        Console.WriteLine($"  Water: {100 - landPercent:F1}% ({water} cells)");

        // Show distribution histogram
        Console.Write("  Distribution: ");
        for (int i = 0; i <= 100; i += 10)
        {
            var count = heights.Count(h => h >= i && h < i + 10);
            var bar = new string('█', count / 20);
            Console.Write($"{i:D2}-{i+9:D2}:{bar.PadRight(5)} ");
        }
        Console.WriteLine();
    }
}
