namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a special zone on the map with unique characteristics
/// </summary>
public class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ZoneType Type { get; set; }
    public List<int> Cells { get; set; } = new();
    public int CenterCellId { get; set; }
    
    /// <summary>
    /// Zone intensity (0.0-1.0) - affects gameplay/rendering
    /// </summary>
    public double Intensity { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Zone color for map display (hex format #RRGGBBAA with alpha)
    /// </summary>
    public string Color { get; set; } = "#80808080";
}

/// <summary>
/// Types of zones that can appear on the map
/// </summary>
public enum ZoneType
{
    // Dangerous zones
    DangerZone,      // Monster-infested area
    Cursed,          // Cursed land
    Haunted,         // Haunted area
    Blighted,        // Diseased/corrupted land
    
    // Protected areas
    NatureReserve,   // Protected wilderness
    SacredGrove,     // Religious protection
    RoyalHunt,       // Royal hunting grounds
    Sanctuary,       // Wildlife sanctuary
    
    // Special zones
    MagicalForest,   // Enchanted woods
    AncientRuins,    // Extensive ruins
    Wasteland,       // Barren wasteland
    Frontier         // Unexplored frontier
}
