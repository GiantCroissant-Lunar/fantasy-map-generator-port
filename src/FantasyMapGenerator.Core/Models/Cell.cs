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
    
    /// <summary>
    /// Harbor quality (0 = none, 1 = good, 2 = excellent)
    /// </summary>
    public byte Harbor { get; set; }
    
    /// <summary>
    /// Nearest water cell (for port detection)
    /// </summary>
    public int? HavenCell { get; set; }
    
    /// <summary>
    /// River ID if this cell has a river
    /// </summary>
    public int RiverId { get; set; } = -1;
    
    /// <summary>
    /// River flux (flow accumulation)
    /// </summary>
    public int Flux { get; set; }
    
    /// <summary>
    /// Grid cell ID for temperature/precipitation lookup
    /// </summary>
    public int GridCellId { get; set; }

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
