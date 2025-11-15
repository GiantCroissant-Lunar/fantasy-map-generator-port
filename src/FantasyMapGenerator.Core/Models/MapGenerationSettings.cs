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
    /// Generate sea routes for naval powers
    /// </summary>
    public bool GenerateSeaRoutes { get; set; } = true;

    /// <summary>
    /// Maximum route length (cells)
    /// </summary>
    public int MaxRouteLength { get; set; } = 100;

    /// <summary>
    /// Whether to generate provinces
    /// </summary>
    public bool GenerateProvinces { get; set; } = true;

    /// <summary>
    /// Enable marker generation
    /// </summary>
    public bool GenerateMarkers { get; set; } = true;

    /// <summary>
    /// Marker density multiplier
    /// </summary>
    public double MarkerDensity { get; set; } = 1.0;

    /// <summary>
    /// Enable natural markers (volcanoes, hot springs)
    /// </summary>
    public bool GenerateNaturalMarkers { get; set; } = true;

    /// <summary>
    /// Enable historical markers (ruins, battlefields)
    /// </summary>
    public bool GenerateHistoricalMarkers { get; set; } = true;

    /// <summary>
    /// Enable religious markers (sacred sites)
    /// </summary>
    public bool GenerateReligiousMarkers { get; set; } = true;

    /// <summary>
    /// Enable dangerous markers (monster lairs)
    /// </summary>
    public bool GenerateDangerousMarkers { get; set; } = true;

    /// <summary>
    /// Minimum burgs per province
    /// </summary>
    public int MinBurgsPerProvince { get; set; } = 3;

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
    /// Apply Lloyd relaxation to improve point distribution uniformity
    /// </summary>
    public bool ApplyLloydRelaxation { get; set; } = false;

    /// <summary>
    /// Number of Lloyd relaxation iterations (1-3 typical)
    /// </summary>
    public int LloydIterations { get; set; } = 1;

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

    // === River Meandering Settings ===

    /// <summary>
    /// Enable river meandering path generation
    /// </summary>
    public bool EnableRiverMeandering { get; set; } = true;

    /// <summary>
    /// Base meandering factor (0.0 = straight, 1.0 = very curvy)
    /// </summary>
    public double MeanderingFactor { get; set; } = 0.5;

    // === River Erosion Settings ===

    /// <summary>
    /// Enable river erosion/downcutting
    /// </summary>
    public bool EnableRiverErosion { get; set; } = true;

    /// <summary>
    /// Maximum height a river can erode per cell (1-10)
    /// </summary>
    public int MaxErosionDepth { get; set; } = 5;

    /// <summary>
    /// Minimum height for erosion to occur
    /// </summary>
    public int MinErosionHeight { get; set; } = 35;

    /// <summary>
    /// Use advanced erosion algorithm (neighbor-based) instead of simple river downcutting
    /// </summary>
    public bool UseAdvancedErosion { get; set; } = false;

    /// <summary>
    /// Number of erosion iterations for advanced erosion (3-10 typical)
    /// </summary>
    public int ErosionIterations { get; set; } = 5;

    /// <summary>
    /// Erosion amount per iteration for advanced erosion (0.05-0.2 typical)
    /// </summary>
    public double ErosionAmount { get; set; } = 0.1;

    // === Lake Evaporation Settings ===

    /// <summary>
    /// Enable lake evaporation modeling
    /// </summary>
    public bool EnableLakeEvaporation { get; set; } = true;

    /// <summary>
    /// Base evaporation rate (m³/s per km² per degree)
    /// </summary>
    public double BaseEvaporationRate { get; set; } = 0.5;

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

    // === Cultures Settings ===
    
    /// <summary>
    /// Number of cultures to generate
    /// </summary>
    public int CultureCount { get; set; } = 10;
    
    // === Religions Settings ===
    
    /// <summary>
    /// Number of religions to generate
    /// </summary>
    public int ReligionCount { get; set; } = 5;
    
    /// <summary>
    /// Culture set to use (European, Oriental, HighFantasy, DarkFantasy)
    /// </summary>
    public string CultureSet { get; set; } = "European";
    
    /// <summary>
    /// Neutral area rate (affects culture expansion)
    /// </summary>
    public double NeutralRate { get; set; } = 1.0;
    
    // === States Settings ===
    
    /// <summary>
    /// Size variety factor (affects expansionism)
    /// </summary>
    public double SizeVariety { get; set; } = 1.0;
    
    /// <summary>
    /// Growth rate multiplier
    /// </summary>
    public double GrowthRate { get; set; } = 1.0;
    
    /// <summary>
    /// Current year for campaign generation
    /// </summary>
    public int CurrentYear { get; set; } = 1000;

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
