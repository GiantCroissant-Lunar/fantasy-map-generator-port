using Xunit;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Tests for statistical distributions and sanity bounds
/// </summary>
public class StatisticalTests
{
    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 8000)]
    [InlineData(11111, 16000)]
    [InlineData(55555, 4000)]
    public void Map_HasReasonableLandWaterRatio(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        var landCells = map.Cells.Count(c => c.IsLand);
        var waterCells = map.Cells.Count(c => c.IsOcean);
        var landPercent = (double)landCells / map.Cells.Count * 100;

        // Typical island maps have 20-40% land
        Assert.True(landPercent >= 15 && landPercent <= 50,
            $"Map has {landPercent:F1}% land (expected 15-50%). Land cells: {landCells}, Water cells: {waterCells}");
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Rivers_HaveReasonableDistribution(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        // Should generate some rivers (but not too many)
        Assert.True(map.Rivers.Count >= 1 && map.Rivers.Count <= numPoints / 10,
            $"Map has {map.Rivers.Count} rivers (expected 1-{numPoints / 10})");

        if (map.Rivers.Count > 0)
        {
            // Rivers should have reasonable lengths
            var avgLength = map.Rivers.Average(r => r.Cells.Count);
            Assert.True(avgLength >= 3 && avgLength <= 100,
                $"Average river length is {avgLength:F1} (expected 3-100)");

            // Check river length distribution
            var minLength = map.Rivers.Min(r => r.Cells.Count);
            var maxLength = map.Rivers.Max(r => r.Cells.Count);
            Assert.True(minLength >= 2,
                $"Shortest river has only {minLength} cells (expected >= 2)");
            Assert.True(maxLength <= numPoints / 2,
                $"Longest river has {maxLength} cells (expected <= {numPoints / 2})");
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Biomes_CoverAllLandCells(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        var landCells = map.Cells.Where(c => c.IsLand).ToList();
        var cellsWithBiome = landCells.Count(c => c.Biome >= 0);

        // All land cells should have a biome assigned
        Assert.True(landCells.Count == cellsWithBiome,
            $"{landCells.Count - cellsWithBiome} land cells have no biome assigned");
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Biomes_HaveReasonableDistribution(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        var landCells = map.Cells.Where(c => c.IsLand).ToList();
        if (landCells.Count == 0) return;

        // Count biomes
        var biomeCounts = landCells
            .Where(c => c.Biome >= 0)
            .GroupBy(c => c.Biome)
            .ToDictionary(g => g.Key, g => g.Count());

        // Should have multiple biome types (not just one)
        Assert.True(biomeCounts.Count >= 2,
            $"Map has only {biomeCounts.Count} biome types (expected >= 2)");

        // No single biome should dominate excessively (>80% of land)
        var maxBiomePercent = (double)biomeCounts.Values.Max() / landCells.Count * 100;
        Assert.True(maxBiomePercent <= 80,
            $"Dominant biome covers {maxBiomePercent:F1}% of land (expected <= 80%)");
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Heights_HaveReasonableRange(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        if (map.Heights == null || map.Heights.Length == 0) return;

        var minHeight = map.Heights.Min();
        var maxHeight = map.Heights.Max();
        var avgHeight = map.Heights.Select(h => (double)h).Average();

        // Heights should be in reasonable range
        Assert.True(minHeight >= 0 && minHeight <= 100,
            $"Minimum height is {minHeight} (expected 0-100)");
        Assert.True(maxHeight >= 0 && maxHeight <= 100,
            $"Maximum height is {maxHeight} (expected 0-100)");
        Assert.True(avgHeight >= 20 && avgHeight <= 80,
            $"Average height is {avgHeight:F1} (expected 20-80)");
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void States_HaveReasonableDistribution(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        var landCells = map.Cells.Where(c => c.IsLand).ToList();
        if (landCells.Count == 0) return;

        // Should have reasonable number of states
        var expectedMaxStates = Math.Max(1, landCells.Count / 100); // ~1 state per 100 land cells
        Assert.True(map.States.Count >= 1 && map.States.Count <= expectedMaxStates,
            $"Map has {map.States.Count} states (expected 1-{expectedMaxStates})");

        if (map.States.Count > 0)
        {
            // States should have reasonable land distribution
            var cellsPerState = landCells.Count / (double)map.States.Count;
            Assert.True(cellsPerState >= 10 && cellsPerState <= 1000,
                $"Average {cellsPerState:F1} cells per state (expected 10-1000)");

            // Check state size distribution (no state should be tiny or huge)
            var stateSizes = map.States.ToDictionary(
                s => s.Id,
                s => map.Cells.Count(c => c.State == s.Id));

            var minStateSize = stateSizes.Values.Min();
            var maxStateSize = stateSizes.Values.Max();
            var ratio = (double)maxStateSize / minStateSize;

            Assert.True(ratio <= 10,
                $"State size ratio is {ratio:F1} (largest/smallest, expected <= 10)");
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Burgs_HaveReasonableDistribution(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        var landCells = map.Cells.Where(c => c.IsLand).ToList();
        if (landCells.Count == 0) return;

        // Should have reasonable number of burgs
        var expectedMaxBurgs = Math.Max(1, landCells.Count / 50); // ~1 burg per 50 land cells
        Assert.True(map.Burgs.Count >= 0 && map.Burgs.Count <= expectedMaxBurgs,
            $"Map has {map.Burgs.Count} burgs (expected 0-{expectedMaxBurgs})");

        if (map.Burgs.Count > 0)
        {
            // Should have at least one capital
            var capitals = map.Burgs.Count(b => b.IsCapital);
            Assert.True(capitals >= 1,
                $"Map has {capitals} capitals (expected >= 1)");

            // Most burgs should not be capitals
            var capitalPercent = (double)capitals / map.Burgs.Count * 100;
            Assert.True(capitalPercent <= 50,
                $"{capitalPercent:F1}% of burgs are capitals (expected <= 50%)");
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void TemperatureAndPrecipitation_HaveReasonableValues(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        var landCells = map.Cells.Where(c => c.IsLand).ToList();
        if (landCells.Count == 0) return;

        // Temperature should be in reasonable range
        var temperatures = landCells.Select(c => c.Temperature).ToList();
        var minTemp = temperatures.Min();
        var maxTemp = temperatures.Max();
        var avgTemp = temperatures.Average();

        Assert.True(minTemp >= -50 && minTemp <= 60,
            $"Minimum temperature is {minTemp:F1}°C (expected -50 to 60°C)");
        Assert.True(maxTemp >= -50 && maxTemp <= 60,
            $"Maximum temperature is {maxTemp:F1}°C (expected -50 to 60°C)");
        Assert.True(avgTemp >= -20 && avgTemp <= 40,
            $"Average temperature is {avgTemp:F1}°C (expected -20 to 40°C)");

        // Precipitation should be in reasonable range
        var precipitations = landCells.Select(c => c.Precipitation).ToList();
        var minPrecip = precipitations.Min();
        var maxPrecip = precipitations.Max();
        var avgPrecip = precipitations.Average();

        Assert.True(minPrecip >= 0 && minPrecip <= 500,
            $"Minimum precipitation is {minPrecip:F1}mm (expected 0-500mm)");
        Assert.True(maxPrecip >= 0 && maxPrecip <= 500,
            $"Maximum precipitation is {maxPrecip:F1}mm (expected 0-500mm)");
        Assert.True(avgPrecip >= 50 && avgPrecip <= 300,
            $"Average precipitation is {avgPrecip:F1}mm (expected 50-300mm)");
    }

    /// <summary>
    /// Helper method to generate a map for testing
    /// </summary>
    private static MapData GenerateMap(long seed, int numPoints)
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
        return generator.Generate(settings);
    }
}
