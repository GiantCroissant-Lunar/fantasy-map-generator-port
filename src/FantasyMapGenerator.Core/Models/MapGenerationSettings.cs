using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Settings for map generation
/// </summary>
public class MapGenerationSettings
{
    /// <summary>
    /// Map width in pixels
    /// </summary>
    public int Width { get; set; } = 800;
    
    /// <summary>
    /// Map height in pixels
    /// </summary>
    public int Height { get; set; } = 600;
    
    /// <summary>
    /// Random seed for generation
    /// </summary>
    public long Seed { get; set; }
    
    /// <summary>
    /// Number of Voronoi points to generate
    /// </summary>
    public int NumPoints { get; set; } = 1000;
    
    /// <summary>
    /// Sea level (0.0 to 1.0)
    /// </summary>
    public float SeaLevel { get; set; } = 0.3f;
    
    /// <summary>
    /// Number of states to generate
    /// </summary>
    public int NumStates { get; set; } = 20;
    
    /// <summary>
    /// Number of cultures to generate
    /// </summary>
    public int NumCultures { get; set; } = 15;
    
    /// <summary>
    /// Number of cities/burgs to generate
    /// </summary>
    public int NumBurgs { get; set; } = 50;
    
    /// <summary>
    /// Whether to generate rivers
    /// </summary>
    public bool GenerateRivers { get; set; } = true;
    
    /// <summary>
    /// Whether to generate routes/roads
    /// </summary>
    public bool GenerateRoutes { get; set; } = true;
    
    /// <summary>
    /// Whether to generate provinces
    /// </summary>
    public bool GenerateProvinces { get; set; } = true;
    
    /// <summary>
    /// Map name template
    /// </summary>
    public string MapNameTemplate { get; set; } = "Fantasy Map";
    
    /// <summary>
    /// RNG algorithm: "System" or "PCG" (default: PCG)
    /// </summary>
    public string RandomAlgorithm { get; set; } = "PCG";
    
    /// <summary>
    /// Create RNG instance based on settings
    /// </summary>
    public IRandomSource CreateRandom()
    {
        return RandomAlgorithm.ToLowerInvariant() switch
        {
            "system" => new SystemRandomSource((int)Seed),
            "pcg" => new PcgRandomSource(Seed),
            _ => new PcgRandomSource(Seed)
        };
    }
}