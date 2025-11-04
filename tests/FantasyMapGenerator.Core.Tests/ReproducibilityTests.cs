using Xunit;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using FantasyMapGenerator.Core.Geometry;

namespace FantasyMapGenerator.Core.Tests;

public class ReproducibilityTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalMaps_PCG()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 1000,
            Height = 1000,
            NumPoints = 1000,
            RandomAlgorithm = "PCG"
        };

        var generator = new MapGenerator();

        var map1 = generator.Generate(settings);
        var map2 = generator.Generate(settings);

        // Compare cell properties
        Assert.Equal(map1.Cells.Count, map2.Cells.Count);

        for (int i = 0; i < map1.Cells.Count; i++)
        {
            Assert.Equal(map1.Cells[i].Height, map2.Cells[i].Height);
            Assert.Equal(map1.Cells[i].Biome, map2.Cells[i].Biome);
            Assert.Equal(map1.Cells[i].Temperature, map2.Cells[i].Temperature);
            Assert.Equal(map1.Cells[i].Precipitation, map2.Cells[i].Precipitation);
            Assert.Equal(map1.Cells[i].State, map2.Cells[i].State);
        }
    }

    [Fact]
    public void SameSeed_ProducesIdenticalMaps_System()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 1000,
            Height = 1000,
            NumPoints = 1000,
            RandomAlgorithm = "System"
        };

        var generator = new MapGenerator();

        var map1 = generator.Generate(settings);
        var map2 = generator.Generate(settings);

        // Compare cell properties
        Assert.Equal(map1.Cells.Count, map2.Cells.Count);

        for (int i = 0; i < map1.Cells.Count; i++)
        {
            Assert.Equal(map1.Cells[i].Height, map2.Cells[i].Height);
            Assert.Equal(map1.Cells[i].Biome, map2.Cells[i].Biome);
            Assert.Equal(map1.Cells[i].Temperature, map2.Cells[i].Temperature);
            Assert.Equal(map1.Cells[i].Precipitation, map2.Cells[i].Precipitation);
            Assert.Equal(map1.Cells[i].State, map2.Cells[i].State);
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentMaps()
    {
        var settings1 = new MapGenerationSettings { Seed = 111, RandomAlgorithm = "PCG" };
        var settings2 = new MapGenerationSettings { Seed = 222, RandomAlgorithm = "PCG" };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // Maps should differ
        bool hasDifference = false;

        for (int i = 0; i < Math.Min(map1.Cells.Count, map2.Cells.Count); i++)
        {
            if (map1.Cells[i].Height != map2.Cells[i].Height ||
                map1.Cells[i].Biome != map2.Cells[i].Biome)
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference, "Different seeds should produce different maps");
    }

    [Fact]
    public void PcgRng_IsDeterministic()
    {
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(42);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(rng1.Next(), rng2.Next());
        }
    }

    [Fact]
    public void PcgRng_ProducesDifferentSequences()
    {
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(43);

        bool hasDifference = false;

        for (int i = 0; i < 100; i++)
        {
            if (rng1.Next() != rng2.Next())
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference);
    }

    [Theory]
    [InlineData(-9223372036854775808L)] // long.MinValue
    [InlineData(0L)]
    [InlineData(9223372036854775807L)]  // long.MaxValue
    public void PcgRng_HandlesFull64BitSeeds(long seed)
    {
        var rng = new PcgRandomSource(seed);

        // Should not crash
        for (int i = 0; i < 100; i++)
        {
            rng.Next();
        }
    }

    [Fact]
    public void SystemRandomSource_IsDeterministic()
    {
        var rng1 = new SystemRandomSource(42);
        var rng2 = new SystemRandomSource(42);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(rng1.Next(), rng2.Next());
        }
    }

    [Fact]
    public void SystemRandomSource_ProducesDifferentSequences()
    {
        var rng1 = new SystemRandomSource(42);
        var rng2 = new SystemRandomSource(43);

        bool hasDifference = false;

        for (int i = 0; i < 100; i++)
        {
            if (rng1.Next() != rng2.Next())
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference);
    }

    [Fact]
    public void MapGenerationSettings_CreatesCorrectRng()
    {
        var pcgSettings = new MapGenerationSettings { Seed = 123, RandomAlgorithm = "PCG" };
        var systemSettings = new MapGenerationSettings { Seed = 123, RandomAlgorithm = "System" };
        var defaultSettings = new MapGenerationSettings { Seed = 123 }; // Should default to PCG

        var pcgRng = pcgSettings.CreateRandom();
        var systemRng = systemSettings.CreateRandom();
        var defaultRng = defaultSettings.CreateRandom();

        Assert.IsType<PcgRandomSource>(pcgRng);
        Assert.IsType<SystemRandomSource>(systemRng);
        Assert.IsType<PcgRandomSource>(defaultRng);
    }

    [Fact]
    public void PcgChildRng_IsIndependent()
    {
        var parentRng = new PcgRandomSource(42);
        var child1 = parentRng.CreateChild(1);
        var child2 = parentRng.CreateChild(2);

        // Children should produce different sequences
        bool hasDifference = false;
        for (int i = 0; i < 100; i++)
        {
            if (child1.Next() != child2.Next())
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference);
    }

    [Fact]
    public void GeometryUtils_PoissonDisk_Reproducible()
    {
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(42);

        var points1 = GeometryUtils.GeneratePoissonDiskPoints(100, 100, 10, rng1);
        var points2 = GeometryUtils.GeneratePoissonDiskPoints(100, 100, 10, rng2);

        Assert.Equal(points1.Count, points2.Count);
        for (int i = 0; i < points1.Count; i++)
        {
            Assert.Equal(points1[i].X, points2[i].X);
            Assert.Equal(points1[i].Y, points2[i].Y);
        }
    }

    [Fact]
    public void HeightmapGenerator_FromNoise_Reproducible()
    {
        var mapData = new MapData(100, 100, 100);
        
        // Create some test points and cells so the heightmap generator works
        var points = new List<Point>();
        for (int i = 0; i < 100; i++)
        {
            points.Add(new Point(
                System.Random.Shared.Next(0, 100),
                System.Random.Shared.Next(0, 100)
            ));
        }
        mapData.Points = points;
        
        // Create simple cells
        mapData.Cells = new List<Cell>();
        for (int i = 0; i < points.Count; i++)
        {
            mapData.Cells.Add(new Cell(i, points[i]));
        }
        
        var generator1 = new HeightmapGenerator(mapData);
        var generator2 = new HeightmapGenerator(mapData);
        
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(42);

        var heights1 = generator1.FromNoise(rng1);
        var heights2 = generator2.FromNoise(rng2);

        Assert.Equal(heights1.Length, heights2.Length);
        for (int i = 0; i < heights1.Length; i++)
        {
            Assert.Equal(heights1[i], heights2[i]);
        }
    }

    [Fact]
    public void BiomeGenerator_GenerateBiomes_Reproducible()
    {
        var mapData = new MapData(100, 100, 100);
        var generator = new BiomeGenerator(mapData);
        
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(42);

        // Initialize cells with some test data
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            mapData.Cells[i].Height = 50;
            mapData.Cells[i].IsBorder = false;
        }

        generator.GenerateBiomes(rng1);
        var biomes1 = mapData.Cells.Select(c => c.Biome).ToArray();

        // Reset and generate again
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            mapData.Cells[i].Biome = -1;
            mapData.Cells[i].Temperature = 0;
            mapData.Cells[i].Precipitation = 0;
        }

        generator.GenerateBiomes(rng2);
        var biomes2 = mapData.Cells.Select(c => c.Biome).ToArray();

        Assert.Equal(biomes1, biomes2);
    }
}