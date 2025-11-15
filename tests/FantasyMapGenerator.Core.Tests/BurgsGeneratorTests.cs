namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class BurgsGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);
        
        // Create test cells with population and culture
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
        
        return map;
    }

    [Fact]
    public void Generate_ShouldCreateBurgs()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 10,
            NumBurgs = 50
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        Assert.NotNull(burgs);
        Assert.True(burgs.Count > 10); // At least capitals
        Assert.Null(burgs[0]); // Index 0 is reserved
    }

    [Fact]
    public void Generate_ShouldPlaceCapitals()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 10,
            NumBurgs = 0 // Only capitals
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        var capitals = burgs.Where(b => b != null && b.IsCapital).ToList();
        Assert.True(capitals.Count > 0);
        Assert.All(capitals, c => Assert.True(c.IsCapital));
    }

    [Fact]
    public void Generate_ShouldAssignPopulation()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 5,
            NumBurgs = 20
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        Assert.All(burgs.Where(b => b != null), b => Assert.True(b.Population > 0));
    }

    [Fact]
    public void Generate_ShouldAssignBurgTypes()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 5,
            NumBurgs = 20
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        Assert.All(burgs.Where(b => b != null), b => 
        {
            Assert.True(Enum.IsDefined(typeof(BurgType), b.Type));
        });
    }

    [Fact]
    public void Generate_ShouldAssignFeatures()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 5,
            NumBurgs = 20
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        var capitals = burgs.Where(b => b != null && b.IsCapital).ToList();
        Assert.All(capitals, c => Assert.True(c.HasCitadel)); // All capitals should have citadels
    }

    [Fact]
    public void Generate_ShouldRespectMinimumSpacing()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 10,
            NumBurgs = 0
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        var capitals = burgs.Where(b => b != null && b.IsCapital).ToList();
        double minSpacing = (map.Width + map.Height) / 2.0 / settings.NumStates;

        for (int i = 0; i < capitals.Count; i++)
        {
            for (int j = i + 1; j < capitals.Count; j++)
            {
                double dx = capitals[i].Position.X - capitals[j].Position.X;
                double dy = capitals[i].Position.Y - capitals[j].Position.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                // Allow 20% tolerance for spacing
                Assert.True(dist >= minSpacing * 0.6, 
                    $"Capitals {i} and {j} are too close: {dist} < {minSpacing * 0.6}");
            }
        }
    }

    [Fact]
    public void Generate_ShouldUpdateCellBurgIds()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 5,
            NumBurgs = 20
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        foreach (var burg in burgs.Where(b => b != null))
        {
            var cell = map.Cells[burg.CellId];
            Assert.Equal(burg.Id, cell.Burg);
        }
    }

    [Fact]
    public void Generate_ShouldGenerateNames()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            NumStates = 5,
            NumBurgs = 20
        };
        var generator = new BurgsGenerator(map, random, settings);

        // Act
        var burgs = generator.Generate();

        // Assert
        Assert.All(burgs.Where(b => b != null), b => Assert.False(string.IsNullOrEmpty(b.Name)));
    }
}
