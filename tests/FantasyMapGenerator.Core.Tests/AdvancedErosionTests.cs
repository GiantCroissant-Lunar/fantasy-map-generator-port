using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Unit tests for the Advanced Erosion system
/// </summary>
public class AdvancedErosionTests
{
    [Fact]
    public void MapGeneration_WithAdvancedErosion_CreatesValleys()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.1
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Check for height variation (valleys and ridges)
        var landCells = map.Cells.Where(c => c.IsLand).ToList();

        if (landCells.Count > 0)
        {
            var heights = landCells.Select(c => (double)c.Height).ToList();
            var minHeight = heights.Min();
            var maxHeight = heights.Max();
            var avgHeight = heights.Average();

            // Should have significant height variation
            Assert.True(maxHeight > minHeight, "Should have height variation");
            Assert.True(maxHeight - minHeight > 10, "Height variation should be significant");

            // Calculate standard deviation
            var variance = heights.Select(h => Math.Pow(h - avgHeight, 2)).Average();
            var stdDev = Math.Sqrt(variance);

            Assert.True(stdDev > 5, $"Erosion should create height variation (stdDev: {stdDev:F2})");
        }
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_DifferentFromSimpleErosion()
    {
        var settingsSimple = new MapGenerationSettings
        {
            Seed = 99999,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = false,
            EnableRiverErosion = true,
            MaxErosionDepth = 5
        };

        var settingsAdvanced = new MapGenerationSettings
        {
            Seed = 99999,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.1
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var mapSimple = generator1.Generate(settingsSimple);
        var mapAdvanced = generator2.Generate(settingsAdvanced);

        // Heights should be different between the two erosion methods
        int differentCells = 0;
        for (int i = 0; i < Math.Min(mapSimple.Cells.Count, mapAdvanced.Cells.Count); i++)
        {
            if (mapSimple.Cells[i].Height != mapAdvanced.Cells[i].Height)
            {
                differentCells++;
            }
        }

        // At least 10% of cells should be different
        Assert.True(differentCells > mapSimple.Cells.Count * 0.1,
            $"Advanced erosion should produce different results ({differentCells} different cells)");
    }

    [Theory]
    [InlineData(1, 0.1)]
    [InlineData(5, 0.1)]
    [InlineData(10, 0.05)]
    public void AdvancedErosion_CompletesInReasonableTime(int iterations, double amount)
    {
        var settings = new MapGenerationSettings
        {
            Seed = 54321,
            Width = 300,
            Height = 300,
            NumPoints = 800,
            UseAdvancedErosion = true,
            ErosionIterations = iterations,
            ErosionAmount = amount
        };

        var generator = new MapGenerator();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var map = generator.Generate(settings);

        stopwatch.Stop();

        // Should complete in reasonable time (< 10s for full generation)
        Assert.True(stopwatch.ElapsedMilliseconds < 10000,
            $"Generation with {iterations} erosion iterations took {stopwatch.ElapsedMilliseconds}ms, should be < 10s");

        Assert.NotNull(map);
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_IsDeterministic()
    {
        var settings1 = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.1
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.1
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // Should generate identical maps
        Assert.Equal(map1.Cells.Count, map2.Cells.Count);

        // Compare heights of corresponding cells
        for (int i = 0; i < Math.Min(map1.Cells.Count, map2.Cells.Count); i++)
        {
            Assert.Equal(map1.Cells[i].Height, map2.Cells[i].Height);
        }
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_DoesNotInvertTerrain()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 11111,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 10, // Many iterations
            ErosionAmount = 0.2 // High amount
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Check that all land cells are within valid range
        var landCells = map.Cells.Where(c => c.IsLand).ToList();

        Assert.All(landCells, cell =>
        {
            Assert.True(cell.Height >= 20, $"Cell {cell.Id} height {cell.Height} is below sea level");
            Assert.True(cell.Height <= 100, $"Cell {cell.Id} height {cell.Height} exceeds maximum");
        });
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_WithDifferentIterations()
    {
        var settings1 = new MapGenerationSettings
        {
            Seed = 22222,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 1,
            ErosionAmount = 0.1
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = 22222,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 10,
            ErosionAmount = 0.1
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // More iterations should produce more refined terrain
        int differentCells = 0;
        for (int i = 0; i < Math.Min(map1.Cells.Count, map2.Cells.Count); i++)
        {
            if (map1.Cells[i].Height != map2.Cells[i].Height)
            {
                differentCells++;
            }
        }

        // Should have differences due to different iteration counts
        Assert.True(differentCells > 0, "Different iteration counts should produce different results");
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_WithDifferentAmounts()
    {
        var settings1 = new MapGenerationSettings
        {
            Seed = 33333,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.05 // Low amount
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = 33333,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.2 // High amount
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // Different erosion amounts should produce different results
        int differentCells = 0;
        for (int i = 0; i < Math.Min(map1.Cells.Count, map2.Cells.Count); i++)
        {
            if (map1.Cells[i].Height != map2.Cells[i].Height)
            {
                differentCells++;
            }
        }

        Assert.True(differentCells > 0, "Different erosion amounts should produce different results");
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_CreatesRealisticTerrain()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 44444,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.1
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        var landCells = map.Cells.Where(c => c.IsLand).ToList();

        if (landCells.Count > 0)
        {
            var heights = landCells.Select(c => c.Height).ToList();
            var minHeight = heights.Min();
            var maxHeight = heights.Max();
            var avgHeight = heights.Average();

            // Terrain should have reasonable properties
            Assert.True(minHeight >= 20, "Minimum height should be at or above sea level");
            Assert.True(maxHeight <= 100, "Maximum height should not exceed limit");
            Assert.True(avgHeight > 20 && avgHeight < 100, "Average height should be reasonable");

            // Should have a distribution of heights (not all the same)
            var uniqueHeights = heights.Distinct().Count();
            Assert.True(uniqueHeights > 5, "Should have variety in heights");
        }
    }

    [Fact]
    public void MapGeneration_NoErosion_Baseline()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 55555,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            UseAdvancedErosion = false,
            EnableRiverErosion = false
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Should generate successfully without any erosion
        Assert.NotNull(map);
        Assert.True(map.Cells.Count > 0);
    }

    [Fact]
    public void MapGeneration_AdvancedErosion_Performance()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 66666,
            Width = 400,
            Height = 400,
            NumPoints = 2000, // Larger map
            UseAdvancedErosion = true,
            ErosionIterations = 5,
            ErosionAmount = 0.1
        };

        var generator = new MapGenerator();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var map = generator.Generate(settings);

        stopwatch.Stop();

        // Should complete in reasonable time even for large maps
        Assert.True(stopwatch.ElapsedMilliseconds < 20000,
            $"Large map generation took {stopwatch.ElapsedMilliseconds}ms, should be < 20s");

        Assert.NotNull(map);
        Assert.True(map.Cells.Count > 0);
    }
}
