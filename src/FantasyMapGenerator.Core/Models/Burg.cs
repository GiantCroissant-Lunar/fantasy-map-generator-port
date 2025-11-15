namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a settlement (city, town, or village)
/// </summary>
public class Burg
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Settlement name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Cell where the burg is located
    /// </summary>
    public int CellId { get; set; }
    
    /// <summary>
    /// Precise position (may differ from cell center for ports/rivers)
    /// </summary>
    public Point Position { get; set; }
    
    /// <summary>
    /// State this burg belongs to (0 = neutral)
    /// </summary>
    public int StateId { get; set; }
    
    /// <summary>
    /// Culture of this burg
    /// </summary>
    public int CultureId { get; set; }
    
    /// <summary>
    /// True if this is a state capital
    /// </summary>
    public bool IsCapital { get; set; }
    
    /// <summary>
    /// True if this is a port city
    /// </summary>
    public bool IsPort { get; set; }
    
    /// <summary>
    /// Water feature ID if this is a port (ocean, sea, or lake)
    /// </summary>
    public int? PortFeatureId { get; set; }
    
    /// <summary>
    /// Population in thousands
    /// </summary>
    public double Population { get; set; }
    
    /// <summary>
    /// Burg type based on location and characteristics
    /// </summary>
    public BurgType Type { get; set; }
    
    /// <summary>
    /// Feature ID (water body) this burg is on
    /// </summary>
    public int FeatureId { get; set; }
    
    // Burg Features
    
    /// <summary>
    /// Has a citadel/fortress
    /// </summary>
    public bool HasCitadel { get; set; }
    
    /// <summary>
    /// Has a central plaza/square
    /// </summary>
    public bool HasPlaza { get; set; }
    
    /// <summary>
    /// Has defensive walls
    /// </summary>
    public bool HasWalls { get; set; }
    
    /// <summary>
    /// Has shantytown/slums
    /// </summary>
    public bool HasShanty { get; set; }
    
    /// <summary>
    /// Has a major temple
    /// </summary>
    public bool HasTemple { get; set; }
    
    /// <summary>
    /// Coat of arms (heraldry)
    /// </summary>
    public CoatOfArms? CoA { get; set; }

    // Coordinates for display
    public double X => Position.X;
    public double Y => Position.Y;

    // Legacy properties for backward compatibility
    public int Cell { get => CellId; set => CellId = value; }
    public int State { get => StateId; set => StateId = value; }
    public int Culture { get => CultureId; set => CultureId = value; }
}

/// <summary>
/// Burg classification based on location and characteristics
/// </summary>
public enum BurgType
{
    /// <summary>
    /// Generic inland settlement
    /// </summary>
    Generic,
    
    /// <summary>
    /// Port city on ocean or sea
    /// </summary>
    Naval,
    
    /// <summary>
    /// Settlement on lake shore
    /// </summary>
    Lake,
    
    /// <summary>
    /// Mountain or highland settlement
    /// </summary>
    Highland,
    
    /// <summary>
    /// Major river crossing or riverside city
    /// </summary>
    River,
    
    /// <summary>
    /// Desert or steppe nomadic settlement
    /// </summary>
    Nomadic,
    
    /// <summary>
    /// Forest hunting settlement
    /// </summary>
    Hunting
}

/// <summary>
/// Coat of arms for heraldry
/// </summary>
public class CoatOfArms
{
    public string Shield { get; set; } = "heater";
    public List<string> Charges { get; set; } = new();
    public List<string> Colors { get; set; } = new();
}
