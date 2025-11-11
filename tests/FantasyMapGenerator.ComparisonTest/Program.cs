using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Processing;
using FantasyMapGenerator.Rendering;
using SkiaSharp;

namespace FantasyMapGenerator;

/// <summary>
/// Demonstrates side-by-side comparison of different rendering approaches
/// </summary>
public class TestRenderingComparison
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Fantasy Map Generator: Rendering Comparison ===\n");
        Console.WriteLine("This demo compares rendering approaches:");
        Console.WriteLine("1. Discrete Voronoi (Sharp cell boundaries)");
        Console.WriteLine("2. OLD Smooth Interpolation (IDW - failed approach)");
        Console.WriteLine("3. NEW Raster Blur (Canvas → Blur - proven approach)\n");

        // Generate a test map
        Console.WriteLine("Generating test map...");
        var mapData = GenerateTestMap();
        Console.WriteLine($"Map generated: {mapData.Cells.Count} cells\n");

        // Test 1: Discrete Voronoi Renderer (Skipped - requires Voronoi fix)
        // Console.WriteLine("=== Test 1: Discrete Voronoi Renderer ===");
        // TestDiscreteRenderer(mapData);

        // Test 2: OLD Smooth Interpolated Renderer (for comparison)
        Console.WriteLine("\n=== Test 2: OLD Smooth Interpolated Renderer (IDW) ===");
        TestSmoothRenderer(mapData);

        // Test 3: NEW Raster Blur Renderer (the correct approach!)
        Console.WriteLine("\n=== Test 3: NEW Raster Blur Renderer ===");
        TestRasterBlurRenderer(mapData);

        // Test 4: Raster Blur with Height Pre-smoothing
        Console.WriteLine("\n=== Test 4: Raster Blur + Height Pre-smoothing ===");
        TestRasterBlurWithPreSmoothing(mapData);

        // Test 5: Enhanced Smooth Renderer with Color Schemes (old approach)
        Console.WriteLine("\n=== Test 5: OLD Enhanced Renderer (Multiple Color Schemes) ===");
        TestEnhancedRenderer(mapData);

        Console.WriteLine("\n=== Comparison Complete ===");
        Console.WriteLine("\nOutput files created:");
        Console.WriteLine("  OLD APPROACH:");
        Console.WriteLine("  - smooth_basic.png           (Basic smooth rendering - IDW)");
        Console.WriteLine("  - smooth_classic.png         (Classic color scheme - IDW)");
        Console.WriteLine("  - smooth_realistic.png       (Realistic color scheme - IDW)");
        Console.WriteLine("  - smooth_vibrant.png         (Vibrant color scheme - IDW)");
        Console.WriteLine("\n  NEW APPROACH:");
        Console.WriteLine("  - raster_blur_default.png    (Raster blur - default settings)");
        Console.WriteLine("  - raster_blur_strong.png     (Raster blur - strong blur)");
        Console.WriteLine("  - raster_blur_classic.png    (Raster blur - classic colors)");
        Console.WriteLine("  - raster_blur_presmooth.png  (Raster blur + height smoothing)");
    }

    private static MapData GenerateTestMap()
    {
        var settings = new MapGenerationSettings
        {
            Width = 1000,
            Height = 800,
            NumPoints = 800,
            Seed = 12345,
            HeightmapTemplate = "island",
            UseAdvancedNoise = true
        };

        var generator = new MapGenerator();
        return generator.Generate(settings);
    }

    private static void TestDiscreteRenderer(MapData mapData)
    {
        Console.WriteLine("  Rendering with discrete Voronoi polygons...");

        var settings = new MapRenderSettings
        {
            BackgroundColor = new SKColor(240, 240, 230),
            WaterColor = new SKColor(60, 120, 200),
            BeachColor = new SKColor(220, 200, 160),
            PlainsColor = new SKColor(80, 140, 60),
            HillsColor = new SKColor(140, 160, 100),
            MountainColor = new SKColor(120, 100, 80)
        };

        using var renderer = new MapRenderer(settings);
        using var surface = renderer.RenderMap(mapData, 1200, 900);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        using var file = File.OpenWrite("discrete_voronoi.png");
        data.SaveTo(file);

        Console.WriteLine("  ✓ Saved: discrete_voronoi.png");
        Console.WriteLine("    Shows distinct cellular polygons (Voronoi cells)");
        Console.WriteLine("    Best for: TUI/Braille rendering, functional display");
    }

    private static void TestSmoothRenderer(MapData mapData)
    {
        Console.WriteLine("  Rendering with smooth interpolation...");

        var settings = new MapRenderSettings
        {
            BackgroundColor = SKColors.White,
            WaterColor = new SKColor(60, 120, 200),
            BeachColor = new SKColor(220, 200, 160),
            PlainsColor = new SKColor(80, 140, 60),
            HillsColor = new SKColor(140, 160, 100),
            MountainColor = new SKColor(120, 100, 80)
        };

        using var renderer = new SmoothTerrainRenderer(settings);
        using var surface = renderer.RenderSmoothTerrain(mapData, 1200, 900);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        using var file = File.OpenWrite("smooth_basic.png");
        data.SaveTo(file);

        Console.WriteLine("  ✓ Saved: smooth_basic.png");
        Console.WriteLine("    Shows smooth, flowing terrain boundaries");
        Console.WriteLine("    Best for: GUI applications, artistic visualization");
    }

    private static void TestEnhancedRenderer(MapData mapData)
    {
        var schemes = new[]
        {
            ("smooth_classic.png", TerrainColorSchemes.Classic, "Classic fantasy map style"),
            ("smooth_realistic.png", TerrainColorSchemes.Realistic, "Realistic earth tones"),
            ("smooth_vibrant.png", TerrainColorSchemes.Vibrant, "Bold, vibrant colors"),
            ("smooth_parchment.png", TerrainColorSchemes.Parchment, "Sepia/old map style"),
            ("smooth_dark_fantasy.png", TerrainColorSchemes.DarkFantasy, "Dark, atmospheric tones"),
            ("smooth_watercolor.png", TerrainColorSchemes.Watercolor, "Soft watercolor style")
        };

        foreach (var (filename, scheme, description) in schemes)
        {
            Console.WriteLine($"  Rendering: {scheme.Name}");

            var settings = new SmoothTerrainRenderSettings
            {
                ColorScheme = scheme,
                UseGradients = true,
                AntiAlias = true,
                BackgroundColor = SKColors.White
            };
            settings.ApplyQualityPreset(SmoothTerrainRenderSettings.QualityPreset.High);

            using var renderer = new SimpleSmoothRenderer(settings);
            using var surface = renderer.RenderMap(mapData, 1200, 900);
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var file = File.OpenWrite(filename);
            data.SaveTo(file);

            Console.WriteLine($"    ✓ Saved: {filename}");
            Console.WriteLine($"      {description}");
        }
    }

    private static void TestRasterBlurRenderer(MapData mapData)
    {
        // Test with different blur strengths
        var tests = new[]
        {
            ("raster_blur_default.png", 2.0f, TerrainColorSchemes.Classic, "Default blur (sigma=2.0)"),
            ("raster_blur_strong.png", 4.0f, TerrainColorSchemes.Classic, "Strong blur (sigma=4.0)"),
            ("raster_blur_classic.png", 2.5f, TerrainColorSchemes.Classic, "Classic colors with moderate blur"),
            ("raster_blur_vibrant.png", 2.5f, TerrainColorSchemes.Vibrant, "Vibrant colors with moderate blur")
        };

        foreach (var (filename, blurSigma, colorScheme, description) in tests)
        {
            Console.WriteLine($"  Rendering: {description}");

            using var renderer = new RasterBlurRenderer(colorScheme, blurSigma, antiAlias: true);
            renderer.RenderToFile(mapData, 1200, 900, filename);

            Console.WriteLine($"    ✓ Saved: {filename}");
            Console.WriteLine($"      {description}");
        }
    }

    private static void TestRasterBlurWithPreSmoothing(MapData mapData)
    {
        // Clone map data so we don't modify the original
        var clonedMapData = CloneMapData(mapData);

        Console.WriteLine("  Pre-smoothing heights (3 iterations, strength=0.5)...");
        var smoother = new HeightSmoother();
        smoother.SmoothHeights(clonedMapData, iterations: 3, strength: 0.5);

        Console.WriteLine("  Rendering with raster blur...");
        using var renderer = new RasterBlurRenderer(TerrainColorSchemes.Classic, blurSigma: 2.0f, antiAlias: true);
        renderer.RenderToFile(clonedMapData, 1200, 900, "raster_blur_presmooth.png");

        Console.WriteLine("    ✓ Saved: raster_blur_presmooth.png");
        Console.WriteLine("      Combines height pre-smoothing + raster blur for ultra-smooth results");
    }

    private static MapData CloneMapData(MapData original)
    {
        // Simple clone for testing - just copy the essential data
        var clone = new MapData(original.Width, original.Height, original.CellsDesired)
        {
            Points = new List<Point>(original.Points),
            Biomes = original.Biomes,
            Burgs = original.Burgs,
            States = original.States,
            Cultures = original.Cultures,
            Rivers = original.Rivers
        };

        // Deep clone cells since we'll modify heights
        foreach (var cell in original.Cells)
        {
            clone.Cells.Add(new Cell(cell.Id, cell.Center)
            {
                Height = cell.Height,
                Biome = cell.Biome,
                Culture = cell.Culture,
                State = cell.State,
                Province = cell.Province,
                Burg = cell.Burg,
                Neighbors = new List<int>(cell.Neighbors),
                Vertices = new List<int>(cell.Vertices),
                IsBorder = cell.IsBorder
            });
        }

        return clone;
    }
}
