namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Heightmap generation profiles for different terrain types
/// </summary>
public enum HeightmapProfile
{
    /// <summary>
    /// Default terrain generation
    /// </summary>
    Default,
    
    /// <summary>
    /// Island with coastal falloff
    /// </summary>
    Island,
    
    /// <summary>
    /// Large continents with some ocean
    /// </summary>
    Continents,
    
    /// <summary>
    /// Scattered small islands
    /// </summary>
    Archipelago,
    
    /// <summary>
    /// Single large landmass
    /// </summary>
    Pangea,
    
    /// <summary>
    /// Mixed land and sea with coastal regions
    /// </summary>
    Mediterranean
}
