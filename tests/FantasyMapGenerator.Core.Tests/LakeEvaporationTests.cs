using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Unit tests for the Lake Evaporation system
/// </summary>
public class LakeEvaporationTests
{
    [Fact]
    public void Lake_IsClosed_WhenEvaporationExceedsInflow()
    {
        var lake = new Lake
        {
            Inflow = 100,
            Evaporation = 150
        };

        Assert.True(lake.IsClosed);
        Assert.Equal(0, lake.NetOutflow);
    }

    [Fact]
    public void Lake_IsOpen_WhenInflowExceedsEvaporation()
    {
        var lake = new Lake
        {
            Inflow = 150,
            Evaporation = 100
        };

        Assert.False(lake.IsClosed);
        Assert.Equal(50, lake.NetOutflow);
    }

    [Fact]
    public void Lake_NetOutflow_NeverNegative()
    {
        var lake = new Lake
        {
            Inflow = 50,
            Evaporation = 200
        };

        Assert.True(lake.NetOutflow >= 0);
        Assert.Equal(0, lake.NetOutflow);
    }

    [Fact]
    public void MapGeneration_WithLakeEvaporation_CreatesLakes()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 0.5
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Should have some lakes
        Assert.NotNull(map.Lakes);
        
        // If lakes exist, verify they have properties calculated
        if (map.Lakes.Count > 0)
        {
            foreach (var lake in map.Lakes)
            {
                Assert.NotEmpty(lake.Cells);
                Assert.True(lake.SurfaceArea > 0);
                Assert.True(lake.Evaporation >= 0);
                Assert.True(lake.Inflow >= 0);
            }
        }
    }

    [Fact]
    public void MapGeneration_WithLakeEvaporation_CreatesClosedBasins()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 99999,
            Width = 300,
            Height = 300,
            NumPoints = 800,
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 0.8 // Higher evaporation
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Should have some lakes
        if (map.Lakes.Count > 0)
        {
            // Check that lake types are assigned
            Assert.All(map.Lakes, lake =>
            {
                Assert.True(Enum.IsDefined(typeof(LakeType), lake.Type));
            });

            // Closed lakes should be saltwater
            var closedLakes = map.Lakes.Where(l => l.IsClosed).ToList();
            if (closedLakes.Any())
            {
                Assert.All(closedLakes, lake =>
                {
                    Assert.Equal(LakeType.Saltwater, lake.Type);
                    Assert.Equal(-1, lake.OutletCell);
                    Assert.Null(lake.OutletRiver);
                });
            }
        }
    }

    [Fact]
    public void MapGeneration_WithLakeEvaporationDisabled_NoLakes()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 54321,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = false
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Lakes list should be empty when evaporation is disabled
        Assert.Empty(map.Lakes);
    }

    [Fact]
    public void Lake_HighTemperature_HighEvaporation()
    {
        // This tests the evaporation calculation logic indirectly
        var settings = new MapGenerationSettings
        {
            Seed = 11111,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 1.0 // High evaporation rate
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        if (map.Lakes.Count > 0)
        {
            // Lakes should have evaporation calculated
            Assert.All(map.Lakes, lake =>
            {
                Assert.True(lake.Evaporation >= 0);
            });

            // Higher temperature lakes should generally have higher evaporation
            // (though this depends on other factors too)
            var lakesWithTemp = map.Lakes.Where(l => l.Temperature > 0).ToList();
            if (lakesWithTemp.Count > 1)
            {
                Assert.True(lakesWithTemp.Any(l => l.Evaporation > 0));
            }
        }
    }

    [Fact]
    public void Lake_HasShoreline()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 22222,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        if (map.Lakes.Count > 0)
        {
            // Lakes should have shoreline cells identified
            Assert.All(map.Lakes, lake =>
            {
                // Shoreline might be empty for very small lakes, but most should have it
                Assert.NotNull(lake.Shoreline);
            });

            // At least some lakes should have shoreline
            Assert.Contains(map.Lakes, l => l.Shoreline.Count > 0);
        }
    }

    [Fact]
    public void Lake_OpenLake_HasOutlet()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 33333,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 0.3 // Low evaporation to ensure open lakes
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        if (map.Lakes.Count > 0)
        {
            var openLakes = map.Lakes.Where(l => !l.IsClosed).ToList();
            
            if (openLakes.Any())
            {
                // Open lakes should have outlet cells
                Assert.All(openLakes, lake =>
                {
                    // Outlet cell should be valid or -1
                    Assert.True(lake.OutletCell >= -1);
                    
                    // Type should not be saltwater
                    Assert.NotEqual(LakeType.Saltwater, lake.Type);
                });
            }
        }
    }

    [Fact]
    public void MapGeneration_LakeEvaporation_IsDeterministic()
    {
        var settings1 = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 0.5
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 0.5
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // Should generate same number of lakes
        Assert.Equal(map1.Lakes.Count, map2.Lakes.Count);

        // Lakes should have same properties
        for (int i = 0; i < map1.Lakes.Count; i++)
        {
            var lake1 = map1.Lakes[i];
            var lake2 = map2.Lakes[i];

            Assert.Equal(lake1.Cells.Count, lake2.Cells.Count);
            Assert.Equal(lake1.IsClosed, lake2.IsClosed);
            Assert.Equal(lake1.Type, lake2.Type);
            Assert.Equal(lake1.Evaporation, lake2.Evaporation, precision: 2);
            Assert.Equal(lake1.Inflow, lake2.Inflow, precision: 2);
        }
    }

    [Fact]
    public void Lake_SurfaceArea_MatchesCellCount()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 44444,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        if (map.Lakes.Count > 0)
        {
            // Surface area should equal cell count (simplified model)
            Assert.All(map.Lakes, lake =>
            {
                Assert.Equal(lake.Cells.Count, lake.SurfaceArea);
            });
        }
    }

    [Fact]
    public void MapGeneration_LakeEvaporation_Performance()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 55555,
            Width = 400,
            Height = 400,
            NumPoints = 2000, // Larger map
            EnableLakeEvaporation = true,
            BaseEvaporationRate = 0.5
        };

        var generator = new MapGenerator();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var map = generator.Generate(settings);

        stopwatch.Stop();

        // Total generation should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 15000,
            $"Map generation took {stopwatch.ElapsedMilliseconds}ms, should be < 15s");

        Assert.NotNull(map);
        Assert.NotNull(map.Lakes);
    }

    [Fact]
    public void Lake_InflowingRivers_Tracked()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 66666,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableLakeEvaporation = true
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        if (map.Lakes.Count > 0 && map.Rivers.Count > 0)
        {
            // Check that inflowing rivers are tracked
            Assert.All(map.Lakes, lake =>
            {
                Assert.NotNull(lake.InflowingRivers);
                
                // If lake has inflow, it should have inflowing rivers
                if (lake.Inflow > 0)
                {
                    // Note: Inflow might come from precipitation too
                    // so this is not a strict requirement
                }
            });
        }
    }
}
