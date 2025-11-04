# Noise Generation Implementation Guide

## Overview

This guide details how to integrate **FastNoiseLite** into the Fantasy Map Generator port for procedural terrain generation.

**Current State**: Template-based DSL (blob, line, smooth, noise)
**Target State**: FastNoiseLite for realistic, multi-octave noise + keep templates for compatibility

---

## Why FastNoiseLite?

**Advantages**:
- 🎯 **Single-file drop-in**: ~2500 lines, no dependencies
- 🚀 **Performance**: Highly optimized, SIMD-friendly
- 🎨 **Variety**: Perlin, Simplex, Cellular, Value, Perlin+Cellular hybrid
- 📐 **Fractal Types**: FBm (Fractal Brownian Motion), Ridged, Ping-Pong
- 🌀 **Domain Warping**: Organic, flowing terrain
- 🎲 **Deterministic**: Reproducible from seed
- 📦 **Portable**: C, C++, C#, Java, JS, HLSL, GLSL versions available

**Comparison to Current**:

| Feature | Template DSL | FastNoiseLite |
|---------|--------------|---------------|
| Blob/Gaussian hills | ✅ `blob x y r h` | ✅ Cellular noise |
| Linear ridges | ✅ `line x1 y1 x2 y2 h w` | ✅ Ridged fractal |
| Smooth terrain | ✅ `smooth N` | ✅ Multi-octave noise |
| Random variation | ⚠️ Simple random | ✅ Perlin/Simplex |
| Realistic erosion | ❌ | ✅ Domain warping |
| Configurable detail | ❌ | ✅ Octave control |

---

## Installation

### Option 1: Single-File Drop-In (Recommended)

1. Download [FastNoiseLite.cs](https://github.com/Auburn/FastNoiseLite/blob/master/CSharp/FastNoiseLite.cs)
2. Place in `src/FantasyMapGenerator.Core/Noise/FastNoiseLite.cs`
3. No NuGet dependencies

### Option 2: NuGet Package (If Available)

```bash
cd src/FantasyMapGenerator.Core
dotnet add package FastNoiseLite
```

*(Note: Check NuGet.org for official package; may not exist)*

---

## Basic Usage

### 1. Simple Heightmap Generation

```csharp
using FantasyMapGenerator.Core.Noise;

public class BasicNoiseExample
{
    public byte[] GenerateHeightmap(int width, int height, int seed)
    {
        // Initialize FastNoiseLite
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        var heightmap = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalize coordinates to [0, 1]
                float nx = x / (float)width;
                float ny = y / (float)height;

                // Get noise value in range [-1, 1]
                float value = noise.GetNoise(nx * 1000, ny * 1000);

                // Map to [0, 255]
                heightmap[y * width + x] = (byte)((value + 1) * 127.5f);
            }
        }

        return heightmap;
    }
}
```

**Key Points**:
- Noise coordinates should be scaled (e.g., `× 1000`) to control frequency
- `GetNoise()` returns values in `[-1, 1]`
- Need to remap to your height range (e.g., `[0, 100]` for FMG)

---

### 2. Multi-Octave (Fractal) Noise

```csharp
public byte[] GenerateDetailedTerrain(int width, int height, int seed)
{
    var noise = new FastNoiseLite(seed);

    // Configure fractal noise
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    noise.SetFractalOctaves(5);            // More octaves = more detail
    noise.SetFractalLacunarity(2.0f);      // Frequency multiplier per octave
    noise.SetFractalGain(0.5f);            // Amplitude multiplier per octave

    var heightmap = new byte[width * height];

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            float nx = x / (float)width;
            float ny = y / (float)height;

            // FBm automatically combines octaves
            float value = noise.GetNoise(nx * 800, ny * 800);

            heightmap[y * width + x] = (byte)((value + 1) * 50); // [0, 100]
        }
    }

    return heightmap;
}
```

**Fractal Parameters**:
- **Octaves**: Number of noise layers (3-6 typical)
- **Lacunarity**: Frequency multiplier (2.0 = double frequency each octave)
- **Gain**: Amplitude multiplier (0.5 = half amplitude each octave)

**Fractal Types**:
- `FBm` (Fractal Brownian Motion): Natural terrain, clouds
- `Ridged`: Mountain ridges, veins
- `PingPong`: Terrace-like, layered terrain

---

### 3. Domain Warping (Advanced)

```csharp
public byte[] GenerateOrganicTerrain(int width, int height, int seed)
{
    var noise = new FastNoiseLite(seed);
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    noise.SetFractalOctaves(4);

    // Enable domain warping for organic, flowing shapes
    noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
    noise.SetDomainWarpAmp(30.0f); // Warping strength

    var heightmap = new byte[width * height];

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            float nx = x / (float)width * 1000;
            float ny = y / (float)height * 1000;

            // Apply domain warping
            noise.DomainWarp(ref nx, ref ny);

            // Get noise at warped coordinates
            float value = noise.GetNoise(nx, ny);

            heightmap[y * width + x] = (byte)((value + 1) * 50);
        }
    }

    return heightmap;
}
```

**Domain Warping**: Distorts the noise coordinates, creating organic, flowing patterns. Great for coastlines and rivers.

---

## Integration with Existing Code

### Strategy: Additive (Keep Template System)

Don't replace `HeightmapGenerator.cs` - add FastNoiseLite alongside it.

**File Structure**:
```
src/FantasyMapGenerator.Core/
├── Generators/
│   ├── HeightmapGenerator.cs              (existing - keep)
│   ├── FastNoiseHeightmapGenerator.cs     (new)
│   └── HybridHeightmapGenerator.cs        (new - combines both)
├── Noise/
│   └── FastNoiseLite.cs                   (new - drop-in)
```

---

### Implementation: FastNoiseHeightmapGenerator.cs

```csharp
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

    public byte[] Generate(int width, int height, HeightmapProfile profile)
    {
        var heightmap = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)width;
                float ny = y / (float)height;

                // Combine base and detail
                float baseValue = _baseNoise.GetNoise(nx * 1000, ny * 1000);
                float detailValue = _detailNoise.GetNoise(nx * 1000, ny * 1000);

                float combined = baseValue * 0.7f + detailValue * 0.3f;

                // Apply profile (e.g., island mask)
                combined = ApplyProfile(combined, nx, ny, profile);

                // Clamp to [0, 100]
                byte height = (byte)Math.Clamp((combined + 1) * 50, 0, 100);
                heightmap[y * width + x] = height;
            }
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
}

public enum HeightmapProfile
{
    Default,
    Island,
    Continents,
    Archipelago,
    Pangea,
    Mediterranean
}
```

---

### Integration with MapGenerator.cs

```csharp
public class MapGenerator
{
    public MapData Generate(MapGenerationSettings settings)
    {
        // ... existing code ...

        // Choose generator based on settings
        byte[] heightmap;

        if (settings.UseAdvancedNoise)
        {
            var noiseGenerator = new FastNoiseHeightmapGenerator((int)settings.Seed);
            var profile = ParseProfile(settings.HeightmapTemplate);
            heightmap = noiseGenerator.Generate(settings.Width, settings.Height, profile);
        }
        else
        {
            // Use existing template-based generator
            var templateGenerator = new HeightmapGenerator(settings.Width, settings.Height);
            heightmap = templateGenerator.GenerateFromTemplate(settings.HeightmapTemplate);
        }

        // ... rest of generation ...
    }

    private HeightmapProfile ParseProfile(string template)
    {
        // Map template names to profiles
        if (template.Contains("island", StringComparison.OrdinalIgnoreCase))
            return HeightmapProfile.Island;

        if (template.Contains("archipelago", StringComparison.OrdinalIgnoreCase))
            return HeightmapProfile.Archipelago;

        // ... etc ...

        return HeightmapProfile.Default;
    }
}
```

---

### Add Setting to MapGenerationSettings.cs

```csharp
public class MapGenerationSettings
{
    // ... existing properties ...

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
    /// Domain warping strength (0 = disabled)
    /// </summary>
    public float DomainWarpStrength { get; set; } = 0.0f;
}
```

---

## Advanced Use Cases

### 1. Temperature/Moisture Fields

```csharp
public class ClimateGenerator
{
    private readonly FastNoiseLite _temperatureNoise;
    private readonly FastNoiseLite _moistureNoise;

    public ClimateGenerator(int seed)
    {
        _temperatureNoise = new FastNoiseLite(seed);
        _temperatureNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        _temperatureNoise.SetFrequency(0.5f);

        _moistureNoise = new FastNoiseLite(seed + 100);
        _moistureNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        _moistureNoise.SetFrequency(2.0f);
    }

    public (double temperature, double moisture) CalculateClimate(
        Point position,
        double latitude,
        double elevation)
    {
        // Base temperature from latitude
        double baseTemp = 30.0 - Math.Abs(latitude) * 40.0; // [-10, 30]°C

        // Elevation lapse rate (-6.5°C per 1000m)
        double elevationEffect = -elevation * 0.065;

        // Noise variation
        float noiseTemp = _temperatureNoise.GetNoise((float)position.X, (float)position.Y);
        double tempVariation = noiseTemp * 5.0; // ±5°C

        double finalTemp = baseTemp + elevationEffect + tempVariation;

        // Moisture (higher near coast, varies with noise)
        float noiseMoisture = _moistureNoise.GetNoise((float)position.X, (float)position.Y);
        double moisture = (noiseMoisture + 1) * 0.5; // [0, 1]

        return (finalTemp, moisture);
    }
}
```

---

### 2. Biome-Specific Noise

```csharp
public byte[] GenerateDesertTerrain(int seed)
{
    var noise = new FastNoiseLite(seed);

    // Desert: Low-frequency dunes with cellular pits
    noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
    noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
    noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Sub);
    noise.SetFrequency(2.0f);

    // ... generate ...
}

public byte[] GenerateMountainTerrain(int seed)
{
    var noise = new FastNoiseLite(seed);

    // Mountains: Ridged fractal with high octaves
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
    noise.SetFractalOctaves(6);
    noise.SetFrequency(1.5f);

    // ... generate ...
}
```

---

### 3. Erosion Simulation (Simplified)

```csharp
public byte[] ApplySimpleErosion(byte[] heightmap, int width, int height, int iterations)
{
    var erosionNoise = new FastNoiseLite(42);
    erosionNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
    erosionNoise.SetFrequency(5.0f);

    for (int iter = 0; iter < iterations; iter++)
    {
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;

                // Find steepest descent
                int steepestIdx = idx;
                int maxDrop = 0;

                foreach (var (dx, dy) in Neighbors)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    int nIdx = ny * width + nx;

                    int drop = heightmap[idx] - heightmap[nIdx];
                    if (drop > maxDrop)
                    {
                        maxDrop = drop;
                        steepestIdx = nIdx;
                    }
                }

                // Erode proportional to slope + noise
                if (maxDrop > 0)
                {
                    float erosionFactor = erosionNoise.GetNoise(x, y);
                    int erosion = (int)(maxDrop * 0.1f * (erosionFactor + 1) * 0.5f);

                    heightmap[idx] = (byte)Math.Max(0, heightmap[idx] - erosion);
                    heightmap[steepestIdx] = (byte)Math.Min(100, heightmap[steepestIdx] + erosion);
                }
            }
        }
    }

    return heightmap;
}
```

---

## Testing

### Unit Tests

```csharp
using Xunit;
using FantasyMapGenerator.Core.Generators;

public class FastNoiseHeightmapGeneratorTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalHeightmaps()
    {
        int seed = 12345;
        var generator1 = new FastNoiseHeightmapGenerator(seed);
        var generator2 = new FastNoiseHeightmapGenerator(seed);

        var heightmap1 = generator1.Generate(100, 100, HeightmapProfile.Island);
        var heightmap2 = generator2.Generate(100, 100, HeightmapProfile.Island);

        Assert.Equal(heightmap1, heightmap2);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentHeightmaps()
    {
        var generator1 = new FastNoiseHeightmapGenerator(111);
        var generator2 = new FastNoiseHeightmapGenerator(222);

        var heightmap1 = generator1.Generate(100, 100, HeightmapProfile.Default);
        var heightmap2 = generator2.Generate(100, 100, HeightmapProfile.Default);

        Assert.NotEqual(heightmap1, heightmap2);
    }

    [Fact]
    public void IslandProfile_HasCoastalFalloff()
    {
        var generator = new FastNoiseHeightmapGenerator(42);
        var heightmap = generator.Generate(100, 100, HeightmapProfile.Island);

        // Check center is higher than edges
        byte centerHeight = heightmap[50 * 100 + 50];
        byte edgeHeight = heightmap[0 * 100 + 0];

        Assert.True(centerHeight > edgeHeight);
    }

    [Fact]
    public void GeneratedValues_AreInValidRange()
    {
        var generator = new FastNoiseHeightmapGenerator(999);
        var heightmap = generator.Generate(100, 100, HeightmapProfile.Default);

        Assert.All(heightmap, h => Assert.InRange(h, 0, 100));
    }
}
```

---

## Performance Considerations

### 1. Caching

```csharp
public class CachedNoiseGenerator
{
    private readonly Dictionary<(int, int, HeightmapProfile), byte[]> _cache = new();

    public byte[] Generate(int width, int height, int seed, HeightmapProfile profile)
    {
        var key = (seed, width * height, profile);

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var generator = new FastNoiseHeightmapGenerator(seed);
        var heightmap = generator.Generate(width, height, profile);

        _cache[key] = heightmap;
        return heightmap;
    }
}
```

### 2. Parallel Generation

```csharp
public byte[] GenerateParallel(int width, int height, HeightmapProfile profile)
{
    var heightmap = new byte[width * height];

    Parallel.For(0, height, y =>
    {
        for (int x = 0; x < width; x++)
        {
            float nx = x / (float)width;
            float ny = y / (float)height;

            float value = _baseNoise.GetNoise(nx * 1000, ny * 1000);
            // ... rest of calculation ...

            heightmap[y * width + x] = (byte)((value + 1) * 50);
        }
    });

    return heightmap;
}
```

**Warning**: FastNoiseLite is thread-safe for reading, but each thread should use its own instance if modifying state.

---

## Noise Type Reference

| Type | Best For | Characteristics |
|------|----------|-----------------|
| **OpenSimplex2** | General terrain | Smooth, organic, no directional artifacts |
| **Perlin** | Classic terrain | Smooth gradients, slight grid bias |
| **Value** | Soft hills | Very smooth, less detail |
| **Cellular** | Islands, craters | Discrete cells, sharp boundaries |
| **Cellular+Perlin** | Rocky terrain | Combines cell structure with smoothness |

---

## Fractal Type Reference

| Type | Best For | Formula |
|------|----------|---------|
| **FBm** | Natural terrain | `Σ (amplitude^i × noise(freq^i × x))` |
| **Ridged** | Mountains, veins | `1 - |noise(...)|` |
| **PingPong** | Terraces, cliffs | Bounces noise between min/max |

---

## Next Steps

1. **Add FastNoiseLite.cs** to project
2. **Implement FastNoiseHeightmapGenerator**
3. **Add UI toggle** for advanced noise (Avalonia UI)
4. **Test determinism** with reproducibility tests
5. **Compare visually** - screenshot template vs noise-based maps
6. **Optimize** - profile generation time, add caching if needed

---

## Resources

- [FastNoiseLite GitHub](https://github.com/Auburn/FastNoiseLite)
- [FastNoiseLite Documentation](https://github.com/Auburn/FastNoiseLite/wiki)
- [Interactive Noise Tool](https://auburn.github.io/FastNoiseLite/) (browser-based)
- [Noise Comparison Article](https://www.redblobgames.com/maps/terrain-from-noise/)

---

**Last Updated**: 2025-11-04
**Related**: library-adoption-roadmap.md, deterministic-seeding-guide.md
