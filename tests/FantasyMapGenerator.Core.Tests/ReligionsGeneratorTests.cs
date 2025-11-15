namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class ReligionsGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);

        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point((i % 40) * 20 + 10, (i / 40) * 24 + 12));
            cell.Height = 50;
            cell.Population = 10 + i % 50;
            cell.CultureId = (i % 5) + 1;
            cell.StateId = (i % 10) + 1;
            map.Cells.Add(cell);
        }

        return map;
    }

    [Fact]
    public void Generate_ShouldCreateReligions()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { ReligionCount = 5 };
        var generator = new ReligionsGenerator(map, random, settings);

        var religions = generator.Generate();

        Assert.NotNull(religions);
        Assert.Equal(6, religions.Count); // 5 + no religion
        Assert.Equal("No Religion", religions[0].Name);
    }

    [Fact]
    public void Generate_ShouldAssignTypes()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { ReligionCount = 5 };
        var generator = new ReligionsGenerator(map, random, settings);

        var religions = generator.Generate();

        foreach (var religion in religions.Where(r => r.Id > 0))
        {
            Assert.True(Enum.IsDefined(typeof(ReligionType), religion.Type));
            Assert.True(Enum.IsDefined(typeof(ExpansionType), religion.Expansion));
        }
    }

    [Fact]
    public void Generate_ShouldGenerateDeities()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { ReligionCount = 5 };
        var generator = new ReligionsGenerator(map, random, settings);

        var religions = generator.Generate();

        foreach (var religion in religions.Where(r => r.Id > 0))
        {
            Assert.NotEmpty(religion.Deities);
        }
    }

    [Fact]
    public void Generate_ShouldPlaceOrigins()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { ReligionCount = 5 };
        var generator = new ReligionsGenerator(map, random, settings);

        var religions = generator.Generate();

        foreach (var religion in religions.Where(r => r.Id > 0))
        {
            Assert.True(religion.CenterCellId >= 0);
        }
    }

    [Fact]
    public void Generate_ShouldExpandReligions()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { ReligionCount = 5 };
        var generator = new ReligionsGenerator(map, random, settings);

        var religions = generator.Generate();

        var cellsWithReligion = map.Cells.Count(c => c.ReligionId > 0);
        Assert.True(cellsWithReligion > 5);
    }

    [Fact]
    public void Generate_ShouldCalculateStatistics()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { ReligionCount = 5 };
        var generator = new ReligionsGenerator(map, random, settings);

        var religions = generator.Generate();

        foreach (var religion in religions.Where(r => r.Id > 0))
        {
            if (religion.CellCount > 0)
            {
                Assert.True(religion.Area > 0);
                Assert.True(religion.RuralPopulation > 0);
            }
        }
    }
}
