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
    /// Heightmap template name (for backward compatibility)
    /// </summary>
    public string? HeightmapTemplate { get; set; }
    
    /// <summary>
    /// RNG algorithm: "System" or "PCG" (default: PCG)
    /// </summary>
    public string RandomAlgorithm { get; set; } = "PCG";
    
    // === Advanced Noise Settings ===
    
    /// <summary>
    /// Use FastNoiseLite for advanced terrain generation
    /// </summary>
    public bool UseAdvancedNoise { get; set; } = false;
    
    /// <summary>
    /// Noise type: OpenSimplex2, Perlin, Value, Cellular, etc.
    /// </summary>
    public string NoiseType { get; set; } = "OpenSimplex2";
    
    /// <summary>
    /// Fractal type: FBm, Ridged, PingPong
    /// </summary>
    public string FractalType { get; set; } = "FBm";
    
    /// <summary>
    /// Number of octaves (3-6 typical)
    /// </summary>
    public int Octaves { get; set; } = 4;
    
    /// <summary>
    /// Noise frequency (lower = larger features)
    /// </summary>
    public float Frequency { get; set; } = 0.8f;
    
    /// <summary>
    /// Domain warping strength (0 = disabled)
    /// </summary>
    public float DomainWarpStrength { get; set; } = 0.0f;

    /// <summary>
    /// Domain warp type: OpenSimplex2, OpenSimplex2Reduced, BasicGrid
    /// </summary>
    public string DomainWarpType { get; set; } = "OpenSimplex2";

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
