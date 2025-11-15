using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Noise;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates heightmaps using FastNoiseLite for realistic terrain
/// </summary>
public class FastNoiseHeightmapGenerator
{
    private readonly FastNoiseLite _baseNoise;
    private readonly FastNoiseLite _detailNoise;
    private readonly int _seed;

    public FastNoiseHeightmapGenerator(int seed)
    {
        _seed = seed;

        // Base terrain (large features)
        _baseNoise = new FastNoiseLite(seed);
        _baseNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _baseNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        _baseNoise.SetFractalOctaves(4);
        _baseNoise.SetFractalLacunarity(2.0f);
        _baseNoise.SetFractalGain(0.5f);
        _baseNoise.SetFrequency(0.8f); // Lower = larger features

        // Detail noise (small features)
        _detailNoise = new FastNoiseLite(seed + 1);
        _detailNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _detailNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        _detailNoise.SetFractalOctaves(3);
        _detailNoise.SetFrequency(3.0f); // Higher = smaller features
    }

    /// <summary>
    /// Generate heightmap using FastNoiseLite with specified profile
    /// </summary>
    public byte[] Generate(MapData mapData, HeightmapProfile profile)
    {
        var width = mapData.Width;
        var height = mapData.Height;
        var heightmap = new byte[mapData.Cells.Count];

        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];

            // Normalize coordinates to [0, 1]
            float nx = (float)(cell.Center.X / width);
            float ny = (float)(cell.Center.Y / height);

            // Combine base and detail noise
            float baseValue = _baseNoise.GetNoise(nx * 1000, ny * 1000);
            float detailValue = _detailNoise.GetNoise(nx * 1000, ny * 1000);

            float combined = baseValue * 0.7f + detailValue * 0.3f;

            // Apply profile (e.g., island mask)
            combined = ApplyProfile(combined, nx, ny, profile);

            // Clamp to [0, 100]
            byte heightValue = (byte)Math.Clamp((combined + 1) * 50, 0, 100);
            heightmap[i] = heightValue;
        }

        return heightmap;
    }

    /// <summary>
    /// Generate heightmap using FastNoiseLite with custom settings
    /// </summary>
    public byte[] Generate(MapData mapData, MapGenerationSettings settings)
    {
        // Validate settings
        ValidateSettings(settings);

        var noise = new FastNoiseLite((int)settings.Seed);

        // Apply custom settings if provided
        if (!string.IsNullOrEmpty(settings.NoiseType))
        {
            if (Enum.TryParse<FastNoiseLite.NoiseType>(settings.NoiseType, true, out var noiseType))
            {
                noise.SetNoiseType(noiseType);
            }
        }

        if (!string.IsNullOrEmpty(settings.FractalType))
        {
            if (Enum.TryParse<FastNoiseLite.FractalType>(settings.FractalType, true, out var fractalType))
            {
                noise.SetFractalType(fractalType);
            }
        }

        noise.SetFractalOctaves(settings.Octaves);
        noise.SetFrequency(settings.Frequency);

        // Configure domain warping if enabled
        if (settings.DomainWarpStrength > 0)
        {
            var warpType = ParseDomainWarpType(settings.DomainWarpType);
            noise.SetDomainWarpType(warpType);
            noise.SetDomainWarpAmp(settings.DomainWarpStrength);
        }

        var width = mapData.Width;
        var height = mapData.Height;
        var heightmap = new byte[mapData.Cells.Count];

        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];

            // Normalize coordinates to [0, 1]
            float nx = (float)(cell.Center.X / width);
            float ny = (float)(cell.Center.Y / height);

            // Apply domain warping if enabled
            if (settings.DomainWarpStrength > 0)
            {
                noise.DomainWarp(ref nx, ref ny);
            }

            // Get noise value in range [-1, 1]
            float value = noise.GetNoise(nx * 1000, ny * 1000);

            // Apply profile if specified
            var profile = ParseProfile(settings.HeightmapTemplate ?? "default");
            value = ApplyProfile(value, nx, ny, profile);

            // Map to [0, 100]
            byte heightValue = (byte)Math.Clamp((value + 1) * 50, 0, 100);
            heightmap[i] = heightValue;
        }

        return heightmap;
    }

    private float ApplyProfile(float value, float nx, float ny, HeightmapProfile profile)
    {
        switch (profile)
        {
            case HeightmapProfile.Island:
                return ApplyIslandMask(value, nx, ny);

            case HeightmapProfile.Continents:
                return ApplyContinentMask(value, nx, ny);

            case HeightmapProfile.Archipelago:
                return ApplyArchipelagoMask(value, nx, ny);

            case HeightmapProfile.Pangea:
                return ApplyPangeaMask(value, nx, ny);

            case HeightmapProfile.Mediterranean:
                return ApplyMediterraneanMask(value, nx, ny);

            default:
                return value;
        }
    }

    private float ApplyIslandMask(float value, float nx, float ny)
    {
        // Distance from center
        float dx = nx - 0.5f;
        float dy = ny - 0.5f;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        // Radial falloff
        float mask = 1.0f - MathF.Pow(distance * 2.0f, 2.5f);
        mask = Math.Clamp(mask, 0, 1);

        // Blend with noise
        return value * mask + (mask - 1.0f);
    }

    private float ApplyContinentMask(float value, float nx, float ny)
    {
        // Less aggressive falloff for larger landmasses
        float dx = nx - 0.5f;
        float dy = ny - 0.5f;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        float mask = 1.0f - MathF.Pow(distance * 1.5f, 1.8f);
        mask = Math.Clamp(mask, -0.2f, 1); // Allow some ocean

        return value * mask;
    }

    private float ApplyArchipelagoMask(float value, float nx, float ny)
    {
        // Use cellular noise for scattered islands
        var islandNoise = new FastNoiseLite(_seed + 2);
        islandNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        islandNoise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
        islandNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
        islandNoise.SetFrequency(3.0f);

        float islandMask = islandNoise.GetNoise(nx * 1000, ny * 1000);
        islandMask = 1.0f - islandMask; // Invert (closer to cell center = higher)

        return value * islandMask - 0.3f; // Bias toward ocean
    }

    private float ApplyPangeaMask(float value, float nx, float ny)
    {
        // Very gentle falloff for massive landmass
        float dx = nx - 0.5f;
        float dy = ny - 0.5f;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        float mask = 1.0f - MathF.Pow(distance * 1.2f, 1.5f);
        mask = Math.Clamp(mask, -0.1f, 1);

        return value * mask + 0.1f; // Bias toward land
    }

    private float ApplyMediterraneanMask(float value, float nx, float ny)
    {
        // Complex mask with multiple landmasses and sea
        float dx = nx - 0.5f;
        float dy = ny - 0.5f;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        // Create a "ring" of land with sea in middle and edges
        float ringMask = MathF.Sin(distance * MathF.PI * 2) * 0.5f + 0.5f;
        float radialMask = 1.0f - MathF.Pow(distance * 1.8f, 2.0f);
        radialMask = Math.Clamp(radialMask, 0, 1);

        float mask = ringMask * radialMask;
        return value * mask - 0.2f;
    }

    private void ValidateSettings(MapGenerationSettings settings)
    {
        if (settings.Octaves < 1 || settings.Octaves > 10)
        {
            throw new ArgumentException($"Octaves must be between 1 and 10, got {settings.Octaves}", nameof(settings));
        }

        if (settings.Frequency <= 0)
        {
            throw new ArgumentException($"Frequency must be positive, got {settings.Frequency}", nameof(settings));
        }

        if (settings.DomainWarpStrength < 0)
        {
            throw new ArgumentException($"DomainWarpStrength must be non-negative, got {settings.DomainWarpStrength}", nameof(settings));
        }
    }

    private HeightmapProfile ParseProfile(string template)
    {
        if (string.IsNullOrEmpty(template))
            return HeightmapProfile.Default;

        return template.ToLowerInvariant() switch
        {
            "island" => HeightmapProfile.Island,
            "continents" => HeightmapProfile.Continents,
            "archipelago" => HeightmapProfile.Archipelago,
            "pangea" => HeightmapProfile.Pangea,
            "mediterranean" => HeightmapProfile.Mediterranean,
            _ => HeightmapProfile.Default
        };
    }

    private FastNoiseLite.DomainWarpType ParseDomainWarpType(string warpType)
    {
        if (string.IsNullOrEmpty(warpType))
            return FastNoiseLite.DomainWarpType.OpenSimplex2;

        return warpType.ToLowerInvariant() switch
        {
            "opensimplex2" => FastNoiseLite.DomainWarpType.OpenSimplex2,
            "opensimplex2reduced" => FastNoiseLite.DomainWarpType.OpenSimplex2Reduced,
            "basicgrid" => FastNoiseLite.DomainWarpType.BasicGrid,
            _ => FastNoiseLite.DomainWarpType.OpenSimplex2
        };
    }
}
