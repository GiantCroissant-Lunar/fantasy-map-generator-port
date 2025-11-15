namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class MarkersGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);

        // Create cells with varied terrain
        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point((i % 40) * 20 + 10, (i / 40) * 24 + 12));
            
            // Varied heights: mountains, hills, plains
            if (i < 100)
                cell.Height = 75; // Mountains
            else if (i < 300)
                cell.Height = 50; // Hills
            else if (i < 900)
                cell.Height = 30; // Plains
            else
                cell.Height = 10; // Water

            cell.Population = i < 800 ? 10 : 0;
            cell.Temperature = 15;
            cell.StateId = (i % 5) + 1;
            cell.ReligionId = (i % 3) + 1;
            
            map.Cells.Add(cell);
        }

        // Create states
        for (int i = 1; i <= 5; i++)
        {
            map.States.Add(new State
            {
                Id = i,
                Name = $"State{i}"
            });
        }

        // Create religions
        for (int i = 1; i <= 3; i++)
        {
            map.Religions.Add(new Religion
            {
                Id = i,
                Name = $"Religion{i}"
            });
        }

        return map;
    }

    [Fact]
    public void Generate_ShouldCreateMarkers()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMarkers = true };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        Assert.NotNull(markers);
        Assert.NotEmpty(markers);
    }

    [Fact]
    public void Generate_ShouldPlaceVolcanoes()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateMarkers = true,
            GenerateNaturalMarkers = true
        };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        var volcanoes = markers.Where(m => m.Type == MarkerType.Volcano).ToList();
        Assert.NotEmpty(volcanoes);

        // Volcanoes should be in mountains
        foreach (var volcano in volcanoes)
        {
            var cell = map.Cells[volcano.CellId];
            Assert.True(cell.Height >= 70);
        }
    }

    [Fact]
    public void Generate_ShouldPlaceHotSprings()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateMarkers = true,
            GenerateNaturalMarkers = true
        };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        var hotSprings = markers.Where(m => m.Type == MarkerType.HotSpring).ToList();
        
        // Should have hot springs if there are volcanoes
        var volcanoes = markers.Where(m => m.Type == MarkerType.Volcano).ToList();
        if (volcanoes.Any())
        {
            Assert.NotEmpty(hotSprings);
        }
    }

    [Fact]
    public void Generate_ShouldPlaceRuins()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateMarkers = true,
            GenerateHistoricalMarkers = true
        };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        var ruins = markers.Where(m => m.Type == MarkerType.Ruins).ToList();
        Assert.NotEmpty(ruins);

        // Ruins should be on land
        foreach (var ruin in ruins)
        {
            var cell = map.Cells[ruin.CellId];
            Assert.True(cell.Height >= 20);
        }
    }

    [Fact]
    public void Generate_ShouldPlaceSacredSites()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateMarkers = true,
            GenerateReligiousMarkers = true
        };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        var sacredSites = markers.Where(m => m.Type == MarkerType.SacredSite).ToList();
        Assert.NotEmpty(sacredSites);

        // Sacred sites should be on land
        foreach (var site in sacredSites)
        {
            var cell = map.Cells[site.CellId];
            Assert.True(cell.Height >= 20);
        }
    }

    [Fact]
    public void Generate_ShouldPlaceMonsterLairs()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateMarkers = true,
            GenerateDangerousMarkers = true
        };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        var lairs = markers.Where(m => m.Type == MarkerType.MonsterLair).ToList();
        Assert.NotEmpty(lairs);

        // Monster lairs should be in remote areas
        foreach (var lair in lairs)
        {
            var cell = map.Cells[lair.CellId];
            Assert.True(cell.Height >= 20); // Land
            Assert.Equal(0, cell.Population); // Unpopulated
        }
    }

    [Fact]
    public void Generate_ShouldRespectMarkerDensity()
    {
        var map = CreateTestMap();
        var random1 = new PcgRandomSource(12345);
        var random2 = new PcgRandomSource(12345);

        var settings1 = new MapGenerationSettings
        {
            GenerateMarkers = true,
            MarkerDensity = 0.5
        };
        var settings2 = new MapGenerationSettings
        {
            GenerateMarkers = true,
            MarkerDensity = 2.0
        };

        var generator1 = new MarkersGenerator(map, random1, settings1);
        var generator2 = new MarkersGenerator(map, random2, settings2);

        var markers1 = generator1.Generate();
        var markers2 = generator2.Generate();

        // Higher density should produce more markers
        Assert.True(markers2.Count >= markers1.Count);
    }

    [Fact]
    public void Generate_ShouldAssignUniqueIds()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateMarkers = true };
        var generator = new MarkersGenerator(map, random, settings);

        var markers = generator.Generate();

        var ids = markers.Select(m => m.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(ids.Count, uniqueIds.Count);
    }
}
