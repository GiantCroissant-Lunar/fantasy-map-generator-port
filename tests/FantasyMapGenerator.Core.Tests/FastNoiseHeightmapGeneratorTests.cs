using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Unit tests for FastNoiseHeightmapGenerator
/// </summary>
public class FastNoiseHeightmapGeneratorTests
{
    private MapData CreateTestMapData(int width = 100, int height = 100, int numPoints = 100)
    {
        var mapData = new MapData(width, height, numPoints);
        var rng = new SystemRandomSource(42);
        
        // Generate test points using appropriate minDistance for desired point count
        // For a 100x100 area with ~100 points, minDistance should be around 10
        var minDistance = Math.Sqrt((width * height) / (numPoints * Math.PI)) * 0.8;
        var points = GeometryUtils.GeneratePoissonDiskPoints(width, height, minDistance, rng);
        mapData.Points = points;
        
        // Create simple cells
        mapData.Cells = new List<Cell>();
        for (int i = 0; i < points.Count; i++)
        {
            var cell = new Cell(i, points[i])
            {
                Neighbors = new List<int>()
            };
            mapData.Cells.Add(cell);
        }
        
        return mapData;
    }

    [Fact]
    public void Constructor_WithValidSeed_DoesNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() => new FastNoiseHeightmapGenerator(12345));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Generate_WithSameSeed_ProducesIdenticalHeightmaps()
    {
        // Arrange
        int seed = 12345;
        var mapData = CreateTestMapData();
        var generator1 = new FastNoiseHeightmapGenerator(seed);
        var generator2 = new FastNoiseHeightmapGenerator(seed);

        // Act
        var heightmap1 = generator1.Generate(mapData, HeightmapProfile.Default);
        var heightmap2 = generator2.Generate(mapData, HeightmapProfile.Default);

        // Assert
        Assert.Equal(heightmap1, heightmap2);
    }

    [Fact]
    public void Generate_WithDifferentSeeds_ProduceDifferentHeightmaps()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator1 = new FastNoiseHeightmapGenerator(111);
        var generator2 = new FastNoiseHeightmapGenerator(222);

        // Act
        var heightmap1 = generator1.Generate(mapData, HeightmapProfile.Default);
        var heightmap2 = generator2.Generate(mapData, HeightmapProfile.Default);

        // Assert
        Assert.NotEqual(heightmap1, heightmap2);
    }

    [Theory]
    [InlineData(HeightmapProfile.Island)]
    [InlineData(HeightmapProfile.Continents)]
    [InlineData(HeightmapProfile.Archipelago)]
    [InlineData(HeightmapProfile.Pangea)]
    [InlineData(HeightmapProfile.Mediterranean)]
    public void Generate_WithDifferentProfiles_ProducesValidHeightmaps(HeightmapProfile profile)
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);

        // Act
        var heightmap = generator.Generate(mapData, profile);

        // Assert
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
        Assert.All(heightmap, h => Assert.InRange(h, (byte)0, (byte)100));
    }

    [Fact]
    public void Generate_IslandProfile_HasCoastalFalloff()
    {
        // Arrange
        var mapData = CreateTestMapData(100, 100, 200);
        var generator = new FastNoiseHeightmapGenerator(42);

        // Act
        var heightmap = generator.Generate(mapData, HeightmapProfile.Island);

        // Assert
        // Check that cells closer to center tend to be higher than cells closer to edges
        var centerX = mapData.Width / 2.0;
        var centerY = mapData.Height / 2.0;
        var maxDistance = Math.Sqrt(centerX * centerX + centerY * centerY);
        
        var cellHeights = mapData.Cells.Select(c => new
        {
            Height = (double)heightmap[c.Id],
            DistanceFromCenter = Math.Sqrt(Math.Pow(c.Center.X - centerX, 2) + Math.Pow(c.Center.Y - centerY, 2)),
            RelativeDistance = Math.Sqrt(Math.Pow(c.Center.X - centerX, 2) + Math.Pow(c.Center.Y - centerY, 2)) / maxDistance
        }).ToList();

        // Group cells by relative distance from center (0 = center, 1 = corner)
        var innerCells = cellHeights.Where(ch => ch.RelativeDistance < 0.3).Select(ch => ch.Height).ToList();
        var outerCells = cellHeights.Where(ch => ch.RelativeDistance > 0.7).Select(ch => ch.Height).ToList();

        // Ensure we have cells in both regions
        Assert.True(innerCells.Count > 0, "No cells found in inner region");
        Assert.True(outerCells.Count > 0, "No cells found in outer region");

        var innerAvg = innerCells.Average();
        var outerAvg = outerCells.Average();

        Assert.True(innerAvg > outerAvg, $"Inner average: {innerAvg}, Outer average: {outerAvg}");
    }

    [Fact]
    public void Generate_WithCustomSettings_ProducesValidHeightmaps()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 4,
            Frequency = 0.8f,
            DomainWarpStrength = 0.0f,
            HeightmapTemplate = "island"
        };

        // Act
        var heightmap = generator.Generate(mapData, settings);

        // Assert
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
        Assert.All(heightmap, h => Assert.InRange(h, (byte)0, (byte)100));
    }

    [Theory]
    [InlineData("OpenSimplex2")]
    [InlineData("Perlin")]
    [InlineData("Value")]
    [InlineData("Cellular")]
    public void Generate_WithDifferentNoiseTypes_ProducesValidHeightmaps(string noiseType)
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = noiseType,
            FractalType = "FBm",
            Octaves = 3,
            Frequency = 1.0f
        };

        // Act
        var heightmap = generator.Generate(mapData, settings);

        // Assert
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
        Assert.All(heightmap, h => Assert.InRange(h, (byte)0, (byte)100));
    }

    [Theory]
    [InlineData("FBm")]
    [InlineData("Ridged")]
    [InlineData("PingPong")]
    public void Generate_WithDifferentFractalTypes_ProducesValidHeightmaps(string fractalType)
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = fractalType,
            Octaves = 3,
            Frequency = 1.0f
        };

        // Act
        var heightmap = generator.Generate(mapData, settings);

        // Assert
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
        Assert.All(heightmap, h => Assert.InRange(h, (byte)0, (byte)100));
    }

    [Fact]
    public void Generate_WithDomainWarping_ProducesValidHeightmaps()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 3,
            Frequency = 1.0f,
            DomainWarpStrength = 30.0f
        };

        // Act
        var heightmap = generator.Generate(mapData, settings);

        // Assert
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
        Assert.All(heightmap, h => Assert.InRange(h, (byte)0, (byte)100));
    }

    [Fact]
    public void Generate_WithInvalidNoiseType_UsesDefault()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "InvalidNoiseType",
            FractalType = "FBm",
            Octaves = 3,
            Frequency = 1.0f
        };

        // Act & Assert - Should not throw
        var heightmap = generator.Generate(mapData, settings);
        
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
    }

    [Fact]
    public void Generate_WithInvalidFractalType_UsesDefault()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        var settings = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "InvalidFractalType",
            Octaves = 3,
            Frequency = 1.0f
        };

        // Act & Assert - Should not throw
        var heightmap = generator.Generate(mapData, settings);
        
        Assert.NotNull(heightmap);
        Assert.Equal(mapData.Cells.Count, heightmap.Length);
    }

    [Fact]
    public void Generate_ArchipelagoProfile_HasScatteredIslands()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);

        // Act
        var heightmap = generator.Generate(mapData, HeightmapProfile.Archipelago);

        // Assert
        // Archipelago should have a significant amount of ocean (lower values)
        var landCount = heightmap.Count(h => h > 30);
        var oceanCount = heightmap.Count(h => h <= 30);
        var oceanPercentage = (double)oceanCount / heightmap.Length * 100;
        
        // Should have at least 40% ocean in archipelago (more realistic expectation)
        Assert.True(oceanPercentage >= 40.0, $"Ocean percentage: {oceanPercentage:F1}%, should be at least 40%");
    }

    [Fact]
    public void Generate_PangeaProfile_HasLargeLandmass()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);

        // Act
        var heightmap = generator.Generate(mapData, HeightmapProfile.Pangea);

        // Assert
        // Pangea should have more land than ocean
        var landCount = heightmap.Count(h => h > 30);
        var oceanCount = heightmap.Count(h => h <= 30);
        
        // Should have more land than ocean in pangea
        Assert.True(landCount > oceanCount, $"Land: {landCount}, Ocean: {oceanCount}");
    }

    [Fact]
    public void Generate_VaryingOctaves_ProducesDifferentDetailLevels()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        
        var settingsLowOctaves = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 1,
            Frequency = 1.0f
        };
        
        var settingsHighOctaves = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 6,
            Frequency = 1.0f
        };

        // Act
        var heightmapLow = generator.Generate(mapData, settingsLowOctaves);
        var heightmapHigh = generator.Generate(mapData, settingsHighOctaves);

        // Assert
        // Higher octaves should produce more variation (different from lower octaves)
        Assert.NotEqual(heightmapLow, heightmapHigh);
    }

    [Fact]
    public void Generate_VaryingFrequency_ProducesDifferentFeatureSizes()
    {
        // Arrange
        var mapData = CreateTestMapData();
        var generator = new FastNoiseHeightmapGenerator(42);
        
        var settingsLowFreq = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 3,
            Frequency = 0.2f // Low frequency = larger features
        };
        
        var settingsHighFreq = new MapGenerationSettings
        {
            Seed = 42,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 3,
            Frequency = 3.0f // High frequency = smaller features
        };

        // Act
        var heightmapLowFreq = generator.Generate(mapData, settingsLowFreq);
        var heightmapHighFreq = generator.Generate(mapData, settingsHighFreq);

        // Assert
        // Different frequencies should produce different patterns
        Assert.NotEqual(heightmapLowFreq, heightmapHighFreq);
    }
}
