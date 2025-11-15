# Project Scope: Core vs Rendering

## Clear Separation of Concerns

This document clarifies what belongs in **FantasyMapGenerator.Core** vs external rendering projects.

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│  FantasyMapGenerator.Core (THIS PROJECT)│
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│  • Map data generation                  │
│  • Algorithms (Voronoi, noise, etc.)   │
│  • Domain models (Cell, River, etc.)   │
│  • Data export (JSON, etc.)             │
│  • NO RENDERING CODE                    │
└─────────────┬───────────────────────────┘
              │
              │ MapData (rich data structures)
              │
      ┌───────┴────────┐
      ↓                ↓
┌─────────────┐  ┌──────────────────┐
│ HyacinthBean│  │ Other Rendering  │
│ MapViewer   │  │ Projects         │
│ ━━━━━━━━━━━ │  │ ━━━━━━━━━━━━━━━━ │
│ • GUI       │  │ • Web viewers    │
│ • Rendering │  │ • Console/TUI    │
│ • Styling   │  │ • Export tools   │
└─────────────┘  └──────────────────┘
```

---

## FantasyMapGenerator.Core Responsibilities

### ✅ IN SCOPE

#### 1. Data Generation
- Generate Voronoi tessellation
- Create heightmaps
- Calculate biomes
- Generate rivers and lakes
- Create political boundaries
- Calculate all properties

#### 2. Algorithms
- Noise generation (FastNoiseLite)
- Geometry operations (NetTopologySuite)
- Flow simulation
- Erosion calculations
- Lloyd relaxation
- Path interpolation

#### 3. Data Models
```csharp
// Rich data structures for consumers
public class MapData
{
    public List<Cell> Cells { get; set; }
    public List<River> Rivers { get; set; }
    public List<Lake> Lakes { get; set; }
    // ... all map data
}

public class River
{
    public List<int> Cells { get; set; }
    public List<Point> MeanderedPath { get; set; }  // For smooth rendering
    public int Width { get; set; }                   // For styling
    public RiverType Type { get; set; }              // For categorization
}

public class Lake
{
    public bool IsClosed { get; set; }               // For coloring
    public LakeType Type { get; set; }               // For styling
    public double Evaporation { get; set; }          // For simulation
}
```

#### 4. Data Export
- JSON serialization
- Data format conversion
- API for consumers

### ❌ OUT OF SCOPE

#### 1. Rendering
- Drawing to screen
- SkiaSharp/Canvas operations
- Image generation
- Visual styling

#### 2. UI Components
- Windows/controls
- User interaction
- Settings panels
- Avalonia/WinForms code

#### 3. Visualization
- Smooth curve rendering
- Contour tracing (for display)
- Color schemes
- Texture mapping

---

## Example: River Meandering

### ✅ Core Responsibility (Data Generation)

```csharp
// FantasyMapGenerator.Core/Generators/RiverMeandering.cs
public class RiverMeandering
{
    /// <summary>
    /// Generates meandered path points for a river.
    /// Rendering projects use these points to draw smooth curves.
    /// </summary>
    public List<Point> GenerateMeanderedPath(River river, MapData map)
    {
        var meandered = new List<Point>();
        
        for (int i = 0; i < river.Cells.Count - 1; i++)
        {
            var cell = map.Cells[river.Cells[i]];
            var nextCell = map.Cells[river.Cells[i + 1]];
            
            // Add current point
            meandered.Add(cell.Center);
            
            // Calculate intermediate points for smooth curves
            var intermediates = InterpolatePoints(
                cell.Center, 
                nextCell.Center, 
                meanderingFactor);
            
            meandered.AddRange(intermediates);
        }
        
        return meandered;
    }
}

// Store in data model
river.MeanderedPath = meandering.GenerateMeanderedPath(river, map);
```

### ❌ NOT Core Responsibility (Rendering)

```csharp
// External rendering project (HyacinthBean.MapViewer)
public class RiverRenderer
{
    public void DrawRiver(River river, SKCanvas canvas)
    {
        using var path = new SKPath();
        
        // Use the meandered path data from Core
        if (river.MeanderedPath.Any())
        {
            path.MoveTo(river.MeanderedPath[0].X, river.MeanderedPath[0].Y);
            
            foreach (var point in river.MeanderedPath.Skip(1))
            {
                path.LineTo(point.X, point.Y);
            }
        }
        
        // Rendering logic
        canvas.DrawPath(path, paint);
    }
}
```

---

## Example: Lake Evaporation

### ✅ Core Responsibility (Data Model)

```csharp
// FantasyMapGenerator.Core/Models/Lake.cs
public class Lake
{
    public int Id { get; set; }
    public List<int> Cells { get; set; }
    public double Inflow { get; set; }
    public double Evaporation { get; set; }
    
    /// <summary>
    /// True if lake has no outlet (evaporation >= inflow).
    /// Rendering projects can use this to color salt lakes differently.
    /// </summary>
    public bool IsClosed => Evaporation >= Inflow;
    
    public LakeType Type { get; set; }
}

// Core calculates the properties
private void CalculateLakeEvaporation(Lake lake)
{
    lake.Evaporation = CalculateEvaporation(
        lake.SurfaceArea, 
        lake.Temperature, 
        lake.Precipitation);
    
    // Determine type based on closure
    lake.Type = lake.IsClosed ? LakeType.Saltwater : LakeType.Freshwater;
}
```

### ❌ NOT Core Responsibility (Rendering)

```csharp
// External rendering project
public class LakeRenderer
{
    public void DrawLake(Lake lake, SKCanvas canvas)
    {
        // Use the data from Core to determine styling
        var color = lake.IsClosed 
            ? new SKColor(176, 224, 230)  // Pale blue for salt lakes
            : new SKColor(100, 149, 237); // Blue for freshwater
        
        // Rendering logic
        foreach (var cellId in lake.Cells)
        {
            DrawCell(cellId, color, canvas);
        }
    }
}
```

---

## Example: River Erosion

### ✅ Core Responsibility (Algorithm)

```csharp
// FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs
private void DowncutRivers()
{
    foreach (var cell in _map.Cells.Where(c => c.HasRiver && c.Height >= 35))
    {
        // Calculate erosion amount
        var downcut = CalculateErosion(cell);
        
        // Modify the height data
        cell.Height = (byte)Math.Max(cell.Height - downcut, 20);
    }
}
```

**Result**: Modified `Cell.Height` values

### ❌ NOT Core Responsibility (Visualization)

```csharp
// External rendering project
public class TerrainRenderer
{
    public void DrawTerrain(MapData map, SKCanvas canvas)
    {
        foreach (var cell in map.Cells)
        {
            // Use the height data from Core
            var color = GetColorForHeight(cell.Height);
            
            // Rendering logic
            DrawCell(cell, color, canvas);
        }
    }
}
```

**Result**: Visual representation of the height data

---

## What This Means for Implementation

### Core Features to Implement

1. **River Meandering** ✅
   - Generate `River.MeanderedPath` data
   - Store interpolated points
   - **No rendering code**

2. **River Erosion** ✅
   - Modify `Cell.Height` values
   - Implement erosion algorithm
   - **No visualization code**

3. **Lake Evaporation** ✅
   - Calculate `Lake.Evaporation`
   - Set `Lake.IsClosed` property
   - **No coloring/styling code**

4. **Advanced Erosion** ✅
   - Better heightmap algorithm
   - Modify terrain data
   - **No rendering code**

5. **Lloyd Relaxation** ✅
   - Improve point distribution
   - Better Voronoi cells
   - **No visualization code**

### Features NOT to Implement (Out of Scope)

1. ~~**Smooth Terrain Rendering**~~ ❌
   - External projects handle this
   - They use Core's data

2. ~~**Contour Visualization**~~ ❌
   - External projects handle this
   - They use Core's heightmap

3. ~~**UI Components**~~ ❌
   - External projects handle this
   - Core is UI-agnostic

4. ~~**SkiaSharp Integration**~~ ❌
   - External projects handle this
   - Core has no rendering dependencies

---

## Benefits of This Separation

### 1. Clean Architecture
- Core is pure logic
- No UI/rendering dependencies
- Easy to test

### 2. Multiple Consumers
- HyacinthBean.MapViewer (GUI)
- Console/TUI viewers
- Web applications
- Export tools

### 3. Flexibility
- Different rendering styles
- Different platforms
- Different use cases

### 4. Maintainability
- Core changes don't affect rendering
- Rendering changes don't affect Core
- Clear boundaries

---

## Testing Strategy

### Core Tests (In Scope)

```csharp
[Fact]
public void RiverMeandering_GeneratesCorrectNumberOfPoints()
{
    var river = new River { Cells = new List<int> { 0, 1, 2, 3 } };
    var meandering = new RiverMeandering();
    
    var path = meandering.GenerateMeanderedPath(river, map);
    
    // Test data generation, not rendering
    Assert.True(path.Count > river.Cells.Count);
    Assert.All(path, p => Assert.True(p.X >= 0 && p.Y >= 0));
}

[Fact]
public void LakeEvaporation_CorrectlyIdentifiesClosedBasins()
{
    var lake = new Lake 
    { 
        Inflow = 100, 
        Evaporation = 150 
    };
    
    // Test data model, not rendering
    Assert.True(lake.IsClosed);
    Assert.Equal(LakeType.Saltwater, lake.Type);
}
```

### Rendering Tests (Out of Scope)

```csharp
// These belong in external rendering projects
[Fact]
public void RiverRenderer_DrawsSmoothCurves() { /* ... */ }

[Fact]
public void LakeRenderer_UsesCorrectColors() { /* ... */ }
```

---

## Documentation Updates

### Core Documentation (This Project)

- ✅ Algorithm descriptions
- ✅ Data model specifications
- ✅ API documentation
- ✅ Usage examples (data generation)

### Rendering Documentation (External Projects)

- ❌ Rendering techniques
- ❌ Visual styling guides
- ❌ UI component documentation
- ❌ SkiaSharp usage

---

## Summary

### FantasyMapGenerator.Core

**Purpose**: Generate rich map data  
**Output**: Data structures (MapData, River, Lake, etc.)  
**Consumers**: Rendering projects, export tools, web apps

**Responsibilities**:
- ✅ Data generation
- ✅ Algorithms
- ✅ Domain models
- ✅ Data export

**NOT Responsibilities**:
- ❌ Rendering
- ❌ UI
- ❌ Visualization
- ❌ Styling

### External Rendering Projects

**Purpose**: Visualize map data  
**Input**: MapData from Core  
**Output**: Visual representations

**Responsibilities**:
- ✅ Drawing to screen
- ✅ User interface
- ✅ Visual styling
- ✅ Interactive features

---

## Revised Feature List (Core Only)

| Feature | Status | Scope |
|---------|--------|-------|
| Voronoi tessellation | ✅ Complete | Core |
| Heightmap generation | ✅ Complete | Core |
| Biome assignment | ✅ Complete | Core |
| River generation | ✅ Complete | Core |
| **River meandering data** | ❌ Missing | **Core** |
| **River erosion algorithm** | ❌ Missing | **Core** |
| **Lake evaporation model** | ❌ Missing | **Core** |
| **Advanced erosion** | ❌ Missing | **Core** |
| **Lloyd relaxation** | ❌ Missing | **Core** |
| Smooth rendering | N/A | External |
| UI components | N/A | External |
| Visual styling | N/A | External |

**Core Completion**: 87% → 100% (2 weeks)  
**Rendering**: Handled by external projects

---

## Questions?

**For core features**: See `CORE_FOCUSED_ROADMAP.md`  
**For rendering**: See external project documentation (HyacinthBean.MapViewer, etc.)

**This separation keeps the Core library clean, focused, and reusable!** ✅
