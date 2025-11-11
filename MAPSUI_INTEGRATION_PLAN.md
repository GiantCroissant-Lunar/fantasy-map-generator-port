# Mapsui Integration Plan for Fantasy Map Generator

## Executive Summary

This document outlines the plan to integrate **Mapsui** with **BruTile** and implement the **Raster Blur** rendering approach to achieve smooth, professional fantasy map visualization with interactive pan/zoom capabilities.

## Current State Analysis

### Existing Architecture

```
┌─────────────────────────────────────────────────────┐
│  FantasyMapGenerator.UI (Avalonia)                  │
│  ├── MapControl (Simple Image Display)              │
│  ├── MapControlViewModel                            │
│  └── MainWindow                                      │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│  FantasyMapGenerator.Rendering                      │
│  ├── MapRenderer (Vector → Direct Raster)           │
│  ├── SmoothTerrainRenderer (Failed IDW Approach)    │
│  ├── EnhancedSmoothRenderer                         │
│  └── SimpleSmoothRenderer                           │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│  FantasyMapGenerator.Core                           │
│  ├── MapData (Voronoi cells, heightmap, biomes)     │
│  ├── Voronoi (NetTopologySuite)                     │
│  └── MapGenerator                                    │
└─────────────────────────────────────────────────────┘
```

### Current Problems

1. **No smooth terrain transitions** - Voronoi cells have sharp boundaries
2. **Failed IDW interpolation** - Attempted vector space smoothing didn't work
3. **Simple image display** - No pan/zoom/rotate capabilities
4. **Missing canvas blur step** - Not following original's proven approach
5. **Poor UX** - Static image with no interactivity

### Key Insight from Analysis

The original Fantasy Map Generator's secret:
```
Voronoi Cells → Rasterize to Canvas → Apply Gaussian Blur → Smooth Map
```

We tried:
```
Voronoi Cells → IDW Interpolation → Render → Artifacts
```

## Proposed Architecture

### Three-Tier Approach

```
┌──────────────────────────────────────────────────────────────────┐
│  TIER 1: Interactive Map Viewer (Mapsui)                         │
│  ├── Pan/Zoom/Rotate controls                                    │
│  ├── Layer management                                            │
│  ├── Export functionality                                        │
│  └── Measure tools                                               │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│  TIER 2: Tile Generation (BruTile + Custom Provider)            │
│  ├── Generate tiles at different zoom levels                    │
│  ├── Cache tiles for performance                                │
│  ├── Custom FantasyMapTileProvider                              │
│  └── Coordinate transformation (pixel space)                    │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│  TIER 3: Raster Blur Rendering (SkiaSharp)                      │
│  ├── Render Voronoi cells to raster (pixel-by-pixel)            │
│  ├── Apply Gaussian blur filter                                 │
│  ├── Generate smooth terrain tiles                              │
│  └── Optional: Pre-smooth heights                               │
└──────────────────────────────────────────────────────────────────┘
```

## Implementation Phases

### Phase 1: Implement Raster Blur Renderer (Foundation)

**Goal**: Fix the smoothing issue by rendering to raster first, then applying blur.

**Files to Create/Modify**:
- `src/FantasyMapGenerator.Rendering/RasterBlurRenderer.cs` (NEW)
- `src/FantasyMapGenerator.Rendering/HeightSmoother.cs` (NEW)

**Implementation Steps**:

1. **Create RasterBlurRenderer**:
```csharp
public class RasterBlurRenderer
{
    // Step 1: Render Voronoi cells to bitmap
    private SKBitmap RenderVoronoiToRaster(MapData mapData, int width, int height)
    {
        var bitmap = new SKBitmap(width, height);

        // For each pixel, find nearest Voronoi cell and set color
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                var worldPos = PixelToWorld(x, y, width, height, mapData);
                var cellIndex = FindNearestCell(mapData, worldPos);
                var cell = mapData.Cells[cellIndex];
                var color = GetTerrainColor(cell.Height, cell.Biome);
                bitmap.SetPixel(x, y, color);
            }
        });

        return bitmap;
    }

    // Step 2: Apply Gaussian blur
    public SKSurface RenderMap(MapData mapData, int width, int height, float blurSigma = 2.0f)
    {
        // Render to raster
        using var bitmap = RenderVoronoiToRaster(mapData, width, height);

        // Apply blur filter
        using var blurFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
        using var paint = new SKPaint { ImageFilter = blurFilter };

        // Create final surface with blurred result
        var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.DrawBitmap(bitmap, 0, 0, paint);

        return surface;
    }
}
```

2. **Add spatial index for fast cell lookup**:
```csharp
// Use KD-Tree or R-Tree for O(log n) nearest cell search
private int FindNearestCell(MapData mapData, Point worldPos)
{
    // TODO: Implement spatial index (KD-Tree)
    // For now, brute force (optimize later)
    return mapData.Cells
        .Select((cell, index) => (index, dist: Distance(cell.Center, worldPos)))
        .OrderBy(x => x.dist)
        .First().index;
}
```

3. **Test the new renderer**:
   - Create comparison test: Old vs New renderer
   - Verify smooth transitions
   - Tune blur sigma parameter (1.0 - 4.0)

**Expected Outcome**: Beautiful smooth terrain transitions matching the original JavaScript version.

**Estimated Time**: 2-3 days

---

### Phase 2: Implement Height Pre-Smoothing (Enhancement)

**Goal**: Optionally smooth height values before rendering for even better results.

**Files to Create**:
- `src/FantasyMapGenerator.Core/HeightSmoother.cs` (NEW)

**Implementation**:

```csharp
public class HeightSmoother
{
    public void SmoothHeights(MapData mapData, int iterations = 3)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            var newHeights = new byte[mapData.Cells.Count];

            Parallel.For(0, mapData.Cells.Count, i =>
            {
                var cell = mapData.Cells[i];
                var neighbors = GetNeighborCells(mapData, i);

                // Weighted average with neighbors
                var totalWeight = 1.0;
                var totalHeight = (double)cell.Height;

                foreach (var neighbor in neighbors)
                {
                    totalHeight += neighbor.Height;
                    totalWeight += 1.0;
                }

                newHeights[i] = (byte)Math.Clamp(totalHeight / totalWeight, 0, 100);
            });

            // Apply smoothed heights
            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                mapData.Cells[i].Height = newHeights[i];
            }
        }
    }
}
```

**Usage**:
```csharp
// Optional: Pre-smooth heights before rendering
var smoother = new HeightSmoother();
smoother.SmoothHeights(mapData, iterations: 2);

// Then render with raster blur
var renderer = new RasterBlurRenderer();
var surface = renderer.RenderMap(mapData, width, height, blurSigma: 2.0f);
```

**Estimated Time**: 1 day

---

### Phase 3: Create Custom Mapsui Tile Provider

**Goal**: Generate map tiles on-demand for Mapsui's tile-based rendering system.

**NuGet Packages to Add**:
```xml
<PackageReference Include="BruTile" Version="5.0.0" />
<PackageReference Include="Mapsui" Version="5.0.0" />
<PackageReference Include="Mapsui.Rendering.Skia" Version="5.0.0" />
```

**Files to Create**:
- `src/FantasyMapGenerator.Rendering/FantasyMapTileProvider.cs` (NEW)
- `src/FantasyMapGenerator.Rendering/FantasyMapTileSource.cs` (NEW)

**Implementation**:

1. **Create Tile Provider**:

```csharp
public class FantasyMapTileProvider : ITileProvider
{
    private readonly MapData _mapData;
    private readonly RasterBlurRenderer _renderer;
    private readonly ConcurrentDictionary<TileIndex, byte[]> _tileCache;
    private readonly int _tileSize = 256; // Standard tile size

    public FantasyMapTileProvider(MapData mapData)
    {
        _mapData = mapData;
        _renderer = new RasterBlurRenderer();
        _tileCache = new ConcurrentDictionary<TileIndex, byte[]>();
    }

    public byte[] GetTile(TileInfo tileInfo)
    {
        var tileIndex = tileInfo.Index;

        // Check cache first
        if (_tileCache.TryGetValue(tileIndex, out var cachedTile))
            return cachedTile;

        // Generate tile
        var tile = GenerateTile(tileIndex);

        // Cache it
        _tileCache[tileIndex] = tile;

        return tile;
    }

    private byte[] GenerateTile(TileIndex tileIndex)
    {
        // Calculate tile bounds in world coordinates
        var bounds = GetTileBounds(tileIndex);

        // Extract relevant cells for this tile
        var tileCells = GetCellsInBounds(_mapData, bounds);

        // Render tile with raster blur
        using var surface = _renderer.RenderMapRegion(
            _mapData, tileCells, _tileSize, _tileSize);

        // Encode as PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private MRect GetTileBounds(TileIndex tileIndex)
    {
        // BruTile uses Web Mercator by default
        // We'll use a custom coordinate system (pixel space)
        var tileSize = _tileSize;
        var x = tileIndex.Col * tileSize;
        var y = tileIndex.Row * tileSize;

        return new MRect(x, y, x + tileSize, y + tileSize);
    }
}
```

2. **Create Custom Tile Schema**:

```csharp
public class FantasyMapTileSchema : TileSchema
{
    public FantasyMapTileSchema(int mapWidth, int mapHeight)
    {
        Name = "FantasyMapSchema";
        Srs = "PixelSpace"; // Custom coordinate system

        // Calculate zoom levels based on map size
        var maxZoom = CalculateMaxZoom(mapWidth, mapHeight);

        // Add zoom levels
        for (int zoom = 0; zoom <= maxZoom; zoom++)
        {
            var resolution = Math.Pow(2, maxZoom - zoom);
            Resolutions[zoom.ToString()] = new Resolution
            {
                Id = zoom.ToString(),
                UnitsPerPixel = resolution,
                TileWidth = 256,
                TileHeight = 256
            };
        }

        // Set extent to map bounds
        Extent = new Extent(0, 0, mapWidth, mapHeight);
    }

    private int CalculateMaxZoom(int width, int height)
    {
        var maxDimension = Math.Max(width, height);
        var maxZoom = (int)Math.Ceiling(Math.Log(maxDimension / 256.0, 2));
        return Math.Max(maxZoom, 5); // At least 5 zoom levels
    }
}
```

**Estimated Time**: 3-4 days

---

### Phase 4: Integrate Mapsui into Avalonia UI

**Goal**: Replace simple Image control with interactive Mapsui map control.

**Files to Modify**:
- `src/FantasyMapGenerator.UI/FantasyMapGenerator.UI.csproj`
- `src/FantasyMapGenerator.UI/Controls/MapControl.axaml`
- `src/FantasyMapGenerator.UI/Controls/MapControl.axaml.cs`
- `src/FantasyMapGenerator.UI/ViewModels/MapControlViewModel.cs`

**Implementation Steps**:

1. **Add NuGet Package**:
```xml
<PackageReference Include="Mapsui.Avalonia" Version="5.0.0" />
```

2. **Update MapControl.axaml**:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mapsui="clr-namespace:Mapsui.UI.Avalonia;assembly=Mapsui.UI.Avalonia"
             x:Class="FantasyMapGenerator.UI.Controls.MapControl">

    <Grid>
        <!-- Mapsui Map Control -->
        <mapsui:MapControl x:Name="MapView"
                           Map="{Binding Map}"
                           PointerWheelChanged="OnPointerWheelChanged"
                           PointerPressed="OnPointerPressed"
                           PointerMoved="OnPointerMoved"
                           PointerReleased="OnPointerReleased">

            <!-- Overlay controls -->
            <mapsui:MapControl.OverlayItems>
                <!-- Zoom controls -->
                <StackPanel VerticalAlignment="Top" HorizontalAlignment="Right" Margin="10">
                    <Button Content="+" Command="{Binding ZoomInCommand}" />
                    <Button Content="-" Command="{Binding ZoomOutCommand}" />
                    <Button Content="⌂" Command="{Binding ResetViewCommand}" />
                </StackPanel>

                <!-- Layer toggle -->
                <StackPanel VerticalAlignment="Bottom" HorizontalAlignment="Left" Margin="10">
                    <CheckBox Content="Terrain" IsChecked="{Binding ShowTerrain}" />
                    <CheckBox Content="Borders" IsChecked="{Binding ShowBorders}" />
                    <CheckBox Content="Rivers" IsChecked="{Binding ShowRivers}" />
                    <CheckBox Content="Cities" IsChecked="{Binding ShowCities}" />
                    <CheckBox Content="Labels" IsChecked="{Binding ShowLabels}" />
                </StackPanel>
            </mapsui:MapControl.OverlayItems>
        </mapsui:MapControl>

        <!-- Loading overlay -->
        <Panel IsVisible="{Binding IsLoading}">
            <Border Background="#80000000">
                <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                    <ProgressBar IsIndeterminate="True" Width="200" />
                    <TextBlock Text="Generating map tiles..." Foreground="White" />
                </StackPanel>
            </Border>
        </Panel>
    </Grid>
</UserControl>
```

3. **Update MapControl.axaml.cs**:

```csharp
public partial class MapControl : UserControl
{
    private readonly FantasyMapLayer _mapLayer;

    public MapControl()
    {
        InitializeComponent();
        DataContext = new MapControlViewModel();

        // Initialize Mapsui map
        InitializeMap();
    }

    private void InitializeMap()
    {
        var map = new Map();

        // Set up custom projection (pixel space)
        map.CRS = "PixelSpace";

        // Add fantasy map layer when data is loaded
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapControlViewModel.MapData)
                && ViewModel.MapData != null)
            {
                LoadFantasyMap(ViewModel.MapData);
            }
        };

        ViewModel.Map = map;
    }

    private void LoadFantasyMap(MapData mapData)
    {
        // Create tile provider
        var tileProvider = new FantasyMapTileProvider(mapData);

        // Create tile schema
        var schema = new FantasyMapTileSchema(mapData.Width, mapData.Height);

        // Create tile source
        var tileSource = new TileSource(tileProvider, schema);

        // Create layer
        var layer = new TileLayer(tileSource)
        {
            Name = "Fantasy Map"
        };

        // Add to map
        ViewModel.Map.Layers.Add(layer);

        // Set initial view
        ViewModel.Map.Navigator.ZoomToBox(
            new MRect(0, 0, mapData.Width, mapData.Height));
    }
}
```

4. **Update ViewModel**:

```csharp
public class MapControlViewModel : ViewModelBase
{
    private Map? _map;
    private MapData? _mapData;
    private bool _showTerrain = true;
    private bool _showBorders = true;
    private bool _showRivers = true;
    private bool _showCities = true;
    private bool _showLabels = true;

    public Map? Map
    {
        get => _map;
        set => this.RaiseAndSetIfChanged(ref _map, value);
    }

    public MapData? MapData
    {
        get => _mapData;
        set => this.RaiseAndSetIfChanged(ref _mapData, value);
    }

    // Layer visibility properties
    public bool ShowTerrain
    {
        get => _showTerrain;
        set
        {
            this.RaiseAndSetIfChanged(ref _showTerrain, value);
            UpdateLayerVisibility("Terrain", value);
        }
    }

    // Commands
    public ICommand ZoomInCommand => ReactiveCommand.Create(() =>
        Map?.Navigator.ZoomIn());

    public ICommand ZoomOutCommand => ReactiveCommand.Create(() =>
        Map?.Navigator.ZoomOut());

    public ICommand ResetViewCommand => ReactiveCommand.Create(() =>
    {
        if (Map != null && MapData != null)
        {
            Map.Navigator.ZoomToBox(
                new MRect(0, 0, MapData.Width, MapData.Height));
        }
    });

    private void UpdateLayerVisibility(string layerName, bool visible)
    {
        var layer = Map?.Layers.FirstOrDefault(l => l.Name == layerName);
        if (layer != null)
        {
            layer.Enabled = visible;
            Map?.Refresh();
        }
    }
}
```

**Estimated Time**: 3-4 days

---

### Phase 5: Advanced Features

**Goal**: Add production-ready features for professional map viewing.

**Features to Implement**:

1. **Multi-Layer Support**:
   - Separate layers for terrain, borders, rivers, cities, labels
   - Toggle visibility per layer
   - Adjust layer opacity

2. **Export Functionality**:
   - Export current view as PNG/JPG
   - Export full map at high resolution
   - Export as tile pyramid for web

3. **Measure Tools**:
   - Distance measurement
   - Area measurement
   - Coordinate display

4. **Annotations**:
   - Add custom markers
   - Draw routes
   - Add text notes

5. **Performance Optimizations**:
   - Tile caching to disk
   - Progressive tile loading
   - Background tile generation
   - Spatial indexing (KD-Tree) for cell lookup

**Implementation Example - Multi-Layer**:

```csharp
public class FantasyMapLayerFactory
{
    public ILayer CreateTerrainLayer(MapData mapData)
    {
        return new TileLayer(
            new TileSource(
                new FantasyMapTileProvider(mapData, LayerType.Terrain),
                new FantasyMapTileSchema(mapData.Width, mapData.Height)))
        {
            Name = "Terrain",
            Enabled = true,
            Opacity = 1.0
        };
    }

    public ILayer CreateRiversLayer(MapData mapData)
    {
        var features = mapData.Rivers.Select(river => new Feature
        {
            Geometry = CreateLineString(river.Cells, mapData),
            Styles = new[] { new VectorStyle
            {
                Line = new Pen(Color.Blue, river.Width)
            }}
        }).ToList();

        return new MemoryLayer
        {
            Name = "Rivers",
            Features = features,
            Enabled = true,
            Style = null // Use feature styles
        };
    }

    public ILayer CreateCitiesLayer(MapData mapData)
    {
        var features = mapData.Burgs.Select(burg => new Feature
        {
            Geometry = new Point(burg.X, burg.Y),
            Fields = new Dictionary<string, object>
            {
                ["Name"] = burg.Name,
                ["Type"] = burg.Type.ToString(),
                ["IsCapital"] = burg.IsCapital
            },
            Styles = new[] { CreateCityStyle(burg) }
        }).ToList();

        return new MemoryLayer
        {
            Name = "Cities",
            Features = features,
            Enabled = true,
            IsMapInfoLayer = true // Enable click for info
        };
    }
}
```

**Estimated Time**: 5-7 days

---

## Technical Considerations

### Coordinate System

Since fantasy maps don't use real-world geographic coordinates:

```csharp
public class PixelSpaceCRS : CRS
{
    public override string Name => "PixelSpace";

    public override double[] Transform(double x, double y)
    {
        // No transformation needed - already in pixel space
        return new[] { x, y };
    }

    public override double[] InverseTransform(double x, double y)
    {
        // No transformation needed
        return new[] { x, y };
    }
}
```

### Performance Optimization

**1. Spatial Index for Cell Lookup**:

```csharp
public class CellSpatialIndex
{
    private readonly KdTree<int> _tree;

    public CellSpatialIndex(MapData mapData)
    {
        _tree = new KdTree<int>(2); // 2D tree

        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            _tree.Insert(new[] { cell.Center.X, cell.Center.Y }, i);
        }
    }

    public int FindNearest(Point point)
    {
        var nearest = _tree.GetNearestNeighbours(
            new[] { point.X, point.Y }, 1);
        return nearest[0].Value;
    }
}
```

**2. Tile Caching**:

```csharp
public class DiskTileCache
{
    private readonly string _cacheDir;

    public DiskTileCache(string cacheDir)
    {
        _cacheDir = cacheDir;
        Directory.CreateDirectory(_cacheDir);
    }

    public byte[]? GetTile(TileIndex index)
    {
        var path = GetTilePath(index);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public void PutTile(TileIndex index, byte[] data)
    {
        var path = GetTilePath(index);
        File.WriteAllBytes(path, data);
    }

    private string GetTilePath(TileIndex index)
    {
        return Path.Combine(_cacheDir,
            $"tile_{index.Level}_{index.Col}_{index.Row}.png");
    }
}
```

**3. Progressive Tile Loading**:

```csharp
public class ProgressiveTileProvider : ITileProvider
{
    public byte[] GetTile(TileInfo tileInfo)
    {
        // Return low-res placeholder immediately
        var placeholder = GetPlaceholder(tileInfo);

        // Generate high-res tile in background
        Task.Run(() => GenerateHighResTile(tileInfo));

        return placeholder;
    }
}
```

### Memory Management

```csharp
public class FantasyMapTileProvider : ITileProvider, IDisposable
{
    private readonly LRUCache<TileIndex, byte[]> _memoryCache;
    private readonly DiskTileCache _diskCache;

    public FantasyMapTileProvider(MapData mapData, int memoryCacheSize = 100)
    {
        _memoryCache = new LRUCache<TileIndex, byte[]>(memoryCacheSize);
        _diskCache = new DiskTileCache("./tile_cache");
    }

    public void Dispose()
    {
        _memoryCache.Clear();
        _renderer?.Dispose();
    }
}
```

---

## Testing Strategy

### Unit Tests

1. **RasterBlurRenderer Tests**:
   - Test pixel-to-world coordinate conversion
   - Test nearest cell lookup
   - Test blur filter application
   - Verify output image dimensions

2. **HeightSmoother Tests**:
   - Test neighbor averaging
   - Test iteration convergence
   - Verify height value constraints (0-100)

3. **Tile Provider Tests**:
   - Test tile generation at different zooms
   - Test tile caching
   - Test coordinate transformations

### Integration Tests

1. **Rendering Comparison**:
   - Compare old renderer vs new RasterBlurRenderer
   - Verify smooth transitions
   - Check color accuracy

2. **Mapsui Integration**:
   - Test pan/zoom functionality
   - Test layer toggling
   - Test export functionality

### Visual Tests

Create comparison suite:
```
tests/
├── ComparisonTest/
│   ├── original_js_output.png
│   ├── old_csharp_output.png (sharp edges)
│   └── new_raster_blur_output.png (should match original)
```

---

## Migration Path

### Step-by-Step Migration

1. **Week 1: Foundation**
   - Implement RasterBlurRenderer
   - Test against original output
   - Tune blur parameters

2. **Week 2: Enhancement**
   - Add HeightSmoother
   - Optimize with spatial index
   - Performance benchmarks

3. **Week 3: Tile System**
   - Implement tile provider
   - Create tile schema
   - Test tile generation

4. **Week 4: UI Integration**
   - Integrate Mapsui
   - Update MapControl
   - Test interactivity

5. **Week 5: Polish**
   - Add multi-layer support
   - Implement export
   - Add measure tools

6. **Week 6: Testing & Documentation**
   - Comprehensive testing
   - Performance optimization
   - User documentation

### Backward Compatibility

Keep old renderers available:
```csharp
public enum RenderingMode
{
    Legacy,          // Old vector-based renderer
    RasterBlur,      // New raster blur renderer
    MapsuiInteractive // Mapsui with tiles
}
```

---

## Risk Assessment

### Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Mapsui incompatibility with Avalonia | High | Low | Use Mapsui.Avalonia package (officially supported) |
| Poor tile generation performance | Medium | Medium | Implement caching, spatial indexing, parallel processing |
| Coordinate system confusion | Medium | Medium | Use simple pixel space CRS, thoroughly document |
| Memory issues with large maps | High | Medium | LRU cache, disk cache, tile streaming |
| Blur artifacts at tile edges | High | Low | Overlap tiles slightly, blend edges |

---

## Success Criteria

### Must Have
- ✅ Smooth terrain transitions (no sharp Voronoi edges)
- ✅ Interactive pan/zoom functionality
- ✅ Layer toggling (terrain, borders, rivers, cities)
- ✅ Export map as image

### Should Have
- ✅ Multi-zoom level support (at least 3-5 levels)
- ✅ Tile caching for performance
- ✅ Measure distance tool
- ✅ Visual quality matching original JavaScript version

### Nice to Have
- 🎯 Export as web tile pyramid
- 🎯 Custom annotations
- 🎯 3D terrain view
- 🎯 Animation support (fly-to, route animation)

---

## Conclusion

This plan combines the best of three approaches:

1. **Raster Blur** (from analysis) - Fix the smoothing issue
2. **BruTile** - Professional tile-based rendering
3. **Mapsui** - Interactive map viewer with pan/zoom

The result will be a professional fantasy map generator with:
- Smooth, beautiful terrain rendering
- Interactive exploration
- Multi-layer visualization
- Export capabilities
- Performance optimizations

The architecture is modular, allowing incremental implementation and testing at each phase.

---

## Next Steps

1. Review this plan with stakeholders
2. Set up project timeline and milestones
3. Begin Phase 1: Implement RasterBlurRenderer
4. Create comparison test suite
5. Iterate based on results

**Ready to begin implementation!** 🚀
