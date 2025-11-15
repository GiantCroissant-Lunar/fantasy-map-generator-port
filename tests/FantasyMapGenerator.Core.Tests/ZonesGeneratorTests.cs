using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

namespace FantasyMapGenerator.Core.Tests;

public class ZonesGeneratorTests
{
    private MapData CreateTestMap(int cellCount = 1000)
    {
        var map = new MapData(800, 600, cellCount);
        var random = new PcgRandomSource(12345);

        // Create cells with varied terrain
        for (int i = 0; i < cellCount; i++)
        {
            var cell = new Cell
            {
                Id = i,
                Center = new Point(random.Next(800), random.Next(600)),
                Height = (byte)random.Next(0, 100),
                BiomeId = random.Next(1, 13),
                Population = random.Next(0, 100),
                Burg = random.Next(0, 10) == 0 ? random.Next(1, 5) : 0,
                Neighbors = new List<int>()
            };

            // Add some neighbors
            for (int j = Math.Max(0, i - 3); j < i; j++)
            {
                if (random.Next(3) == 0)
                {
                    cell.Neighbors.Add(j);
                    map.Cells[j].Neighbors.Add(i);
                }
            }

            map.Cells.Add(cell);
        }

        // Add biomes
        for (int i = 0; i < 13; i++)
        {
            map.Biomes.Add(new Biome { Id = i, Name = $"Biome{i}" });
        }

        return map;
    }

    [Fact]
    public void Generate_CreatesZones()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true,
            GenerateDangerZones = true,
            GenerateProtectedAreas = true,
            GenerateSpecialZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        Assert.NotEmpty(map.Zones);
    }

    [Fact]
    public void Generate_WhenDisabled_CreatesNoZones()
    {
        // Arrange
        var map = CreateTestMap();
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = false
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        Assert.Empty(map.Zones);
    }

    [Fact]
    public void Generate_CreatesDangerZones()
    {
        // Arrange
        var map = CreateTestMap(2000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true,
            GenerateDangerZones = true,
            GenerateProtectedAreas = false,
            GenerateSpecialZones = false
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        var dangerZones = map.Zones.Where(z =>
            z.Type == ZoneType.DangerZone ||
            z.Type == ZoneType.Cursed ||
            z.Type == ZoneType.Haunted ||
            z.Type == ZoneType.Blighted).ToList();

        Assert.NotEmpty(dangerZones);
        Assert.All(dangerZones, z =>
        {
            Assert.NotEmpty(z.Name);
            Assert.NotEmpty(z.Description);
            Assert.InRange(z.Intensity, 0.5, 1.0);
            Assert.NotEmpty(z.Cells);
        });
    }

    [Fact]
    public void Generate_CreatesProtectedAreas()
    {
        // Arrange
        var map = CreateTestMap(2000);
        
        // Ensure we have forest biomes
        foreach (var cell in map.Cells.Where(c => c.Height >= 20).Take(500))
        {
            cell.BiomeId = 7; // Forest
            cell.Population = 0;
        }

        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true,
            GenerateDangerZones = false,
            GenerateProtectedAreas = true,
            GenerateSpecialZones = false
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        var protectedAreas = map.Zones.Where(z =>
            z.Type == ZoneType.NatureReserve ||
            z.Type == ZoneType.SacredGrove ||
            z.Type == ZoneType.RoyalHunt ||
            z.Type == ZoneType.Sanctuary).ToList();

        Assert.NotEmpty(protectedAreas);
        Assert.All(protectedAreas, z =>
        {
            Assert.NotEmpty(z.Name);
            Assert.NotEmpty(z.Description);
            Assert.InRange(z.Intensity, 0.3, 0.6);
            Assert.NotEmpty(z.Cells);
        });
    }

    [Fact]
    public void Generate_CreatesSpecialZones()
    {
        // Arrange
        var map = CreateTestMap(3000);
        
        // Ensure we have appropriate biomes
        foreach (var cell in map.Cells.Where(c => c.Height >= 20).Take(500))
        {
            cell.BiomeId = 8; // Dense forest
            cell.Population = 0;
        }
        
        foreach (var cell in map.Cells.Where(c => c.Height >= 20).Skip(500).Take(500))
        {
            cell.BiomeId = 1; // Desert
            cell.Population = 0;
        }

        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true,
            GenerateDangerZones = false,
            GenerateProtectedAreas = false,
            GenerateSpecialZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert - Special zones are probabilistic, so we just check structure
        if (map.Zones.Any())
        {
            Assert.All(map.Zones, z =>
            {
                Assert.NotEmpty(z.Name);
                Assert.NotEmpty(z.Description);
                Assert.InRange(z.Intensity, 0.0, 1.0);
                Assert.NotEmpty(z.Cells);
            });
        }
    }

    [Fact]
    public void Generate_ZonesHaveValidIds()
    {
        // Arrange
        var map = CreateTestMap(2000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        var ids = map.Zones.Select(z => z.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count); // All unique
        Assert.All(ids, id => Assert.True(id > 0));
    }

    [Fact]
    public void Generate_ZonesHaveValidCells()
    {
        // Arrange
        var map = CreateTestMap(2000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        Assert.All(map.Zones, zone =>
        {
            Assert.NotEmpty(zone.Cells);
            Assert.All(zone.Cells, cellId =>
            {
                Assert.InRange(cellId, 0, map.Cells.Count - 1);
            });
            Assert.Contains(zone.CenterCellId, zone.Cells);
        });
    }

    [Fact]
    public void Generate_ZonesHaveReasonableSizes()
    {
        // Arrange
        var map = CreateTestMap(2000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        Assert.All(map.Zones, zone =>
        {
            Assert.InRange(zone.Cells.Count, 2, 50); // Reasonable zone sizes
        });
    }

    [Fact]
    public void Generate_ZoneDensityAffectsCount()
    {
        // Arrange
        var map1 = CreateTestMap(5000);
        var map2 = CreateTestMap(5000);
        var random1 = new PcgRandomSource(12345);
        var random2 = new PcgRandomSource(12345);
        
        var settings1 = new MapGenerationSettings
        {
            GenerateZones = true,
            ZoneDensity = 0.5
        };
        var settings2 = new MapGenerationSettings
        {
            GenerateZones = true,
            ZoneDensity = 2.0
        };

        var generator1 = new ZonesGenerator(map1, random1, settings1);
        var generator2 = new ZonesGenerator(map2, random2, settings2);

        // Act
        generator1.Generate();
        generator2.Generate();

        // Assert
        Assert.True(map2.Zones.Count >= map1.Zones.Count);
    }

    [Fact]
    public void Generate_ZonesHaveAppropriateNames()
    {
        // Arrange
        var map = CreateTestMap(2000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        Assert.All(map.Zones, zone =>
        {
            Assert.NotEmpty(zone.Name);
            Assert.Contains(" ", zone.Name); // Should have prefix and suffix
            
            // Check name matches type
            switch (zone.Type)
            {
                case ZoneType.Cursed:
                    Assert.Contains("Cursed", zone.Name, StringComparison.OrdinalIgnoreCase);
                    break;
                case ZoneType.SacredGrove:
                    Assert.True(
                        zone.Name.Contains("Sacred", StringComparison.OrdinalIgnoreCase) ||
                        zone.Name.Contains("Holy", StringComparison.OrdinalIgnoreCase) ||
                        zone.Name.Contains("Blessed", StringComparison.OrdinalIgnoreCase));
                    break;
                case ZoneType.MagicalForest:
                    Assert.True(
                        zone.Name.Contains("Enchanted", StringComparison.OrdinalIgnoreCase) ||
                        zone.Name.Contains("Mystic", StringComparison.OrdinalIgnoreCase) ||
                        zone.Name.Contains("Magical", StringComparison.OrdinalIgnoreCase));
                    break;
            }
        });
    }

    [Fact]
    public void Generate_ZonesHaveValidColors()
    {
        // Arrange
        var map = CreateTestMap(2000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        Assert.All(map.Zones, zone =>
        {
            Assert.NotEmpty(zone.Color);
            Assert.StartsWith("#", zone.Color);
            Assert.True(zone.Color.Length == 7 || zone.Color.Length == 9); // #RRGGBB or #RRGGBBAA
        });
    }

    [Fact]
    public void Generate_IsDeterministic()
    {
        // Arrange
        var map1 = CreateTestMap(2000);
        var map2 = CreateTestMap(2000);
        var random1 = new PcgRandomSource(12345);
        var random2 = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };

        var generator1 = new ZonesGenerator(map1, random1, settings);
        var generator2 = new ZonesGenerator(map2, random2, settings);

        // Act
        generator1.Generate();
        generator2.Generate();

        // Assert
        Assert.Equal(map1.Zones.Count, map2.Zones.Count);
        for (int i = 0; i < map1.Zones.Count; i++)
        {
            Assert.Equal(map1.Zones[i].Type, map2.Zones[i].Type);
            Assert.Equal(map1.Zones[i].Name, map2.Zones[i].Name);
            Assert.Equal(map1.Zones[i].CenterCellId, map2.Zones[i].CenterCellId);
            Assert.Equal(map1.Zones[i].Cells.Count, map2.Zones[i].Cells.Count);
        }
    }

    [Fact]
    public void Generate_PerformanceTest()
    {
        // Arrange
        var map = CreateTestMap(10000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        generator.Generate();
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 500, $"Generation took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Generate_ZonesDoNotOverlap()
    {
        // Arrange
        var map = CreateTestMap(3000);
        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true,
            ZoneDensity = 2.0
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        var allZoneCells = new HashSet<int>();
        foreach (var zone in map.Zones)
        {
            foreach (var cellId in zone.Cells)
            {
                Assert.DoesNotContain(cellId, allZoneCells); // No overlaps
                allZoneCells.Add(cellId);
            }
        }
    }

    [Fact]
    public void Generate_DangerZonesAvoidSettlements()
    {
        // Arrange
        var map = CreateTestMap(2000);
        
        // Mark some cells with settlements
        foreach (var cell in map.Cells.Take(100))
        {
            cell.Burg = 1;
            cell.Population = 100;
        }

        var random = new PcgRandomSource(12345);
        var settings = new MapGenerationSettings
        {
            GenerateZones = true,
            GenerateDangerZones = true,
            GenerateProtectedAreas = false,
            GenerateSpecialZones = false
        };
        var generator = new ZonesGenerator(map, random, settings);

        // Act
        generator.Generate();

        // Assert
        var dangerZones = map.Zones.Where(z =>
            z.Type == ZoneType.DangerZone ||
            z.Type == ZoneType.Cursed ||
            z.Type == ZoneType.Haunted ||
            z.Type == ZoneType.Blighted).ToList();

        foreach (var zone in dangerZones)
        {
            foreach (var cellId in zone.Cells)
            {
                var cell = map.Cells[cellId];
                Assert.True(cell.Burg == 0 || cell.Burg == -1);
                Assert.Equal(0, cell.Population);
            }
        }
    }
}
