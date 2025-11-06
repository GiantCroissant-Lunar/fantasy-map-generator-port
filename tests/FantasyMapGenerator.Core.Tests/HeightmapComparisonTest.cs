using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Test to verify FastNoiseLite produces better heightmaps than the old template system
/// </summary>
public class HeightmapComparisonTest
{
    [Fact]
    public void FastNoiseLite_ProducesDifferentResults_ThanTemplateSystem()
    {
        // Arrange
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 500,
            SeaLevel = 0.3f,
            HeightmapTemplate = "island"
        };

        var generator = new MapGenerator();

        // Act - Generate with OLD template system
        settings.UseAdvancedNoise = false;
        var mapOld = generator.Generate(settings);

        // Act - Generate with NEW FastNoiseLite
        settings.UseAdvancedNoise = true;
        var mapNew = generator.Generate(settings);

        // Assert - They should be different
        Assert.NotEqual(mapOld.Heights, mapNew.Heights);
    }

    [Fact]
    public void FastNoiseLite_Island_ProducesRealisticLandWaterRatio()
    {
        // Arrange
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 1000,
            SeaLevel = 0.3f,
            UseAdvancedNoise = true,
            HeightmapTemplate = "island"
        };

        var generator = new MapGenerator();

        // Act
        var map = generator.Generate(settings);

        // Assert - Island should have 20-50% land (radial falloff creates coastline)
        var landCells = map.Heights.Count(h => h > 30); // 30 ≈ 30% sea level
        var landPercent = (landCells * 100.0) / map.Heights.Length;

        Assert.InRange(landPercent, 20, 50);
    }

    [Fact]
    public void FastNoiseLite_ProducesSmoothTransitions()
    {
        // Arrange
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 1000,
            SeaLevel = 0.3f,
            UseAdvancedNoise = true,
            HeightmapTemplate = "island"
        };

        var generator = new MapGenerator();

        // Act
        var map = generator.Generate(settings);

        // Assert - Check that adjacent cells don't have massive height differences
        int extremeJumps = 0;
        for (int i = 0; i < map.Cells.Count; i++)
        {
            var cell = map.Cells[i];
            var cellHeight = map.Heights[i];

            foreach (var neighborId in cell.Neighbors)
            {
                if (neighborId >= 0 && neighborId < map.Heights.Length)
                {
                    var neighborHeight = map.Heights[neighborId];
                    var diff = Math.Abs(cellHeight - neighborHeight);

                    // Flag if height difference is > 40 (very steep cliff)
                    if (diff > 40)
                    {
                        extremeJumps++;
                    }
                }
            }
        }

        // Should have very few extreme jumps (< 5% of cells)
        var extremePercent = (extremeJumps * 100.0) / map.Cells.Count;
        Assert.True(extremePercent < 5, $"Too many extreme height jumps: {extremePercent:F1}%");
    }

    [Theory]
    [InlineData("island")]
    [InlineData("continents")]
    [InlineData("archipelago")]
    [InlineData("pangea")]
    [InlineData("mediterranean")]
    public void FastNoiseLite_AllProfiles_ProduceValidHeightmaps(string profile)
    {
        // Arrange
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 500,
            SeaLevel = 0.3f,
            UseAdvancedNoise = true,
            HeightmapTemplate = profile
        };

        var generator = new MapGenerator();

        // Act
        var map = generator.Generate(settings);

        // Assert
        Assert.NotNull(map.Heights);
        Assert.Equal(map.Cells.Count, map.Heights.Length);
        Assert.All(map.Heights, h => Assert.InRange(h, (byte)0, (byte)100));
    }

    [Fact]
    public void PrintHeightmapComparison()
    {
        // Arrange
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 1000,
            SeaLevel = 0.3f
        };

        var generator = new MapGenerator();

        // Generate OLD (without template to avoid parsing errors)
        settings.UseAdvancedNoise = false;
        settings.HeightmapTemplate = null; // Use default noise generation
        var mapOld = generator.Generate(settings);

        // Generate NEW
        settings.UseAdvancedNoise = true;
        settings.HeightmapTemplate = "island";
        var mapNew = generator.Generate(settings);

        // Print comparison
        Console.WriteLine("\n=== HEIGHTMAP COMPARISON ===\n");
        PrintStats("OLD (Random Noise)", mapOld.Heights);
        Console.WriteLine();
        PrintStats("NEW (FastNoise Island)", mapNew.Heights);
        Console.WriteLine("\n============================\n");
    }

    private void PrintStats(string name, byte[] heights)
    {
        var avg = heights.Average(h => (double)h);
        var min = heights.Min();
        var max = heights.Max();
        var variance = heights.Average(h => Math.Pow(h - avg, 2));
        var stddev = Math.Sqrt(variance);

        var land = heights.Count(h => h > 30);
        var water = heights.Count(h => h <= 30);
        var landPercent = (land * 100.0) / heights.Length;

        Console.WriteLine($"{name}:");
        Console.WriteLine($"  Range: {min} - {max}");
        Console.WriteLine($"  Average: {avg:F2}");
        Console.WriteLine($"  Std Dev: {stddev:F2}");
        Console.WriteLine($"  Land: {landPercent:F1}% | Water: {100 - landPercent:F1}%");
    }
}
