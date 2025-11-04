# Deterministic Seeding Implementation Guide

## Overview

This guide details how to implement **fully deterministic random number generation** in the Fantasy Map Generator port, ensuring that the same seed produces identical maps across runs and platforms.

**Current State**: Mix of seeded `Random` and non-deterministic `Random.Shared`
**Target State**: All RNG threaded through generators, reproducible from seed, cross-platform consistent

---

## Why This Matters

### Current Problems

**Problem 1: Random.Shared Usage**
```csharp
// HeightmapGenerator.cs
public void AddNoise(byte[] heightmap, byte amplitude)
{
    var value = (byte)Random.Shared.Next(-amplitude, amplitude + 1); // ❌ Not seeded!
}

// BiomeGenerator.cs
private double CalculateTemperature(Cell cell, double equator, double mapHeight)
{
    double randomFactor = Random.Shared.NextDouble() * 0.1 - 0.05; // ❌ Different each run!
}
```

**Impact**: Same seed produces **different maps** on each run.

---

**Problem 2: Seed Precision Loss**
```csharp
// MapGenerator.cs
public MapData Generate(MapGenerationSettings settings)
{
    var random = new Random((int)settings.Seed); // ❌ long → int loses 32 bits!
}
```

**Impact**: Only 2^32 (~4 billion) unique seeds instead of 2^64.

---

**Problem 3: Platform Inconsistency**
`System.Random` implementation differs between:
- .NET Framework vs .NET Core
- Windows vs Linux vs macOS
- 32-bit vs 64-bit

**Impact**: Same seed produces **different maps** on different platforms.

---

### Goals

✅ **Reproducibility**: Same seed → same map, every time
✅ **Cross-platform**: Same results on Windows, Linux, macOS
✅ **Testability**: Validate generation logic with fixed seeds
✅ **Debuggability**: Reproduce user-reported bugs from seed
✅ **Full seed space**: Use all 64 bits of `long` seed

---

## Solution: RNG Abstraction + PCG

### Architecture

**1. Interface-Based Abstraction**
```
IRandomSource (interface)
    ↓
    ├── SystemRandomSource (System.Random wrapper - backwards compatible)
    └── PcgRandomSource (PCG implementation - cross-platform)
```

**2. Thread RNG Through All Generators**
```
MapGenerator (owns root RNG)
    ↓
    ├── HeightmapGenerator (terrain RNG)
    ├── BiomeGenerator (climate RNG)
    ├── HydrologyGenerator (river RNG)
    └── StateGenerator (political RNG)
```

Each subsystem gets its own **derived RNG** from different seed offsets, ensuring independence.

---

## Implementation

### Step 1: Create IRandomSource Interface

**File**: `src/FantasyMapGenerator.Core/Random/IRandomSource.cs`

```csharp
namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// Abstraction for random number generation
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a non-negative random integer
    /// </summary>
    int Next();

    /// <summary>
    /// Returns a non-negative random integer less than maxValue
    /// </summary>
    int Next(int maxValue);

    /// <summary>
    /// Returns a random integer within a specified range [minValue, maxValue)
    /// </summary>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// Returns a random floating-point number in [0.0, 1.0)
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Fills the elements of a byte array with random numbers
    /// </summary>
    void NextBytes(byte[] buffer);

    /// <summary>
    /// Returns a random floating-point number in [0.0, 1.0) (alias for NextDouble)
    /// </summary>
    float NextFloat() => (float)NextDouble();
}
```

---

### Step 2: Implement SystemRandomSource (Backwards Compatibility)

**File**: `src/FantasyMapGenerator.Core/Random/SystemRandomSource.cs`

```csharp
namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// Wrapper around System.Random for backwards compatibility
/// WARNING: Not cross-platform deterministic
/// </summary>
public class SystemRandomSource : IRandomSource
{
    private readonly System.Random _random;

    public SystemRandomSource(int seed)
    {
        _random = new System.Random(seed);
    }

    public int Next() => _random.Next();

    public int Next(int maxValue) => _random.Next(maxValue);

    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);

    public double NextDouble() => _random.NextDouble();

    public void NextBytes(byte[] buffer) => _random.NextBytes(buffer);
}
```

---

### Step 3: Implement PcgRandomSource (Cross-Platform)

**File**: `src/FantasyMapGenerator.Core/Random/PcgRandomSource.cs`

PCG (Permuted Congruential Generator) is a modern RNG with excellent statistical properties.

```csharp
namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// PCG (Permuted Congruential Generator) random number generator
/// Cross-platform deterministic, fast, and statistically robust
/// Based on PCG-XSH-RR variant
/// </summary>
public class PcgRandomSource : IRandomSource
{
    private ulong _state;
    private readonly ulong _increment;

    /// <summary>
    /// Initialize with a 64-bit seed
    /// </summary>
    public PcgRandomSource(ulong seed, ulong sequence = 0)
    {
        _state = 0;
        _increment = (sequence << 1) | 1; // Must be odd

        // Advance state
        Step();
        _state += seed;
        Step();
    }

    /// <summary>
    /// Initialize from signed long (convenience)
    /// </summary>
    public PcgRandomSource(long seed, ulong sequence = 0)
        : this(unchecked((ulong)seed), sequence)
    {
    }

    /// <summary>
    /// Generate next 32-bit random value
    /// </summary>
    private uint NextUInt32()
    {
        ulong oldState = _state;
        Step();

        // PCG-XSH-RR (XorShift high, Random Rotation)
        uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rot = (int)(oldState >> 59);

        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    private void Step()
    {
        // LCG: state = state * multiplier + increment
        _state = _state * 6364136223846793005UL + _increment;
    }

    public int Next()
    {
        // Return non-negative int
        return (int)(NextUInt32() >> 1);
    }

    public int Next(int maxValue)
    {
        if (maxValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be positive");
        }

        // Unbiased bounded random (avoids modulo bias)
        uint threshold = (uint)(-maxValue) % (uint)maxValue;

        while (true)
        {
            uint value = NextUInt32();

            if (value >= threshold)
            {
                return (int)(value % (uint)maxValue);
            }
        }
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be <= maxValue");
        }

        long range = (long)maxValue - minValue;

        if (range <= int.MaxValue)
        {
            return Next((int)range) + minValue;
        }

        // Large range - use double precision
        return (int)(NextDouble() * range) + minValue;
    }

    public double NextDouble()
    {
        // Generate 53-bit precision (double mantissa)
        uint high = NextUInt32() >> 5;  // 27 bits
        uint low = NextUInt32() >> 6;   // 26 bits

        ulong combined = ((ulong)high << 26) | low; // 53 bits

        return combined / (double)(1UL << 53);
    }

    public void NextBytes(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            if (i % 4 == 0)
            {
                uint value = NextUInt32();
                buffer[i] = (byte)value;
                if (i + 1 < buffer.Length) buffer[i + 1] = (byte)(value >> 8);
                if (i + 2 < buffer.Length) buffer[i + 2] = (byte)(value >> 16);
                if (i + 3 < buffer.Length) buffer[i + 3] = (byte)(value >> 24);
            }
        }
    }

    /// <summary>
    /// Create a child RNG with derived seed (for subsystems)
    /// </summary>
    public PcgRandomSource CreateChild(ulong offset)
    {
        // Generate derived seed using current state
        ulong childSeed = _state + offset;
        return new PcgRandomSource(childSeed, offset);
    }
}
```

**Why PCG?**
- ✅ Cross-platform deterministic
- ✅ Fast (competitive with System.Random)
- ✅ Passes all statistical tests (TestU01, PractRand)
- ✅ Minimal state (16 bytes)
- ✅ Supports multiple streams (for subsystems)

---

### Step 4: Update MapGenerationSettings

**File**: `src/FantasyMapGenerator.Core/Models/MapGenerationSettings.cs`

```csharp
public class MapGenerationSettings
{
    // ... existing properties ...

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
```

---

### Step 5: Update MapGenerator to Thread RNG

**File**: `src/FantasyMapGenerator.Core/MapGenerator.cs`

```csharp
using FantasyMapGenerator.Core.Random;

public class MapGenerator
{
    public MapData Generate(MapGenerationSettings settings)
    {
        // Create root RNG from seed
        var rootRng = settings.CreateRandom();

        // Create child RNGs for each subsystem (different streams)
        var terrainRng = CreateChildRng(rootRng, 1);
        var climateRng = CreateChildRng(rootRng, 2);
        var politicalRng = CreateChildRng(rootRng, 3);
        var hydrologyRng = CreateChildRng(rootRng, 4);

        var mapData = new MapData
        {
            Width = settings.Width,
            Height = settings.Height,
            CellsX = settings.CellsX,
            CellsY = settings.CellsY,
            CellsDesired = settings.CellsDesired,
            Scale = settings.Scale
        };

        // Generate with seeded RNGs
        GenerateRandomPoints(mapData, terrainRng);
        GenerateSimpleHeightmap(mapData, settings, terrainRng);
        CreateSimpleCells(mapData);
        ApplyHeightsToCells(mapData);

        GenerateBiomes(mapData, climateRng);
        GenerateRivers(mapData, hydrologyRng);
        GenerateBasicStates(mapData, settings, politicalRng);

        return mapData;
    }

    private IRandomSource CreateChildRng(IRandomSource parent, ulong offset)
    {
        // For PCG, use proper child creation
        if (parent is PcgRandomSource pcg)
        {
            return pcg.CreateChild(offset);
        }

        // For System.Random, use state-derived seed
        int childSeed = parent.Next();
        return new SystemRandomSource(childSeed);
    }

    private void GenerateRandomPoints(MapData mapData, IRandomSource random)
    {
        // Use GeometryUtils with passed RNG
        mapData.Points = GeometryUtils.GeneratePoissonDiskPoints(
            mapData.Width,
            mapData.Height,
            mapData.CellsDesired,
            random); // Pass RNG!
    }

    // ... other methods updated to take IRandomSource ...
}
```

---

### Step 6: Update HeightmapGenerator

**File**: `src/FantasyMapGenerator.Core/Generators/HeightmapGenerator.cs`

```csharp
using FantasyMapGenerator.Core.Random;

public class HeightmapGenerator
{
    // ... existing code ...

    // OLD: Used Random.Shared
    public void AddNoise(byte[] heightmap, byte amplitude)
    {
        var value = (byte)Random.Shared.Next(-amplitude, amplitude + 1); // ❌
    }

    // NEW: Takes IRandomSource parameter
    public void AddNoise(byte[] heightmap, byte amplitude, IRandomSource random)
    {
        for (int i = 0; i < heightmap.Length; i++)
        {
            int value = random.Next(-amplitude, amplitude + 1);
            heightmap[i] = (byte)Math.Clamp(heightmap[i] + value, 0, 100);
        }
    }

    // Update all methods similarly:
    public void AddRandomPeaks(byte[] heightmap, int count, byte height, IRandomSource random)
    {
        for (int i = 0; i < count; i++)
        {
            int x = random.Next(_width);
            int y = random.Next(_height);
            int idx = y * _width + x;

            heightmap[idx] = (byte)Math.Min(100, heightmap[idx] + height);
        }
    }

    public void AddRandomValleys(byte[] heightmap, int count, byte depth, IRandomSource random)
    {
        for (int i = 0; i < count; i++)
        {
            int x = random.Next(_width);
            int y = random.Next(_height);
            int idx = y * _width + x;

            heightmap[idx] = (byte)Math.Max(0, heightmap[idx] - depth);
        }
    }
}
```

---

### Step 7: Update BiomeGenerator

**File**: `src/FantasyMapGenerator.Core/Generators/BiomeGenerator.cs`

```csharp
using FantasyMapGenerator.Core.Random;

public class BiomeGenerator
{
    public void GenerateBiomes(MapData mapData, IRandomSource random)
    {
        CalculateTemperature(mapData, random);
        CalculatePrecipitation(mapData, random);
        AssignBiomes(mapData);
        SmoothBiomes(mapData);
    }

    private void CalculateTemperature(MapData mapData, IRandomSource random)
    {
        double equator = mapData.Height / 2.0;

        foreach (var cell in mapData.Cells)
        {
            // Base temperature from latitude
            double distanceFromEquator = Math.Abs(cell.Center.Y - equator);
            double baseTemp = 30.0 - (distanceFromEquator / equator) * 40.0;

            // Elevation effect
            double elevationEffect = -cell.Height * 0.5;

            // Random variation (now seeded!)
            double randomFactor = (random.NextDouble() * 0.1 - 0.05);

            cell.Temperature = baseTemp + elevationEffect + randomFactor;
        }
    }

    private void CalculatePrecipitation(MapData mapData, IRandomSource random)
    {
        foreach (var cell in mapData.Cells)
        {
            // Distance to ocean
            double distanceToWater = CalculateDistanceToWater(cell, mapData);

            // Base precipitation
            double basePrecip = Math.Max(0, 100 - distanceToWater * 0.5);

            // Random variation (now seeded!)
            double randomFactor = (random.NextDouble() * 20 - 10);

            cell.Precipitation = Math.Clamp(basePrecip + randomFactor, 0, 200);
        }
    }

    // ... rest of methods ...
}
```

---

### Step 8: Update GeometryUtils

**File**: `src/FantasyMapGenerator.Core/Geometry/GeometryUtils.cs`

```csharp
using FantasyMapGenerator.Core.Random;

public static class GeometryUtils
{
    // OLD: Used Random.Shared
    public static List<Point> GeneratePoissonDiskPoints(
        int width,
        int height,
        int targetCount)
    {
        var random = Random.Shared; // ❌
        // ...
    }

    // NEW: Takes IRandomSource parameter
    public static List<Point> GeneratePoissonDiskPoints(
        int width,
        int height,
        int targetCount,
        IRandomSource random)
    {
        // Estimate minimum distance from target count
        double area = width * height;
        double pointDensity = targetCount / area;
        double minDistance = 1.0 / Math.Sqrt(pointDensity);

        return GeneratePoissonDiskPoints(width, height, minDistance, random);
    }

    public static List<Point> GeneratePoissonDiskPoints(
        double width,
        double height,
        double minDistance,
        IRandomSource random)
    {
        var points = new List<Point>();
        var cellSize = minDistance / Math.Sqrt(2);
        var gridWidth = (int)Math.Ceiling(width / cellSize);
        var gridHeight = (int)Math.Ceiling(height / cellSize);
        var grid = new int[gridWidth * gridHeight];

        // Initialize grid with -1
        Array.Fill(grid, -1);

        var activeList = new List<Point>();

        // Start with random point
        var firstPoint = new Point(
            random.NextDouble() * width,
            random.NextDouble() * height);

        points.Add(firstPoint);
        activeList.Add(firstPoint);
        grid[GetGridIndex(firstPoint, cellSize, gridWidth)] = 0;

        // Generate points
        while (activeList.Count > 0)
        {
            int activeIndex = random.Next(activeList.Count);
            var activePoint = activeList[activeIndex];

            bool found = false;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                double radius = minDistance * (1 + random.NextDouble());

                var candidate = new Point(
                    activePoint.X + radius * Math.Cos(angle),
                    activePoint.Y + radius * Math.Sin(angle));

                if (IsValidPoint(candidate, width, height, minDistance, points, grid, cellSize, gridWidth))
                {
                    points.Add(candidate);
                    activeList.Add(candidate);
                    grid[GetGridIndex(candidate, cellSize, gridWidth)] = points.Count - 1;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                activeList.RemoveAt(activeIndex);
            }
        }

        return points;
    }

    // ... helper methods ...
}
```

---

## Testing Reproducibility

### Unit Tests

**File**: `tests/FantasyMapGenerator.Core.Tests/ReproducibilityTests.cs`

```csharp
using Xunit;
using FantasyMapGenerator.Core;
using FantasyMapGenerator.Core.Models;

public class ReproducibilityTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalMaps()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 1000,
            Height = 1000,
            CellsDesired = 1000,
            RandomAlgorithm = "PCG"
        };

        var generator = new MapGenerator();

        var map1 = generator.Generate(settings);
        var map2 = generator.Generate(settings);

        // Compare cell properties
        Assert.Equal(map1.Cells.Count, map2.Cells.Count);

        for (int i = 0; i < map1.Cells.Count; i++)
        {
            Assert.Equal(map1.Cells[i].Height, map2.Cells[i].Height);
            Assert.Equal(map1.Cells[i].Biome, map2.Cells[i].Biome);
            Assert.Equal(map1.Cells[i].Temperature, map2.Cells[i].Temperature);
            Assert.Equal(map1.Cells[i].Precipitation, map2.Cells[i].Precipitation);
            Assert.Equal(map1.Cells[i].State, map2.Cells[i].State);
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentMaps()
    {
        var settings1 = new MapGenerationSettings { Seed = 111 };
        var settings2 = new MapGenerationSettings { Seed = 222 };

        var generator = new MapGenerator();

        var map1 = generator.Generate(settings1);
        var map2 = generator.Generate(settings2);

        // Maps should differ
        bool hasDifference = false;

        for (int i = 0; i < Math.Min(map1.Cells.Count, map2.Cells.Count); i++)
        {
            if (map1.Cells[i].Height != map2.Cells[i].Height ||
                map1.Cells[i].Biome != map2.Cells[i].Biome)
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference, "Different seeds should produce different maps");
    }

    [Fact]
    public void PcgRng_IsDeterministic()
    {
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(42);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(rng1.Next(), rng2.Next());
        }
    }

    [Fact]
    public void PcgRng_ProducesDifferentSequences()
    {
        var rng1 = new PcgRandomSource(42);
        var rng2 = new PcgRandomSource(43);

        bool hasDifference = false;

        for (int i = 0; i < 100; i++)
        {
            if (rng1.Next() != rng2.Next())
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference);
    }

    [Theory]
    [InlineData(-9223372036854775808L)] // long.MinValue
    [InlineData(0L)]
    [InlineData(9223372036854775807L)]  // long.MaxValue
    public void PcgRng_HandlesFull64BitSeeds(long seed)
    {
        var rng = new PcgRandomSource(seed);

        // Should not crash
        for (int i = 0; i < 100; i++)
        {
            rng.Next();
        }
    }
}
```

---

### Benchmark Tests

**File**: `tests/FantasyMapGenerator.Core.Tests/RngBenchmarks.cs`

```csharp
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Random;

[MemoryDiagnoser]
public class RngBenchmarks
{
    private SystemRandomSource _systemRng;
    private PcgRandomSource _pcgRng;

    [GlobalSetup]
    public void Setup()
    {
        _systemRng = new SystemRandomSource(42);
        _pcgRng = new PcgRandomSource(42);
    }

    [Benchmark]
    public int SystemRandom_Next()
    {
        int sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _systemRng.Next();
        }
        return sum;
    }

    [Benchmark]
    public int PcgRandom_Next()
    {
        int sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _pcgRng.Next();
        }
        return sum;
    }

    [Benchmark]
    public double SystemRandom_NextDouble()
    {
        double sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _systemRng.NextDouble();
        }
        return sum;
    }

    [Benchmark]
    public double PcgRandom_NextDouble()
    {
        double sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _pcgRng.NextDouble();
        }
        return sum;
    }
}
```

**Expected Results**: PCG should be competitive with System.Random (within 10-20% performance).

---

## Migration Checklist

**Phase 1: Foundation**
- [ ] Create `IRandomSource` interface
- [ ] Implement `SystemRandomSource`
- [ ] Implement `PcgRandomSource`
- [ ] Add unit tests for RNG implementations
- [ ] Add benchmarks

**Phase 2: Integration**
- [ ] Update `MapGenerationSettings` to create RNG
- [ ] Update `MapGenerator` to thread RNG through generators
- [ ] Update `HeightmapGenerator` to take `IRandomSource`
- [ ] Update `BiomeGenerator` to take `IRandomSource`
- [ ] Update `GeometryUtils` to take `IRandomSource`
- [ ] Update any other methods using `Random.Shared`

**Phase 3: Validation**
- [ ] Add reproducibility tests
- [ ] Test cross-platform (Windows, Linux, macOS)
- [ ] Verify performance impact < 5%
- [ ] Update documentation

**Phase 4: UI Integration**
- [ ] Add RNG algorithm selector to UI (System vs PCG)
- [ ] Show seed in UI
- [ ] Add "Copy Seed" button
- [ ] Add seed history

---

## Usage Examples

### Generate Reproducible Map

```csharp
var settings = new MapGenerationSettings
{
    Seed = 123456789L,
    RandomAlgorithm = "PCG", // or "System"
    Width = 2000,
    Height = 2000,
    CellsDesired = 10000
};

var generator = new MapGenerator();
var map = generator.Generate(settings);

// Save seed with map
SaveMapWithSeed(map, settings.Seed);
```

### Reproduce User's Map

```csharp
// User reports bug with seed 987654321
var settings = new MapGenerationSettings
{
    Seed = 987654321L,
    // ... other settings from user ...
};

var map = generator.Generate(settings);

// Should produce exact same map as user saw
```

### Generate Map Variations

```csharp
long baseSeed = 42;

// Generate 10 variations
for (int i = 0; i < 10; i++)
{
    var settings = new MapGenerationSettings
    {
        Seed = baseSeed + i, // Different but related seeds
        // ... other settings ...
    };

    var map = generator.Generate(settings);
    ExportMap(map, $"map_variation_{i}.png");
}
```

---

## Advanced: Multiple RNG Streams

For complex generation, create independent RNG streams for different subsystems:

```csharp
public class AdvancedMapGenerator
{
    public MapData Generate(MapGenerationSettings settings)
    {
        var rootRng = new PcgRandomSource(settings.Seed);

        // Create independent streams
        var rngStreams = new Dictionary<string, IRandomSource>
        {
            ["terrain"] = rootRng.CreateChild(1),
            ["climate"] = rootRng.CreateChild(2),
            ["hydrology"] = rootRng.CreateChild(3),
            ["political"] = rootRng.CreateChild(4),
            ["cultural"] = rootRng.CreateChild(5),
            ["names"] = rootRng.CreateChild(6)
        };

        var map = new MapData();

        GenerateTerrain(map, rngStreams["terrain"]);
        GenerateClimate(map, rngStreams["climate"]);
        GenerateRivers(map, rngStreams["hydrology"]);
        GenerateStates(map, rngStreams["political"]);
        GenerateCultures(map, rngStreams["cultural"]);
        GenerateNames(map, rngStreams["names"]);

        return map;
    }
}
```

**Benefit**: Can regenerate just one aspect (e.g., names) without affecting others.

---

## Resources

- [PCG Random](https://www.pcg-random.org/) - Original PCG paper and implementations
- [PCG C# Implementation](https://github.com/igiagkiozis/PCGSharp) - Alternative C# port
- [Random Number Testing](https://www.pcg-random.org/statistical-tests.html) - Statistical test suite
- [Cross-Platform RNG Discussion](https://github.com/dotnet/runtime/issues/23198)

---

**Last Updated**: 2025-11-04
**Related**: library-adoption-roadmap.md, noise-generation-guide.md
