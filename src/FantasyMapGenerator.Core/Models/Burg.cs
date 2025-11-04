namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a city, town, or settlement
/// </summary>
public class Burg
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Point Position { get; set; }
    public int Cell { get; set; } // Cell ID where burg is located
    public int State { get; set; } = -1;
    public int Culture { get; set; } = -1;
    public string? Feature { get; set; } // Special feature description
    
    // Population and type
    public int Population { get; set; }
    public BurgType Type { get; set; }
    public bool IsCapital { get; set; }
    public bool IsPort { get; set; }
    
    // Economic data
    public int Trade { get; set; }
    public int Wealth { get; set; }
    
    // Military
    public int Garrison { get; set; }
    public int Walls { get; set; } // Wall level 0-5
    
    // Religion
    public int Religion { get; set; } = -1;
    public string? Temple { get; set; }
    
    // Coordinates for display
    public double X => Position.X;
    public double Y => Position.Y;
    
    public Burg(int id, Point position, int cell)
    {
        Id = id;
        Position = position;
        Cell = cell;
    }
}

public enum BurgType
{
    Village,
    Town,
    City,
    Capital,
    Port
}