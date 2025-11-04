namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a single cell in the Voronoi diagram
/// </summary>
public class Cell
{
    public int Id { get; set; }
    public Point Center { get; set; }
    public List<int> Vertices { get; set; } = new();
    public List<int> Neighbors { get; set; } = new();
    public bool IsBorder { get; set; }
    
    // Terrain properties
    public byte Height { get; set; } // 0-100 elevation
    public int Biome { get; set; } = -1;
    public bool IsLand => Height >= 20;
    public bool IsOcean => Height < 20;
    
    // Political properties
    public int Culture { get; set; } = -1;
    public int State { get; set; } = -1;
    public int Province { get; set; } = -1;
    public int Burg { get; set; } = -1; // City/town ID
    
    // Features
    public bool HasRiver { get; set; }
    public bool HasRoad { get; set; }
    public int Feature { get; set; } = -1; // Mountain, forest, etc.
    
    // Population and economy
    public int Population { get; set; }
    public double Temperature { get; set; }
    public double Precipitation { get; set; }
    
    // Routing and connectivity
    public int Road { get; set; } = -1;
    public int Route { get; set; } = -1;
    public bool IsRoad => Road >= 0;
    public bool IsRoute => Route >= 0;
    
    // Military
    public int Military { get; set; } = -1;
    
    public Cell(int id, Point center)
    {
        Id = id;
        Center = center;
    }
}