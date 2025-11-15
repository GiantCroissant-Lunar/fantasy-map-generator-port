namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class StatesGeneratorTests
{
    private (MapData map, List<Burg> burgs) CreateTestMapWithBurgs()
    {
        var map = new MapData(800, 600, 1000);

        // Create test cells
        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point(
                (i % 40) * 20 + 10,
                (i / 40) * 24 + 12
            ));

            // Make most cells land with population
            cell.Height = 50;
            cell.Population = 10 + i % 50;
            cell.Culture = (i % 5) + 1;
            cell.Biome = i % 10;
            cell.Feature = 0;

            map.Cells.Add(cell);
        }

        // Create burgs with capitals
        var burgs = new List<Burg> { null! }; // Index 0 reserved
        for (int i = 1; i <= 10; i++)
        {
            var burg = new Burg
            {
                Id = i,
                Name = $"Capital{i}",
                CellId = i * 100,
                Position = map.Cells[i * 100].Center,
                IsCapital = true,
                CultureId = (i % 5) + 1,
                Population = 50 + i * 10
            };
            burgs.Add(burg);
            map.Cells[i * 100].Burg = i;
        }

        map.Burgs = burgs;
        return (map, burgs);
    }

    [Fact]
    public void Generate_ShouldCreateStates()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        Assert.NotNull(states);
        Assert.True(states.Count > 10); // At least 10 states + neutral
        Assert.Equal("Neutrals", states[0].Name);
    }

    [Fact]
    public void Generate_ShouldAssignCapitals()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        foreach (var state in states.Where(s => s.Id > 0))
        {
            Assert.True(state.CapitalBurgId > 0);
            Assert.True(state.CenterCellId >= 0);
        }
    }

    [Fact]
    public void Generate_ShouldExpandTerritories()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        var cellsWithStates = map.Cells.Count(c => c.StateId > 0);
        Assert.True(cellsWithStates > 100); // Should have expanded beyond capitals
    }

    [Fact]
    public void Generate_ShouldCalculateStatistics()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        foreach (var state in states.Where(s => s.Id > 0))
        {
            Assert.True(state.CellCount > 0);
            Assert.True(state.Area > 0);
        }
    }

    [Fact]
    public void Generate_ShouldFindNeighbors()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        var statesWithNeighbors = states.Where(s => s.Id > 0 && s.Neighbors.Count > 0).Count();
        Assert.True(statesWithNeighbors > 0);
    }

    [Fact]
    public void Generate_ShouldGenerateDiplomacy()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        foreach (var state in states.Where(s => s.Id > 0))
        {
            Assert.NotEmpty(state.Diplomacy);
        }
    }

    [Fact]
    public void Generate_ShouldAssignStateForms()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        foreach (var state in states.Where(s => s.Id > 0))
        {
            Assert.True(Enum.IsDefined(typeof(StateForm), state.Form));
            Assert.False(string.IsNullOrEmpty(state.FullName));
        }
    }

    [Fact]
    public void Generate_ShouldAssignColors()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings();
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        foreach (var state in states.Where(s => s.Id > 0))
        {
            Assert.NotEqual(default, state.Color);
        }
    }

    [Fact]
    public void Generate_ShouldRespectExpansionism()
    {
        // Arrange
        var (map, burgs) = CreateTestMapWithBurgs();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { SizeVariety = 2.0 };
        var generator = new StatesGenerator(map, random, settings);

        // Act
        var states = generator.Generate(burgs);

        // Assert
        foreach (var state in states.Where(s => s.Id > 0))
        {
            Assert.True(state.Expansionism > 0);
        }
    }
}
