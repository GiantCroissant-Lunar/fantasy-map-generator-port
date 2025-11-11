using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Defines color schemes for terrain rendering
/// </summary>
public class TerrainColorScheme
{
    public string Name { get; set; } = "Default";
    public List<(double heightThreshold, SKColor color, string name)> Layers { get; set; } = new();

    /// <summary>
    /// Gets color for a specific height value using interpolation
    /// </summary>
    public SKColor GetColorForHeight(double height)
    {
        if (Layers.Count == 0)
            return SKColors.Gray;

        // Find the two layers to interpolate between
        var lower = Layers[0];
        var upper = Layers[^1];

        for (int i = 0; i < Layers.Count - 1; i++)
        {
            if (height >= Layers[i].heightThreshold && height < Layers[i + 1].heightThreshold)
            {
                lower = Layers[i];
                upper = Layers[i + 1];
                break;
            }
        }

        if (height <= lower.heightThreshold)
            return lower.color;
        if (height >= upper.heightThreshold)
            return upper.color;

        // Interpolate between colors
        float t = (float)((height - lower.heightThreshold) / (upper.heightThreshold - lower.heightThreshold));
        return InterpolateColor(lower.color, upper.color, t);
    }

    /// <summary>
    /// Interpolates between two colors
    /// </summary>
    private static SKColor InterpolateColor(SKColor c1, SKColor c2, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(c1.Red + (c2.Red - c1.Red) * t),
            (byte)(c1.Green + (c2.Green - c1.Green) * t),
            (byte)(c1.Blue + (c2.Blue - c1.Blue) * t),
            (byte)(c1.Alpha + (c2.Alpha - c1.Alpha) * t)
        );
    }
}

/// <summary>
/// Provides pre-defined terrain color schemes
/// </summary>
public static class TerrainColorSchemes
{
    /// <summary>
    /// Classic fantasy map style with distinct elevation bands
    /// </summary>
    public static TerrainColorScheme Classic => new()
    {
        Name = "Classic",
        Layers = new()
        {
            (0, new SKColor(25, 50, 120), "Deep Ocean"),          // Dark blue
            (10, new SKColor(40, 80, 160), "Ocean"),              // Medium blue
            (20, new SKColor(60, 120, 200), "Shallow Water"),     // Light blue
            (25, new SKColor(220, 200, 160), "Beach"),            // Sandy
            (30, new SKColor(80, 140, 60), "Lowlands"),           // Light green
            (45, new SKColor(60, 120, 50), "Plains"),             // Medium green
            (60, new SKColor(140, 160, 100), "Hills"),            // Yellow-green
            (75, new SKColor(120, 100, 80), "Mountains"),         // Brown
            (90, new SKColor(160, 160, 160), "High Mountains"),   // Gray
            (100, new SKColor(240, 240, 250), "Peaks"),           // White/snow
        }
    };

    /// <summary>
    /// Realistic style with natural earth tones
    /// </summary>
    public static TerrainColorScheme Realistic => new()
    {
        Name = "Realistic",
        Layers = new()
        {
            (0, new SKColor(15, 30, 80), "Deep Ocean"),
            (10, new SKColor(25, 55, 130), "Ocean"),
            (20, new SKColor(45, 100, 170), "Coastal Waters"),
            (22, new SKColor(180, 170, 130), "Beach"),
            (25, new SKColor(95, 155, 80), "Coastal Lowlands"),
            (35, new SKColor(105, 145, 75), "Lowlands"),
            (50, new SKColor(125, 135, 70), "Plains"),
            (65, new SKColor(140, 125, 80), "Hills"),
            (80, new SKColor(110, 90, 70), "Mountains"),
            (92, new SKColor(130, 130, 135), "High Peaks"),
            (100, new SKColor(250, 250, 255), "Snow"),
        }
    };

    /// <summary>
    /// Vibrant artistic style with bold colors
    /// </summary>
    public static TerrainColorScheme Vibrant => new()
    {
        Name = "Vibrant",
        Layers = new()
        {
            (0, new SKColor(0, 30, 100), "Abyss"),
            (15, new SKColor(20, 80, 200), "Deep Sea"),
            (20, new SKColor(50, 150, 255), "Sea"),
            (25, new SKColor(255, 220, 140), "Shores"),
            (30, new SKColor(60, 200, 80), "Grasslands"),
            (50, new SKColor(100, 180, 50), "Fields"),
            (65, new SKColor(200, 200, 80), "Highlands"),
            (80, new SKColor(180, 140, 80), "Foothills"),
            (92, new SKColor(140, 120, 140), "Mountains"),
            (100, new SKColor(255, 255, 255), "Summits"),
        }
    };

    /// <summary>
    /// Parchment/old map style with sepia tones
    /// </summary>
    public static TerrainColorScheme Parchment => new()
    {
        Name = "Parchment",
        Layers = new()
        {
            (0, new SKColor(140, 130, 100), "Deep Waters"),
            (15, new SKColor(160, 145, 110), "Waters"),
            (20, new SKColor(175, 160, 125), "Shallows"),
            (25, new SKColor(210, 195, 155), "Coast"),
            (35, new SKColor(215, 200, 160), "Lowlands"),
            (55, new SKColor(205, 185, 145), "Plains"),
            (70, new SKColor(190, 170, 130), "Hills"),
            (85, new SKColor(170, 150, 115), "Mountains"),
            (100, new SKColor(150, 135, 105), "Peaks"),
        }
    };

    /// <summary>
    /// Dark fantasy style with muted, atmospheric colors
    /// </summary>
    public static TerrainColorScheme DarkFantasy => new()
    {
        Name = "Dark Fantasy",
        Layers = new()
        {
            (0, new SKColor(10, 15, 25), "Void"),
            (10, new SKColor(15, 25, 40), "Dark Depths"),
            (20, new SKColor(25, 40, 60), "Murky Waters"),
            (25, new SKColor(60, 55, 50), "Bleak Shore"),
            (30, new SKColor(45, 60, 40), "Dark Woods"),
            (50, new SKColor(55, 65, 50), "Wasteland"),
            (70, new SKColor(70, 70, 65), "Barren Hills"),
            (85, new SKColor(60, 60, 60), "Cursed Mountains"),
            (100, new SKColor(80, 80, 85), "Peaks of Despair"),
        }
    };

    /// <summary>
    /// Watercolor style with soft, blended colors
    /// </summary>
    public static TerrainColorScheme Watercolor => new()
    {
        Name = "Watercolor",
        Layers = new()
        {
            (0, new SKColor(90, 130, 180, 200), "Deep Blue"),
            (15, new SKColor(120, 160, 200, 210), "Azure"),
            (20, new SKColor(150, 190, 220, 220), "Light Blue"),
            (25, new SKColor(240, 220, 180, 200), "Sand"),
            (35, new SKColor(140, 190, 130, 200), "Meadow"),
            (55, new SKColor(120, 170, 110, 210), "Green"),
            (70, new SKColor(180, 170, 120, 200), "Olive"),
            (85, new SKColor(160, 140, 120, 210), "Umber"),
            (100, new SKColor(220, 220, 230, 220), "White"),
        }
    };

    /// <summary>
    /// Gets all available color schemes
    /// </summary>
    public static List<TerrainColorScheme> All => new()
    {
        Classic,
        Realistic,
        Vibrant,
        Parchment,
        DarkFantasy,
        Watercolor
    };

    /// <summary>
    /// Gets a color scheme by name
    /// </summary>
    public static TerrainColorScheme? GetByName(string name)
    {
        return All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extended render settings that include color scheme support
/// </summary>
public class SmoothTerrainRenderSettings
{
    public TerrainColorScheme ColorScheme { get; set; } = TerrainColorSchemes.Classic;
    public bool UseGradients { get; set; } = true;
    public float SmoothingFactor { get; set; } = 0.6f;
    public int InterpolationGridSize { get; set; } = 800;
    public bool AntiAlias { get; set; } = true;
    public SKColor BackgroundColor { get; set; } = SKColors.White;

    /// <summary>
    /// Quality preset
    /// </summary>
    public enum QualityPreset
    {
        Draft,      // Fast, lower quality
        Normal,     // Balanced
        High,       // Slow, high quality
        Ultra       // Very slow, maximum quality
    }

    /// <summary>
    /// Applies a quality preset
    /// </summary>
    public void ApplyQualityPreset(QualityPreset preset)
    {
        switch (preset)
        {
            case QualityPreset.Draft:
                InterpolationGridSize = 400;
                SmoothingFactor = 0.4f;
                AntiAlias = false;
                break;
            case QualityPreset.Normal:
                InterpolationGridSize = 800;
                SmoothingFactor = 0.6f;
                AntiAlias = true;
                break;
            case QualityPreset.High:
                InterpolationGridSize = 1200;
                SmoothingFactor = 0.7f;
                AntiAlias = true;
                break;
            case QualityPreset.Ultra:
                InterpolationGridSize = 2000;
                SmoothingFactor = 0.8f;
                AntiAlias = true;
                break;
        }
    }
}
