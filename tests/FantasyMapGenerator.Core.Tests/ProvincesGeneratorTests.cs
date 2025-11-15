namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class ProvincesGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);

        // Create cells
        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point((i % 40) * 20 + 10, (i / 40) * 24 + 12));
            cell.Height = 50;
            cell.Population = 10 + i % 50;
            cell.StateId = (i % 10) + 1;
            map.Cells.Add(cell);
        }

        // Create states
        for (int i = 1; i <= 10; i++)
        {
            map.States.Add(new State
            {
                Id = i,
                Name = $"State{i}",
                Color = $"#{i * 20:X2}{i * 15:X2}{i * 10:X2}",
                Area = 1000
            });
        }

        // Create burgs
        for (int i = 0; i < 30; i++)
        {
            var cellId = i * 30;
            var burg = new Burg
            {
                Id = i,
                Name = $"Burg{i}",
                CellId = cellId,
                StateId = (cellId % 10) + 1,
                Population = 1000 + i * 100,
                Position = map.Cells[cellId].Center
            };
            map.Burgs.Add(burg);
            map.Cells[cellId].Burg = i;
        }

        return map;
    }

    [Fact]
    public void Generate_ShouldCreateProvinces()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateProvinces = true };
        var generator = new ProvincesGenerator(map, random, settings);

        var provinces = generator.Generate();

        Assert.NotNull(provinces);
        Assert.True(provinces.Count > 1); // At least 1 + no province
        Assert.Equal("No Province", provinces[0].Name);
    }

    [Fact]
    public void Generate_ShouldAssignCapitals()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateProvinces = true };
        var generator = new ProvincesGenerator(map, random, settings);

        var provinces = generator.Generate();

        foreach (var province in provinces.Where(p => p.Id > 0))
        {
            Assert.True(province.CapitalBurgId >= 0);
            Assert.True(province.CenterCellId >= 0);
        }
    }

    [Fact]
    public void Generate_ShouldExpandWithinStates()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateProvinces = true };
        var generator = new ProvincesGenerator(map, random, settings);

        var provinces = generator.Generate();

        // Check that provinces don't cross state borders
        foreach (var province in provinces.Where(p => p.Id > 0))
        {
            var provinceCells = map.Cells.Where(c => c.ProvinceId == province.Id).ToList();
            if (provinceCells.Any())
            {
                var stateIds = provinceCells.Select(c => c.StateId).Distinct().ToList();
                Assert.Single(stateIds); // All cells in same state
                Assert.Equal(province.StateId, stateIds[0]);
            }
        }
    }

    [Fact]
    public void Generate_ShouldCalculateStatistics()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateProvinces = true };
        var generator = new ProvincesGenerator(map, random, settings);

        var provinces = generator.Generate();

        foreach (var province in provinces.Where(p => p.Id > 0))
        {
            if (province.CellCount > 0)
            {
                Assert.True(province.Area > 0);
                Assert.True(province.RuralPopulation >= 0);
            }
        }
    }

    [Fact]
    public void Generate_ShouldAssignColors()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateProvinces = true };
        var generator = new ProvincesGenerator(map, random, settings);

        var provinces = generator.Generate();

        foreach (var province in provinces.Where(p => p.Id > 0))
        {
            Assert.NotNull(province.Color);
            Assert.StartsWith("#", province.Color);
        }
    }

    [Fact]
    public void Generate_ShouldRespectMinBurgsPerProvince()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings 
        { 
            GenerateProvinces = true,
            MinBurgsPerProvince = 5
        };
        var generator = new ProvincesGenerator(map, random, settings);

        var provinces = generator.Generate();

        // Each state should have reasonable number of provinces
        foreach (var state in map.States.Where(s => s.Id > 0))
        {
            var stateProvinces = provinces.Where(p => p.StateId == state.Id).ToList();
            var stateBurgs = map.Burgs.Count(b => b != null && b.StateId == state.Id);
            
            if (stateBurgs > 0)
            {
                Assert.True(stateProvinces.Count > 0);
                Assert.True(stateProvinces.Count <= stateBurgs);
            }
        }
    }
}
