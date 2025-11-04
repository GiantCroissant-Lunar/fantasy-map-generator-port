using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates biomes based on temperature, precipitation, and elevation
/// Based on the original JavaScript biomes.js
/// </summary>
public class BiomeGenerator
{
    private readonly MapData _map;
    private readonly Biome[] _biomes;
    
    public BiomeGenerator(MapData map)
    {
        _map = map;
        _biomes = CreateDefaultBiomes();
    }
    
    /// <summary>
    /// Generates biomes for all cells in the map with seeded RNG
    /// </summary>
    public void GenerateBiomes(IRandomSource random)
    {
        CalculateTemperature(random);
        CalculatePrecipitation(random);
        AssignBiomes();
    }
    
    /// <summary>
    /// Gets the default biome definitions
    /// </summary>
    public Biome[] GetDefaultBiomes()
    {
        return _biomes;
    }
    
    /// <summary>
    /// Creates the default biome definitions
    /// </summary>
    private static Biome[] CreateDefaultBiomes()
    {
        var biomes = new Biome[13];
        
        // Marine (0)
        biomes[0] = new Biome(0)
        {
            Name = BiomeTypes.Names[BiomeTypes.Marine],
            Color = BiomeTypes.Colors[BiomeTypes.Marine],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Marine],
            IconsDensity = 0,
            IsWater = true,
            MinHeight = 0,
            MaxHeight = 19,
            MinTemperature = -1,
            MaxTemperature = 2,
            MinPrecipitation = 0,
            MaxPrecipitation = 2
        };
        
        // Hot Desert (1)
        biomes[1] = new Biome(1)
        {
            Name = BiomeTypes.Names[BiomeTypes.HotDesert],
            Color = BiomeTypes.Colors[BiomeTypes.HotDesert],
            Habitability = BiomeTypes.Habitability[BiomeTypes.HotDesert],
            IconsDensity = 3,
            IsDesert = true,
            IsHot = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0.8,
            MaxTemperature = 2,
            MinPrecipitation = 0,
            MaxPrecipitation = 0.2
        };
        
        // Cold Desert (2)
        biomes[2] = new Biome(2)
        {
            Name = BiomeTypes.Names[BiomeTypes.ColdDesert],
            Color = BiomeTypes.Colors[BiomeTypes.ColdDesert],
            Habitability = BiomeTypes.Habitability[BiomeTypes.ColdDesert],
            IconsDensity = 2,
            IsDesert = true,
            IsCold = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = -0.5,
            MaxTemperature = 0.2,
            MinPrecipitation = 0,
            MaxPrecipitation = 0.3
        };
        
        // Savanna (3)
        biomes[3] = new Biome(3)
        {
            Name = BiomeTypes.Names[BiomeTypes.Savanna],
            Color = BiomeTypes.Colors[BiomeTypes.Savanna],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Savanna],
            IconsDensity = 120,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0.5,
            MaxTemperature = 1.5,
            MinPrecipitation = 0.2,
            MaxPrecipitation = 0.6
        };
        
        // Grassland (4)
        biomes[4] = new Biome(4)
        {
            Name = BiomeTypes.Names[BiomeTypes.Grassland],
            Color = BiomeTypes.Colors[BiomeTypes.Grassland],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Grassland],
            IconsDensity = 120,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0.2,
            MaxTemperature = 1.2,
            MinPrecipitation = 0.3,
            MaxPrecipitation = 0.8
        };
        
        // Tropical Seasonal Forest (5)
        biomes[5] = new Biome(5)
        {
            Name = BiomeTypes.Names[BiomeTypes.TropicalSeasonalForest],
            Color = BiomeTypes.Colors[BiomeTypes.TropicalSeasonalForest],
            Habitability = BiomeTypes.Habitability[BiomeTypes.TropicalSeasonalForest],
            IconsDensity = 120,
            IsForest = true,
            IsHot = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0.8,
            MaxTemperature = 2,
            MinPrecipitation = 0.6,
            MaxPrecipitation = 1.2
        };
        
        // Temperate Deciduous Forest (6)
        biomes[6] = new Biome(6)
        {
            Name = BiomeTypes.Names[BiomeTypes.TemperateDeciduousForest],
            Color = BiomeTypes.Colors[BiomeTypes.TemperateDeciduousForest],
            Habitability = BiomeTypes.Habitability[BiomeTypes.TemperateDeciduousForest],
            IconsDensity = 120,
            IsForest = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0.2,
            MaxTemperature = 1.0,
            MinPrecipitation = 0.5,
            MaxPrecipitation = 1.5
        };
        
        // Tropical Rainforest (7)
        biomes[7] = new Biome(7)
        {
            Name = BiomeTypes.Names[BiomeTypes.TropicalRainforest],
            Color = BiomeTypes.Colors[BiomeTypes.TropicalRainforest],
            Habitability = BiomeTypes.Habitability[BiomeTypes.TropicalRainforest],
            IconsDensity = 150,
            IsForest = true,
            IsHot = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 1.0,
            MaxTemperature = 2,
            MinPrecipitation = 1.0,
            MaxPrecipitation = 2
        };
        
        // Temperate Rainforest (8)
        biomes[8] = new Biome(8)
        {
            Name = BiomeTypes.Names[BiomeTypes.TemperateRainforest],
            Color = BiomeTypes.Colors[BiomeTypes.TemperateRainforest],
            Habitability = BiomeTypes.Habitability[BiomeTypes.TemperateRainforest],
            IconsDensity = 150,
            IsForest = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0.3,
            MaxTemperature = 0.9,
            MinPrecipitation = 0.8,
            MaxPrecipitation = 2
        };
        
        // Taiga (9)
        biomes[9] = new Biome(9)
        {
            Name = BiomeTypes.Names[BiomeTypes.Taiga],
            Color = BiomeTypes.Colors[BiomeTypes.Taiga],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Taiga],
            IconsDensity = 100,
            IsForest = true,
            IsCold = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = -0.5,
            MaxTemperature = 0.5,
            MinPrecipitation = 0.3,
            MaxPrecipitation = 0.8
        };
        
        // Tundra (10)
        biomes[10] = new Biome(10)
        {
            Name = BiomeTypes.Names[BiomeTypes.Tundra],
            Color = BiomeTypes.Colors[BiomeTypes.Tundra],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Tundra],
            IconsDensity = 5,
            IsCold = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = -1,
            MaxTemperature = 0,
            MinPrecipitation = 0,
            MaxPrecipitation = 0.5
        };
        
        // Glacier (11)
        biomes[11] = new Biome(11)
        {
            Name = BiomeTypes.Names[BiomeTypes.Glacier],
            Color = BiomeTypes.Colors[BiomeTypes.Glacier],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Glacier],
            IconsDensity = 0,
            IsCold = true,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = -1,
            MaxTemperature = -0.2,
            MinPrecipitation = 0,
            MaxPrecipitation = 0.3
        };
        
        // Wetland (12)
        biomes[12] = new Biome(12)
        {
            Name = BiomeTypes.Names[BiomeTypes.Wetland],
            Color = BiomeTypes.Colors[BiomeTypes.Wetland],
            Habitability = BiomeTypes.Habitability[BiomeTypes.Wetland],
            IconsDensity = 250,
            MinHeight = 20,
            MaxHeight = 100,
            MinTemperature = 0,
            MaxTemperature = 1.5,
            MinPrecipitation = 0.8,
            MaxPrecipitation = 2
        };
        
        return biomes;
    }
    

    /// <summary>
    /// Calculates temperature for each cell based on latitude and elevation with seeded RNG
    /// </summary>
    private void CalculateTemperature(IRandomSource random)
    {
        double mapHeight = _map.Height;
        double equatorY = mapHeight / 2;
        
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            double latitude = Math.Abs(cell.Center.Y - equatorY) / equatorY; // 0 at equator, 1 at poles
            double elevation = cell.Height / 100.0; // 0-1
            
            // Base temperature decreases with latitude and elevation
            double temperature = 1.0 - (latitude * 1.5) - (elevation * 0.8);
            
            // Add some random variation
            temperature += (random.NextDouble() - 0.5) * 0.2;
            
            cell.Temperature = Math.Clamp(temperature, -1, 2);
        }
    }
    

    /// <summary>
    /// Calculates precipitation for each cell based on temperature and distance from water with seeded RNG
    /// </summary>
    private void CalculatePrecipitation(IRandomSource random)
    {
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            
            // Base precipitation from temperature
            double precipitation = Math.Max(0, cell.Temperature * 0.8);
            
            // Increase precipitation near water
            if (cell.IsBorder)
            {
                precipitation += 0.3;
            }
            else
            {
                // Check distance to water
                double minDistanceToWater = double.MaxValue;
                foreach (var neighbor in cell.Neighbors)
                {
                    if (neighbor >= 0 && neighbor < _map.Cells.Count)
                    {
                        var neighborCell = _map.Cells[neighbor];
                        if (neighborCell.IsOcean)
                        {
                            double distance = GeometryUtils.Distance(cell.Center, neighborCell.Center);
                            minDistanceToWater = Math.Min(minDistanceToWater, distance);
                        }
                    }
                }
                
                if (minDistanceToWater < double.MaxValue)
                {
                    double distanceFactor = Math.Max(0, 1 - minDistanceToWater / 100);
                    precipitation += distanceFactor * 0.4;
                }
            }
            
            // Add some random variation
            precipitation += (random.NextDouble() - 0.5) * 0.3;
            
            cell.Precipitation = Math.Clamp(precipitation, 0, 2);
        }
    }
    
    /// <summary>
    /// Assigns biomes to cells based on their climate conditions
    /// </summary>
    private void AssignBiomes()
    {
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            
            // Find the best matching biome
            int bestBiome = 0;
            double bestScore = double.MaxValue;
            
            for (int j = 0; j < _biomes.Length; j++)
            {
                var biome = _biomes[j];
                
                if (biome.IsSuitable(cell.Temperature, cell.Precipitation, cell.Height))
                {
                    // Calculate how well this biome fits
                    double tempDiff = Math.Abs(cell.Temperature - (biome.MinTemperature + biome.MaxTemperature) / 2);
                    double precipDiff = Math.Abs(cell.Precipitation - (biome.MinPrecipitation + biome.MaxPrecipitation) / 2);
                    double heightDiff = Math.Abs(cell.Height - (biome.MinHeight + biome.MaxHeight) / 2);
                    
                    double score = tempDiff + precipDiff + heightDiff * 0.01;
                    
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestBiome = j;
                    }
                }
            }
            
            cell.Biome = bestBiome;
        }
        
        // Apply some smoothing to reduce biome fragmentation
        SmoothBiomes();
    }
    
    /// <summary>
    /// Smooths biome assignments to reduce fragmentation
    /// </summary>
    private void SmoothBiomes()
    {
        var newBiomes = new int[_map.Cells.Count];
        
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            var biomeCounts = new Dictionary<int, int>();
            
            // Count biomes in neighborhood
            biomeCounts[cell.Biome] = biomeCounts.GetValueOrDefault(cell.Biome, 0) + 2; // Weight center cell more
            
            foreach (int neighborId in cell.Neighbors)
            {
                if (neighborId >= 0 && neighborId < _map.Cells.Count)
                {
                    var neighborCell = _map.Cells[neighborId];
                    biomeCounts[neighborCell.Biome] = biomeCounts.GetValueOrDefault(neighborCell.Biome, 0) + 1;
                }
            }
            
            // Find the most common biome
            int mostCommonBiome = cell.Biome;
            int maxCount = biomeCounts.GetValueOrDefault(cell.Biome, 0);
            
            foreach (var kvp in biomeCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    mostCommonBiome = kvp.Key;
                }
            }
            
            newBiomes[i] = mostCommonBiome;
        }
        
        // Apply smoothed biomes
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            _map.Cells[i].Biome = newBiomes[i];
        }
    }
}