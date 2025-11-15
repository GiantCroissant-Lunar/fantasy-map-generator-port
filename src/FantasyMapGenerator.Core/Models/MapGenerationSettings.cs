using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Models;

public enum GridMode
{
    Poisson,
    Jittered
}

public enum RNGMode
{
    PCG,
    Alea,
    System
}

public enum HeightmapMode
{
    Auto,
    Template,
    Noise
}

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
    /// RNG algorithm selection
    /// </summary>
    public RNGMode RNGMode { get; set; } = RNGMode.PCG;

    /// <summary>
    /// Optional string seed (used for Alea-compatible PRNG); if null, Seed.ToString() is used
    /// </summary>
    public string? SeedString { get; set; }

    /// <summary>
    /// Whether to reseed the RNG at the start of key phases (e.g., heightmap)
    /// </summary>
    public bool ReseedAtPhaseStart { get; set; } = false;

    /// <summary>
    /// Point distribution mode for Voronoi site placement
    /// </summary>
    public GridMode GridMode { get; set; } = GridMode.Poisson;

    /// <summary>
    /// Heightmap generation mode (Auto defers to UseAdvancedNoise/Template)
    /// </summary>
    public HeightmapMode HeightmapMode { get; set; } = HeightmapMode.Auto;

    // === Hydrology tuning (FMG parity) ===

    /// <summary>
    /// Scale factor to convert precipitation (0..~2) to flux units used for accumulation.
    /// Default 50 gives typical p95 flux in tens/hundreds.
    /// </summary>
    public double HydrologyPrecipScale { get; set; } = 50.0;

    /// <summary>
    /// Base minimum flux to form a river before size/modifier; FMG uses ~30.
    /// </summary>
    public int HydrologyMinFlux { get; set; } = 30;

    /// <summary>
    /// Minimum accepted river length in cells.
    /// </summary>
    public int HydrologyMinRiverLength { get; set; } = 3;

    /// <summary>
    /// Auto adjust threshold downward if no rivers are formed.
    /// </summary>
    public bool HydrologyAutoAdjust { get; set; } = true;

    /// <summary>
    /// Target minimum number of rivers when auto-adjusting.
    /// </summary>
    public int HydrologyTargetRivers { get; set; } = 10;

    /// <summary>
    /// Lower bound for auto-adjusted threshold.
    /// </summary>
    public int HydrologyMinThreshold { get; set; } = 8;

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
        return RNGMode switch
        {
            RNGMode.System => new SystemRandomSource((int)Seed),
            RNGMode.PCG => new PcgRandomSource(Seed),
            RNGMode.Alea => new AleaRandomSource(SeedString ?? Seed.ToString()),
            _ => new PcgRandomSource(Seed)
        };
    }
}
