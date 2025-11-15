namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class CulturesGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);

        // Create test cells with population
        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point(
                (i % 40) * 20 + 10,
                (i / 40) * 24 + 12
            ));

            // Make most cells land with population
            cell.Height = 50;
            cell.Population = 10 + i % 50;
            cell.Biome = i % 10;
            cell.Feature = 0;

            map.Cells.Add(cell);
        }

        // Add some biomes
        for (int i = 0; i < 15; i++)
        {
            map.Biomes.Add(new Biome { Id = i, Name = $"Biome{i}", Cost = 50 });
        }

        // Add a feature
        map.Features.Add(new Feature { Id = 0, Cells = new List<int>() });

        return map;
    }

    [Fact]
    public void Generate_ShouldCreateCultures()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        Assert.NotNull(cultures);
        Assert.True(cultures.Count > 10); // At least 10 cultures + wildlands
        Assert.Equal("Wildlands", cultures[0].Name);
    }

    [Fact]
    public void Generate_ShouldAssignCenters()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.True(culture.CenterCellId >= 0);
        }
    }

    [Fact]
    public void Generate_ShouldExpandTerritories()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        var cellsWithCulture = map.Cells.Count(c => c.CultureId > 0);
        Assert.True(cellsWithCulture > 100); // Should have expanded beyond centers
    }

    [Fact]
    public void Generate_ShouldCalculateStatistics()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.True(culture.CellCount > 0);
            Assert.True(culture.Area > 0);
        }
    }

    [Fact]
    public void Generate_ShouldAssignCultureTypes()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.True(Enum.IsDefined(typeof(CultureType), culture.Type));
        }
    }

    [Fact]
    public void Generate_ShouldAssignColors()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.False(string.IsNullOrEmpty(culture.Color));
            Assert.StartsWith("#", culture.Color);
        }
    }

    [Fact]
    public void Generate_ShouldAssignCodes()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.False(string.IsNullOrEmpty(culture.Code));
            Assert.True(culture.Code.Length >= 3);
        }
    }

    [Fact]
    public void Generate_ShouldRespectExpansionism()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European",
            SizeVariety = 2.0
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.True(culture.Expansionism > 0);
        }
    }

    [Fact]
    public void Generate_ShouldUseDifferentCultureSets()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "Oriental"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        Assert.NotNull(cultures);
        Assert.True(cultures.Count > 1);
    }

    [Fact]
    public void Generate_ShouldAssignNameBases()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            CultureCount = 10,
            CultureSet = "European"
        };
        var generator = new CulturesGenerator(map, random, settings);

        // Act
        var cultures = generator.Generate();

        // Assert
        foreach (var culture in cultures.Where(c => c.Id > 0))
        {
            Assert.True(culture.NameBaseId >= 0);
        }
    }
}
