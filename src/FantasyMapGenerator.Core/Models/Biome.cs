namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a biome type
/// </summary>
public class Biome
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public int Habitability { get; set; } // 0-100
    public int IconsDensity { get; set; } // Icons per 1000 cells
    public Dictionary<string, int> Icons { get; set; } = new(); // Icon types and weights
    
    // Climate requirements
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public double MinPrecipitation { get; set; }
    public double MaxPrecipitation { get; set; }
    public byte MinHeight { get; set; }
    public byte MaxHeight { get; set; }
    
    // Properties
    public bool IsWater { get; set; }
    public bool IsLand => !IsWater;
    public bool IsForest { get; set; }
    public bool IsDesert { get; set; }
    public bool IsCold { get; set; }
    public bool IsHot { get; set; }
    
    public Biome(int id)
    {
        Id = id;
    }
    
    public bool IsSuitable(double temperature, double precipitation, byte height)
    {
        return temperature >= MinTemperature && temperature <= MaxTemperature &&
               precipitation >= MinPrecipitation && precipitation <= MaxPrecipitation &&
               height >= MinHeight && height <= MaxHeight;
    }
}

public static class BiomeTypes
{
    public const int Marine = 0;
    public const int HotDesert = 1;
    public const int ColdDesert = 2;
    public const int Savanna = 3;
    public const int Grassland = 4;
    public const int TropicalSeasonalForest = 5;
    public const int TemperateDeciduousForest = 6;
    public const int TropicalRainforest = 7;
    public const int TemperateRainforest = 8;
    public const int Taiga = 9;
    public const int Tundra = 10;
    public const int Glacier = 11;
    public const int Wetland = 12;
    
    public static readonly string[] Names = new[]
    {
        "Marine",
        "Hot desert",
        "Cold desert", 
        "Savanna",
        "Grassland",
        "Tropical seasonal forest",
        "Temperate deciduous forest",
        "Tropical rainforest",
        "Temperate rainforest",
        "Taiga",
        "Tundra",
        "Glacier",
        "Wetland"
    };
    
    public static readonly string[] Colors = new[]
    {
        "#466eab", // Marine
        "#fbe79f", // Hot desert
        "#b5b887", // Cold desert
        "#d2d082", // Savanna
        "#c8d68f", // Grassland
        "#b6d95d", // Tropical seasonal forest
        "#29bc56", // Temperate deciduous forest
        "#7dcb35", // Tropical rainforest
        "#409c43", // Temperate rainforest
        "#4b6b32", // Taiga
        "#96784b", // Tundra
        "#d5e7eb", // Glacier
        "#0b9131"  // Wetland
    };
    
    public static readonly int[] Habitability = new[]
    {
        0,   // Marine
        4,   // Hot desert
        10,  // Cold desert
        22,  // Savanna
        30,  // Grassland
        50,  // Tropical seasonal forest
        100, // Temperate deciduous forest
        80,  // Tropical rainforest
        90,  // Temperate rainforest
        12,  // Taiga
        4,   // Tundra
        0,   // Glacier
        12   // Wetland
    };
}