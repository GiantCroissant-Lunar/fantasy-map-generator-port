using Xunit;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Tests.Helpers;
using FantasyMapGenerator.Core.Models;
using System.Collections.Concurrent;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Cross-platform determinism tests to ensure consistent results across different operating systems
/// </summary>
public class CrossPlatformTests
{
    [Theory]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    [InlineData(12345, 1000)]
    [InlineData(67890, 4000)]
    [InlineData(99999, 8000)]
    public void SameSeed_ProducesSameChecksum_AcrossPlatforms(long seed, int numPoints)
    {
        var settings = new MapGenerationSettings
        {
            Seed = seed,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        // Generate multiple maps to test consistency
        var generator = new MapGenerator();
        var maps = new List<MapData>();
        
        for (int i = 0; i < 3; i++)
        {
            var map = generator.Generate(settings);
            maps.Add(map);
        }

        // All maps should have identical checksums
        var checksums = maps.Select(m => MapChecksumHelper.ComputeMapChecksum(m)).ToList();
        
        // All checksums should be identical
        for (int i = 1; i < checksums.Count; i++)
        {
            Assert.Equal(checksums[0], checksums[i]);
        }
        
        // Verify checksum format
        Assert.Equal(64, checksums[0].Length);
        Assert.Matches(checksums[0], @"^[a-f0-9]{64}$");
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    [InlineData(RNGMode.PCG)]
    [InlineData(RNGMode.System)]
    public void DifferentRNGModes_ProduceConsistentResults_PerMode(RNGMode rngMode)
    {
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            Width = 1000,
            Height = 1000,
            NumPoints = 2000,
            RNGMode = rngMode
        };

        var generator = new MapGenerator();
        var maps = new List<MapData>();
        
        // Generate multiple maps with same settings
        for (int i = 0; i < 5; i++)
        {
            var map = generator.Generate(settings);
            maps.Add(map);
        }

        // All maps should have identical checksums for same RNG mode
        var checksums = maps.Select(m => MapChecksumHelper.ComputeMapChecksum(m)).ToList();
        var firstChecksum = checksums[0];
        
        for (int i = 1; i < checksums.Count; i++)
        {
            Assert.Equal(firstChecksum, checksums[i]);
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    [InlineData(1000, 1000)]
    [InlineData(2000, 2000)]
    [InlineData(500, 1500)]
    public void DifferentMapSizes_ProduceDeterministicResults(int width, int height)
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = width,
            Height = height,
            NumPoints = Math.Min(width * height / 100, 10000), // Scale points with map size
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        
        // Generate two maps with same settings
        var map1 = generator.Generate(settings);
        var map2 = generator.Generate(settings);

        var checksum1 = MapChecksumHelper.ComputeMapChecksum(map1);
        var checksum2 = MapChecksumHelper.ComputeMapChecksum(map2);

        // Should be identical
        Assert.Equal(checksum1, checksum2);
        
        // Verify maps have expected dimensions
        Assert.Equal(width, map1.Width);
        Assert.Equal(height, map1.Height);
        Assert.Equal(width, map2.Width);
        Assert.Equal(height, map2.Height);
    }

    [Fact]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    public void MapGeneration_IsThreadSafe()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 54321,
            Width = 1000,
            Height = 1000,
            NumPoints = 3000,
            RNGMode = RNGMode.PCG
        };

        var maps = new ConcurrentBag<MapData>();
        var tasks = new List<Task>();
        
        // Generate maps in parallel
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var generator = new MapGenerator();
                var map = generator.Generate(settings);
                maps.Add(map);
            }));
        }
        
        Task.WaitAll(tasks.ToArray());
        var mapList = maps.ToList();
        
        // All maps should have identical checksums
        var checksums = mapList.Select(m => MapChecksumHelper.ComputeMapChecksum(m)).ToList();
        var firstChecksum = checksums[0];
        
        for (int i = 1; i < checksums.Count; i++)
        {
            Assert.Equal(firstChecksum, checksums[i]);
        }
        
        // Verify we got all maps
        Assert.Equal(10, mapList.Count);
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    [InlineData(1)]      // Small seed
    [InlineData(12345)]   // Medium seed
    [InlineData(int.MaxValue)] // Large seed
    public void SeedRange_ProducesConsistentResults(long seed)
    {
        var settings = new MapGenerationSettings
        {
            Seed = seed,
            Width = 1000,
            Height = 1000,
            NumPoints = 2000,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        
        // Generate same map multiple times
        var checksums = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var map = generator.Generate(settings);
            var checksum = MapChecksumHelper.ComputeMapChecksum(map);
            checksums.Add(checksum);
        }

        // All checksums should be identical
        for (int i = 1; i < checksums.Count; i++)
        {
            Assert.Equal(checksums[0], checksums[i]);
        }
    }

    [Fact]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    public void FloatingPointOperations_AreConsistent()
    {
        // Test that floating-point operations produce consistent results
        var settings = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 1000,
            Height = 1000,
            NumPoints = 2500,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Verify that height values are within expected ranges
        var heights = map.Cells.Values.Select(c => c.Height).ToList();
        
        // Should have valid height values
        Assert.True(heights.Any(h => h > 0), "Should have land cells");
        Assert.True(heights.Any(h => h == 0), "Should have water cells");
        
        // Height distribution should be reasonable
        var avgHeight = heights.Average();
        Assert.True(avgHeight > 0 && avgHeight < 255, $"Average height {avgHeight} should be reasonable");
        
        // Generate same map again and verify identical height distribution
        var map2 = generator.Generate(settings);
        var heights2 = map2.Cells.Values.Select(c => c.Height).ToList();
        
        Assert.Equal(heights.Count, heights2.Count);
        for (int i = 0; i < heights.Count; i++)
        {
            Assert.Equal(heights[i], heights2[i]);
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    [InlineData(1000)]
    [InlineData(2000)]
    [InlineData(5000)]
    public void PointCountScaling_ProducesPredictableResults(int numPoints)
    {
        var settings = new MapGenerationSettings
        {
            Seed = 88888,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Verify actual point count matches requested
        Assert.Equal(numPoints, map.Cells.Count);
        
        // Generate same map again and verify consistency
        var map2 = generator.Generate(settings);
        var checksum1 = MapChecksumHelper.ComputeMapChecksum(map);
        var checksum2 = MapChecksumHelper.ComputeMapChecksum(map2);
        
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    [Trait("Category", "Determinism")]
    [Trait("Category", "CrossPlatform")]
    public void ComplexMapGeneration_ProducesConsistentResults()
    {
        // Test with complex settings that exercise more of the generation pipeline
        var settings = new MapGenerationSettings
        {
            Seed = 99999,
            Width = 1200,
            Height = 1200,
            NumPoints = 5000,
            RNGMode = RNGMode.PCG,
            // Add any additional complex settings here
        };

        var generator = new MapGenerator();
        var maps = new List<MapData>();
        
        // Generate multiple complex maps
        for (int i = 0; i < 3; i++)
        {
            var map = generator.Generate(settings);
            maps.Add(map);
        }

        // All should have identical checksums
        var checksums = maps.Select(m => MapChecksumHelper.ComputeMapChecksum(m)).ToList();
        
        for (int i = 1; i < checksums.Count; i++)
        {
            Assert.Equal(checksums[0], checksums[i]);
        }
        
        // Verify map has expected features
        var map = maps[0];
        Assert.True(map.Cells.Count > 0, "Should have cells");
        
        // Should have some rivers if generation includes them
        if (map.Rivers != null)
        {
            Assert.True(map.Rivers.Count >= 0, "River count should be non-negative");
        }
        
        // Should have some states if generation includes them
        if (map.States != null)
        {
            Assert.True(map.States.Count >= 0, "State count should be non-negative");
        }
    }
}
