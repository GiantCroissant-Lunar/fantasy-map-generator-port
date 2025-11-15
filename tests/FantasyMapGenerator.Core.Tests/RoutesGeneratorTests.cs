namespace FantasyMapGenerator.Core.Tests;

using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

public class RoutesGeneratorTests
{
    private MapData CreateTestMap()
    {
        var map = new MapData(800, 600, 1000);

        // Create cells in a grid
        for (int i = 0; i < 1000; i++)
        {
            var cell = new Cell(i, new Point((i % 40) * 20 + 10, (i / 40) * 24 + 12));
            cell.Height = 50; // Land
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
                Color = $"#{i * 40:X2}{i * 30:X2}{i * 20:X2}"
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
                IsCapital = (i % 5 == 0)
            };
            map.Burgs.Add(burg);
            map.Cells[cellId].Burg = i;
        }

        return map;
    }

    [Fact]
    public void Generate_ShouldCreateRoutes()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateRoutes = true };
        var generator = new RoutesGenerator(map, random, settings);

        var routes = generator.Generate();

        Assert.NotNull(routes);
        Assert.NotEmpty(routes);
    }

    [Fact]
    public void Generate_ShouldConnectBurgsWithinStates()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateRoutes = true };
        var generator = new RoutesGenerator(map, random, settings);

        var routes = generator.Generate();

        // Each state should have routes connecting its burgs
        foreach (var state in map.States.Where(s => s.Id > 0))
        {
            var stateBurgs = map.Burgs.Where(b => b != null && b.StateId == state.Id).ToList();
            if (stateBurgs.Count >= 2)
            {
                var stateRoutes = routes.Where(r =>
                    stateBurgs.Any(b => b.Id == r.StartBurgId) &&
                    stateBurgs.Any(b => b.Id == r.EndBurgId)).ToList();

                Assert.NotEmpty(stateRoutes);
            }
        }
    }

    [Fact]
    public void Generate_ShouldHaveValidPaths()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateRoutes = true };
        var generator = new RoutesGenerator(map, random, settings);

        var routes = generator.Generate();

        foreach (var route in routes)
        {
            Assert.NotEmpty(route.Path);
            Assert.True(route.Length > 0);

            // Path should be continuous
            for (int i = 0; i < route.Path.Count - 1; i++)
            {
                var cell = map.Cells[route.Path[i]];
                var nextCell = route.Path[i + 1];
                Assert.Contains(nextCell, cell.Neighbors);
            }
        }
    }

    [Fact]
    public void Generate_ShouldAssignRouteTypes()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateRoutes = true };
        var generator = new RoutesGenerator(map, random, settings);

        var routes = generator.Generate();

        foreach (var route in routes)
        {
            Assert.True(Enum.IsDefined(typeof(RouteType), route.Type));
        }
    }

    [Fact]
    public void Generate_ShouldOptimizeRoutes()
    {
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings { GenerateRoutes = true };
        var generator = new RoutesGenerator(map, random, settings);

        var routes = generator.Generate();

        // Check for duplicate routes (same endpoints)
        var routePairs = routes.Select(r => new
        {
            Start = Math.Min(r.StartBurgId, r.EndBurgId),
            End = Math.Max(r.StartBurgId, r.EndBurgId)
        }).ToList();

        var duplicates = routePairs.GroupBy(p => new { p.Start, p.End })
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.Empty(duplicates); // No duplicate routes
    }

    [Fact]
    public void Generate_WithSeaRoutes_ShouldConnectPorts()
    {
        var map = CreateTestMap();

        // Make some burgs ports
        for (int i = 0; i < 5; i++)
        {
            map.Burgs[i].IsPort = true;
            map.Burgs[i].PortFeatureId = 1;
        }

        // Make some cells water for sea routes
        for (int i = 0; i < 100; i++)
        {
            map.Cells[i].Height = 10; // Water
        }

        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateRoutes = true,
            GenerateSeaRoutes = true
        };
        var generator = new RoutesGenerator(map, random, settings);

        var routes = generator.Generate();

        var seaRoutes = routes.Where(r => r.Type == RouteType.SeaRoute).ToList();
        
        // Should have some sea routes if ports exist
        if (map.Burgs.Count(b => b != null && b.IsPort) >= 2)
        {
            Assert.NotEmpty(seaRoutes);
        }
    }
}
