using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Settings for map rendering including colors and styles
/// </summary>
public class MapRenderSettings
{
    /// <summary>
    /// Background color of the map
    /// </summary>
    public SKColor BackgroundColor { get; set; } = SKColors.LightGray;
    
    /// <summary>
    /// Color for water/ocean areas
    /// </summary>
    public SKColor WaterColor { get; set; } = new SKColor(64, 164, 223);
    
    /// <summary>
    /// Color for beach/coastal areas
    /// </summary>
    public SKColor BeachColor { get; set; } = new SKColor(238, 203, 173);
    
    /// <summary>
    /// Color for plains/grasslands
    /// </summary>
    public SKColor PlainsColor { get; set; } = new SKColor(134, 168, 95);
    
    /// <summary>
    /// Color for hills
    /// </summary>
    public SKColor HillsColor { get; set; } = new SKColor(158, 154, 135);
    
    /// <summary>
    /// Color for mountains
    /// </summary>
    public SKColor MountainColor { get; set; } = new SKColor(139, 137, 137);
    
    /// <summary>
    /// Color for forests
    /// </summary>
    public SKColor ForestColor { get; set; } = new SKColor(34, 139, 34);
    
    /// <summary>
    /// Color for desert areas
    /// </summary>
    public SKColor DesertColor { get; set; } = new SKColor(238, 203, 173);
    
    /// <summary>
    /// Color for tundra/ice areas
    /// </summary>
    public SKColor TundraColor { get; set; } = new SKColor(207, 207, 207);
    
    /// <summary>
    /// Whether to show grid lines
    /// </summary>
    public bool ShowGrid { get; set; } = false;
    
    /// <summary>
    /// Whether to show cell borders
    /// </summary>
    public bool ShowCellBorders { get; set; } = false;
    
    /// <summary>
    /// Whether to show heightmap shading
    /// </summary>
    public bool ShowHeightShading { get; set; } = true;
    
    /// <summary>
    /// Whether to show terrain features
    /// </summary>
    public bool ShowTerrain { get; set; } = true;
    
    /// <summary>
    /// Whether to show coastlines
    /// </summary>
    public bool ShowCoastlines { get; set; } = true;
    
    /// <summary>
    /// Whether to show political borders
    /// </summary>
    public bool ShowBorders { get; set; } = true;
    
    /// <summary>
    /// Whether to show cities and towns
    /// </summary>
    public bool ShowCities { get; set; } = true;
    
    /// <summary>
    /// Whether to show text labels
    /// </summary>
    public bool ShowLabels { get; set; } = true;
    
    /// <summary>
    /// Line width for borders
    /// </summary>
    public float BorderWidth { get; set; } = 1.0f;
    
    /// <summary>
    /// Line width for rivers
    /// </summary>
    public float RiverWidth { get; set; } = 2.0f;
    
    /// <summary>
    /// Font size for labels
    /// </summary>
    public float LabelFontSize { get; set; } = 12.0f;
    
    /// <summary>
    /// Font family for labels
    /// </summary>
    public string LabelFontFamily { get; set; } = "Arial";
    
    /// <summary>
    /// Creates a copy of these settings
    /// </summary>
    public MapRenderSettings Clone()
    {
        return new MapRenderSettings
        {
            BackgroundColor = BackgroundColor,
            WaterColor = WaterColor,
            BeachColor = BeachColor,
            PlainsColor = PlainsColor,
            HillsColor = HillsColor,
            MountainColor = MountainColor,
            ForestColor = ForestColor,
            DesertColor = DesertColor,
            TundraColor = TundraColor,
            ShowGrid = ShowGrid,
            ShowCellBorders = ShowCellBorders,
            ShowHeightShading = ShowHeightShading,
            ShowTerrain = ShowTerrain,
            ShowCoastlines = ShowCoastlines,
            ShowBorders = ShowBorders,
            ShowCities = ShowCities,
            ShowLabels = ShowLabels,
            BorderWidth = BorderWidth,
            RiverWidth = RiverWidth,
            LabelFontSize = LabelFontSize,
            LabelFontFamily = LabelFontFamily
        };
    }
}