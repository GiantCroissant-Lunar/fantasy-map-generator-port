namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class MilitaryGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);

        // Create cells
        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point((i % 40) * 20 + 10, (i / 40) * 24 + 12));
            cell.Height = 50;
            cell.Population = 10;
            cell.StateId = (i % 5) + 1;
            map.Cells.Add(cell);
        }

        // Add neighbors
        for (int i = 0; i < 1000; i++)
        {
            var cell = map.Cells[i];
            int x = i % 40;
            int y = i / 40;

            if (x > 0) cell.Neighbors.Add(i - 1);
            if (x < 39) cell.Neighbors.Add(i + 1);
            if (y > 0) cell.Neighbors.Add(i - 40);
            if (y < 24) cell.Neighbors.Add(i + 40);
        }

        // Create states
        for (int i = 1; i <= 5; i++)
        {
            map.States.Add(new State
            {
                Id = i,
                Name = $"State{i}",
                CapitalBurgId = i - 1,
                RuralPopulation = 10000,
                UrbanPopulation = 5000,
                BurgCount = 3,
                Type = StateType.Generic
            });
        }

        // Create burgs
        for (int i = 0; i < 15; i++)
        {
            var cellId = i * 60 + 20;
            var burg = new Burg
            {
                Id = i,
                Name = $"Burg{i}",
                CellId = cellId,
                StateId = (i % 5) + 1,
                Population = 1000 + i * 100,
                Position = map.Cells[cellId].Center,
                IsCapital = (i % 5 == 0),
                IsPort = (i % 7 == 0)
            };
            map.Burgs.Add(burg);
            map.Cells[cellId].Burg = i;
        }

        return map;
    }

    [Fact]
    public void Generate_ShouldCreateMilitaryUnits()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMilitary = true };
        var generator = new MilitaryGenerator(map, random, settings);

        var units = generator.Generate();

        Assert.NotNull(units);
        Assert.NotEmpty(units);
    }

    [Fact]
    public void Generate_ShouldPlaceGarrisons()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMilitary = true };
        var generator = new MilitaryGenerator(map, random, settings);

        var units = generator.Generate();

        var garrisons = units.Where(u => u.Status == UnitStatus.Garrison).ToList();
        Assert.NotEmpty(garrisons);

        // Garrisons should be at burgs
        foreach (var garrison in garrisons)
        {
            Assert.True(garrison.GarrisonBurgId.HasValue);
        }
    }

    [Fact]
    public void Generate_ShouldPlaceFieldArmies()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMilitary = true };
        var generator = new MilitaryGenerator(map, random, settings);

        var units = generator.Generate();

        var fieldArmies = units.Where(u => u.Status == UnitStatus.Field).ToList();
        Assert.NotEmpty(fieldArmies);
    }

    [Fact]
    public void Generate_ShouldPlaceNavies()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMilitary = true };
        var generator = new MilitaryGenerator(map, random, settings);

        var units = generator.Generate();

        var navies = units.Where(u => u.Type == UnitType.Navy).ToList();
        
        // Should have navies if there are ports
        var ports = map.Burgs.Count(b => b != null && b.IsPort);
        if (ports > 0)
        {
            Assert.NotEmpty(navies);
        }
    }

    [Fact]
    public void Generate_ShouldAssignStrength()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMilitary = true };
        var generator = new MilitaryGenerator(map, random, settings);

        var units = generator.Generate();

        foreach (var unit in units)
        {
            Assert.True(unit.Strength > 0);
        }
    }

    [Fact]
    public void Generate_ShouldAssignUnitTypes()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMilitary = true };
        var generator = new MilitaryGenerator(map, random, settings);

        var units = generator.Generate();

        foreach (var unit in units)
        {
            Assert.True(Enum.IsDefined(typeof(UnitType), unit.Type));
        }
    }
}
