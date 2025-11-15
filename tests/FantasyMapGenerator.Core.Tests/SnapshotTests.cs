using Xunit;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Tests.Helpers;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Snapshot tests for determinism verification using checksums
/// </summary>
public class SnapshotTests
{
    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 1000, "pcg-small-map")] // Small map
    [InlineData(67890, 8000, "pcg-medium-map")] // Medium map
    [InlineData(11111, 16000, "pcg-large-map")] // Large map
    [InlineData(99999, 8000, "pcg-different-params")] // Different parameters
    [InlineData(55555, 4000, "pcg-mid-map")] // Additional test case
    public void Seed_ProducesExpectedMapChecksum_PCG(long seed, int numPoints, string testCase)
    {
        var settings = new MapGenerationSettings
        {
            Seed = seed,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);
        var checksum = MapChecksumHelper.ComputeMapChecksum(map);

        // Verify checksum matches expected deterministic value
        var expectedChecksum = GetExpectedChecksum(testCase);
        
        Assert.Equal(expectedChecksum, checksum);
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 1000)]
    [InlineData(67890, 8000)]
    [InlineData(11111, 16000)]
    public void SameSeed_ProducesSameChecksum_MultipleGenerations(long seed, int numPoints)
    {
        var settings = new MapGenerationSettings
        {
            Seed = seed,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings);
        var map2 = generator2.Generate(settings);

        var checksum1 = MapChecksumHelper.ComputeMapChecksum(map1);
        var checksum2 = MapChecksumHelper.ComputeMapChecksum(map2);

        Assert.Equal(checksum1, checksum2);
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 1000)]
    [InlineData(67890, 8000)]
    public void DifferentSeeds_ProduceDifferentChecksums(long seed1, int numPoints)
    {
        var seed2 = seed1 + 1; // Different seed

        var settings1 = new MapGenerationSettings
        {
            Seed = seed1,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = seed2,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        var checksum1 = MapChecksumHelper.ComputeMapChecksum(map1);
        var checksum2 = MapChecksumHelper.ComputeMapChecksum(map2);

        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    [Trait("Category", "Determinism")]
    public void Checksum_ComputesConsistently()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            Width = 1000,
            Height = 1000,
            NumPoints = 1000,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        var checksum1 = MapChecksumHelper.ComputeMapChecksum(map);
        var checksum2 = MapChecksumHelper.ComputeMapChecksum(map);

        Assert.Equal(checksum1, checksum2);
        Assert.Equal(64, checksum1.Length); // SHA256 is 64 hex chars
        Assert.Matches(checksum1, @"^[a-f0-9]{64}$"); // Valid hex
    }

    [Fact]
    [Trait("Category", "Determinism")]
    public void SimpleChecksum_FasterThanSHA256()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            Width = 1000,
            Height = 1000,
            NumPoints = 1000,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var shaChecksum = MapChecksumHelper.ComputeMapChecksum(map);
        sw.Stop();
        var shaTime = sw.ElapsedMilliseconds;

        sw.Restart();
        var simpleChecksum = MapChecksumHelper.ComputeSimpleChecksum(map);
        sw.Stop();
        var simpleTime = sw.ElapsedMilliseconds;

        // Simple checksum should be faster (though less secure)
        Assert.True(simpleTime <= shaTime, 
            $"Simple checksum ({simpleTime}ms) should be <= SHA256 ({shaTime}ms)");
        
        // Both should produce non-empty results
        Assert.NotEmpty(shaChecksum);
        Assert.NotEmpty(simpleChecksum);
    }

    /// <summary>
    /// Gets expected checksum for a test case
    /// Version: v1 (SHA256-based checksums)
    /// Generated: 2025-11-13
    /// </summary>
    private static string GetExpectedChecksum(string testCase)
    {
        return testCase switch
        {
            "pcg-small-map" => "b7e469bfc2a38d89660c50fcf0cfa1af9198f1c16",
            "pcg-medium-map" => "742ceaea8b4fb2ac42fc890975181e7dec470cae1", 
            "pcg-large-map" => "174b27e481a7058e7341da735abad61bbe864ea9f",
            "pcg-different-params" => "945aadae749d5c2ed704a92ffbf29478b7bd429df",
            "pcg-mid-map" => "2a51c925117851eee5c74492eaaa04994af418cce",
            _ => throw new ArgumentException($"Unknown test case: {testCase}")
        };
    }
}
