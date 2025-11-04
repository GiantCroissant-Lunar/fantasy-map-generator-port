namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Enumeration of map rendering layers in draw order
/// </summary>
public enum MapLayer
{
    /// <summary>
    /// Base terrain layer (heightmap, biomes)
    /// </summary>
    Terrain = 0,
    
    /// <summary>
    /// Coastlines and shorelines
    /// </summary>
    Coastline = 1,
    
    /// <summary>
    /// Rivers and lakes
    /// </summary>
    Rivers = 2,
    
    /// <summary>
    /// Political borders between states
    /// </summary>
    Borders = 3,
    
    /// <summary>
    /// Cities, towns, and settlements
    /// </summary>
    Cities = 4,
    
    /// <summary>
    /// Text labels for features
    /// </summary>
    Labels = 5
}