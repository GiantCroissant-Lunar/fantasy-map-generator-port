# Core-Focused Implementation Roadmap

## Project Scope Clarification

**This project focuses on**: Core map generation logic (data structures, algorithms)  
**Rendering/UI handled by**: Separate projects (HyacinthBean.MapViewer, etc.)

**Separation of Concerns**:
```
FantasyMapGenerator.Core (THIS PROJECT)
├── Map data generation
├── Algorithms (Voronoi, noise, hydrology)
├── Domain models (Cell, River, Biome, etc.)
└── Export to data formats

↓ (consumed by)

External Projects (NOT THIS PROJECT)
├── HyacinthBean.MapViewer.Avalonia (GUI rendering)
├── HyacinthBean.MapViewer.Sample (Console/Braille)
└── Other consumers
```

---

## Revised Feature Assessment

### ✅ Core Features (In Scope)

1. **River Meandering Data** ⭐⭐⭐⭐⭐
   - Generate meandered path points
   - Store in `River.MeanderedPath`
   - **Consumers render the paths**

2. **River Erosion** ⭐⭐⭐
   - Modify cell heights based on water flow
   - Update `Cell.Height` values
   - **Core algorithm only**

3. **Lake Evaporation** ⭐⭐
   - Calculate evaporation vs inflow
   - Mark lakes as closed/open
   - **Data model enhancement**

4. **Advanced Erosion** ⭐⭐⭐⭐
   - Better terrain shaping algorithm
   - Modify heightmap data
   - **Core algorithm only**

5. **Lloyd Relaxation** ⭐⭐⭐
   - Improve point distribution
   - Better Voronoi cells
   - **Core algorithm only**

### ❌ Rendering Features (Out of Scope)

1. ~~**Smooth Terrain Rendering**~~ → External projects handle this
2. ~~**Contour Visualization**~~ → External projects handle this
3. ~~**UI Components**~~ → External projects handle this
4. ~~**SkiaSharp Integration**~~ → External projects handle this

---

## Revised 2-Week Plan (Core Only)

### Week 1: Azgaar Core Features

#### Day 1-2: River Meandering Data Generation
**What**: Generate meandered path points for rivers  
**Output**: `River.MeanderedPath` populated with interpolated points  
**Consumers**: Rendering projects use these points to draw curves

```csharp
// Core generates the data
public class RiverMeandering
{
    public List<Point> GenerateMeanderedPath(River river, MapData map)
    {
        // Algorithm generates smooth path points
        // Returns list of points for rendering
    }
}

// Rendering project (external) uses the data
foreach (var point in river.MeanderedPath)
{
    // External project draws the curve
}
```

#### Day 3: River Erosion Algorithm
**What**: Modify terrain heights based on river flow  
**Output**: Updated `Cell.Height` values  
**Consumers**: Rendering projects see carved valleys

```csharp
public class HydrologyGenerator
{
    private void DowncutRivers()
    {
        // Modify cell.Height based on flux
        // Pure data transformation
    }
}
```

#### Day 4-5: Lake Evaporation Model
**What**: Calculate lake properties and closure status  
**Output**: `Lake` model with evaporation data  
**Consumers**: Rendering projects can color closed lakes differently

```csharp
public class Lake
{
    public double Evaporation { get; set; }
    public bool IsClosed => Evaporation >= Inflow;
    public LakeType Type { get; set; } // Freshwater, Saltwater
}
```

---

### Week 2: Reference Project Algorithms

#### Day 1-2: Advanced Erosion Algorithm
**What**: Better terrain shaping from Choochoo's port  
**Output**: More realistic heightmap data  
**Consumers**: Rendering projects see better terrain

```csharp
public void ApplyAdvancedErosion(int iterations, double amount)
{
    // Modify heightmap based on neighbor analysis
    // Pure algorithm, no rendering
}
```

#### Day 3: Lloyd Relaxation
**What**: Improve Voronoi point distribution  
**Output**: Better cell shapes  
**Consumers**: Rendering projects see more uniform cells

```csharp
public static List<Point> ApplyLloydRelaxation(
    List<Point> points, int width, int height, int iterations)
{
    // Move points to cell centroids
    // Returns improved point distribution
}
```

#### Day 4-5: Data Export Enhancements
**What**: Better data structures for rendering  
**Output**: Rich data models for consumers

```csharp
// Enhanced data for rendering projects
public class Cell
{
    public int Flux { get; set; }              // For river width
    public int DistanceToWater { get; set; }   // For terrain features
    public bool IsConfluence { get; set; }     // For river junctions
}

public class River
{
    public List<Point> MeanderedPath { get; set; }  // For smooth curves
    public int Width { get; set; }                   // For rendering
    public RiverType Type { get; set; }              // For styling
}
```

---

## Core-Only Implementation Details

### 1. River Meandering (Core Data Generation)

**Core Responsibility**: Generate meandered path points  
**Rendering Responsibility**: Draw the curves

```csharp
// src/FantasyMapGenerator.Core/Generators/RiverMeandering.cs
public class RiverMeandering
{
    public List<Point> GenerateMeanderedPath(
        River river, 
        MapData map, 
        double meanderingFactor = 0.5)
    {
        var meandered = new List<Point>();
        var cells = river.Cells;
        
        for (int i = 0; i < cells.Count - 1; i++)
        {
            var cell = map.Cells[cells[i]];
            var nextCell = map.Cells[cells[i + 1]];
            
            // Add current point
            meandered.Add(cell.Center);
            
            // Calculate intermediate points
            double meander = CalculateMeandering(i, meanderingFactor);
            var intermediatePoints = InterpolatePoints(
                cell.Center, 
                nextCell.Center, 
                meander);
            
            meandered.AddRange(intermediatePoints);
        }
        
        return meandered;
    }
}

// Update River model
public class River
{
    public List<Point> MeanderedPath { get; set; } = new();
}
```

**Usage by Rendering Projects**:
```csharp
// External rendering project
foreach (var river in map.Rivers)
{
    if (river.MeanderedPath.Any())
    {
        // Use meandered path for smooth curves
        DrawSmoothCurve(river.MeanderedPath);
    }
    else
    {
        // Fallback to cell centers
        DrawStraightLines(river.Cells);
    }
}
```

---

### 2. River Erosion (Core Algorithm)

**Core Responsibility**: Modify heightmap  
**Rendering Responsibility**: Visualize the modified terrain

```csharp
// src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs
private void DowncutRivers()
{
    const int MAX_DOWNCUT = 5;
    
    foreach (var cell in _map.Cells.Where(c => c.HasRiver && c.Height >= 35))
    {
        // Calculate erosion
        var downcut = CalculateErosion(cell);
        
        // Modify height (pure data change)
        cell.Height = (byte)Math.Max(cell.Height - downcut, 20);
    }
}
```

**No rendering code in Core!** Rendering projects automatically see the updated heights.

---

### 3. Lake Evaporation (Core Model)

**Core Responsibility**: Calculate lake properties  
**Rendering Responsibility**: Color/style based on properties

```csharp
// src/FantasyMapGenerator.Core/Models/Lake.cs
public class Lake
{
    public int Id { get; set; }
    public List<int> Cells { get; set; } = new();
    public double Inflow { get; set; }
    public double Evaporation { get; set; }
    public bool IsClosed => Evaporation >= Inflow;
    public LakeType Type { get; set; }
}

// Core calculates the data
private void CalculateLakeEvaporation(Lake lake)
{
    lake.Evaporation = CalculateEvaporation(
        lake.SurfaceArea, 
        lake.Temperature, 
        lake.Precipitation);
}
```

**Rendering projects use the data**:
```csharp
// External rendering project
var color = lake.IsClosed 
    ? SaltLakeColor 
    : FreshwaterColor;
```

---

### 4. Enhanced Data Models for Rendering

**Core provides rich data**, rendering projects consume it:

```csharp
// src/FantasyMapGenerator.Core/Models/Cell.cs
public class Cell
{
    // Existing properties...
    
    // NEW: Additional data for rendering
    public int Flux { get; set; }              // Water flow volume
    public int DistanceToWater { get; set; }   // For terrain effects
    public bool IsConfluence { get; set; }     // River junction
    public int RiverId { get; set; }           // Which river
}

// src/FantasyMapGenerator.Core/Models/River.cs
public class River
{
    // Existing properties...
    
    // NEW: Rendering data
    public List<Point> MeanderedPath { get; set; } = new();
    public int Width { get; set; }
    public RiverType Type { get; set; }
    public bool IsSeasonal { get; set; }
}
```

---

## Testing Strategy (Core Only)

### Unit Tests

```csharp
[Fact]
public void RiverMeandering_GeneratesMorePoints()
{
    var river = new River { Cells = new List<int> { 0, 1, 2, 3 } };
    var meandering = new RiverMeandering();
    
    var path = meandering.GenerateMeanderedPath(river, map);
    
    Assert.True(path.Count > river.Cells.Count);
}

[Fact]
public void RiverErosion_LowersRiverCells()
{
    var riverCell = map.Cells.First(c => c.HasRiver);
    int originalHeight = riverCell.Height;
    
    generator.DowncutRivers();
    
    Assert.True(riverCell.Height <= originalHeight);
}

[Fact]
public void LakeEvaporation_IdentifiesClosedBasins()
{
    generator.CalculateLakeEvaporation();
    
    Assert.Contains(map.Lakes, l => l.IsClosed);
}
```

### Integration Tests

```csharp
[Fact]
public void FullGeneration_ProducesValidData()
{
    var settings = new MapGenerationSettings { Seed = 12345 };
    var map = new MapGenerator().Generate(settings);
    
    // Verify data integrity
    Assert.All(map.Rivers, r => 
    {
        Assert.NotEmpty(r.MeanderedPath);
        Assert.True(r.Width > 0);
    });
    
    Assert.All(map.Lakes, l =>
    {
        Assert.True(l.Evaporation >= 0);
        Assert.NotNull(l.Type);
    });
}
```

---

## File Structure (Core Only)

```
src/FantasyMapGenerator.Core/
├── Generators/
│   ├── MapGenerator.cs (orchestrator)
│   ├── HeightmapGenerator.cs
│   ├── HydrologyGenerator.cs (add erosion)
│   ├── RiverMeandering.cs (NEW)
│   ├── BiomeGenerator.cs
│   └── ...
├── Models/
│   ├── MapData.cs
│   ├── Cell.cs (enhance with Flux, etc.)
│   ├── River.cs (add MeanderedPath)
│   ├── Lake.cs (NEW)
│   └── ...
├── Geometry/
│   ├── GeometryUtils.cs (add Lloyd relaxation)
│   ├── Voronoi.cs
│   └── ...
└── Random/
    └── ...

tests/FantasyMapGenerator.Core.Tests/
├── RiverMeanderingTests.cs (NEW)
├── ErosionTests.cs (NEW)
├── LakeEvaporationTests.cs (NEW)
└── ...
```

**No rendering code in Core!**

---

## Configuration (Core Only)

```csharp
public class MapGenerationSettings
{
    // River features
    public bool EnableRiverMeandering { get; set; } = true;
    public double MeanderingFactor { get; set; } = 0.5;
    
    public bool EnableRiverErosion { get; set; } = true;
    public int MaxErosionDepth { get; set; } = 5;
    
    // Lake features
    public bool EnableLakeEvaporation { get; set; } = true;
    
    // Point distribution
    public bool ApplyLloydRelaxation { get; set; } = false;
    public int LloydIterations { get; set; } = 1;
    
    // Advanced erosion
    public bool UseAdvancedErosion { get; set; } = true;
    public int ErosionIterations { get; set; } = 5;
}
```

---

## Export Formats (Core Responsibility)

Core should provide data in formats rendering projects can consume:

```csharp
// JSON export (for web viewers, etc.)
public class MapDataExporter
{
    public string ExportToJson(MapData map)
    {
        return JsonSerializer.Serialize(new
        {
            cells = map.Cells,
            rivers = map.Rivers.Select(r => new
            {
                r.Id,
                r.Cells,
                meanderedPath = r.MeanderedPath, // For smooth rendering
                r.Width,
                r.Type
            }),
            lakes = map.Lakes.Select(l => new
            {
                l.Id,
                l.Cells,
                l.IsClosed,
                l.Type
            })
        });
    }
}
```

---

## Summary: Core vs Rendering Separation

### ✅ Core Project Responsibilities

1. **Data Generation**
   - Generate map data structures
   - Calculate all properties (heights, flux, evaporation, etc.)
   - Provide rich data models

2. **Algorithms**
   - Voronoi tessellation
   - Noise generation
   - River flow simulation
   - Erosion calculations
   - Lloyd relaxation

3. **Data Export**
   - JSON serialization
   - Data format conversion
   - API for consumers

### ❌ NOT Core Responsibilities

1. **Rendering**
   - Drawing to screen
   - Color schemes
   - Visual styling
   - UI components

2. **Visualization**
   - Smooth curve rendering
   - Contour tracing (for display)
   - Image generation
   - Interactive viewers

---

## Revised Timeline (Core Only)

**Week 1**: Azgaar core features (3-4 days)
- River meandering data generation
- River erosion algorithm
- Lake evaporation model

**Week 2**: Reference project algorithms (3-4 days)
- Advanced erosion
- Lloyd relaxation
- Enhanced data models

**Total**: 2 weeks for core features only

**Rendering**: Handled by external projects (HyacinthBean.MapViewer, etc.)

---

## Next Steps

1. ✅ Focus on **data generation** only
2. ✅ Provide **rich data models** for consumers
3. ✅ Let **rendering projects** handle visualization
4. ✅ Keep **Core clean** and rendering-agnostic

**This maintains clean separation of concerns and allows multiple rendering implementations!**
