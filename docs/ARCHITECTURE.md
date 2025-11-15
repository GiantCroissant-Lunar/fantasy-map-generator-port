# Architecture Overview

## System Design

The Fantasy Map Generator is organized into modular components for map generation, geometry processing, and rendering.

## Core Components

### Map Generation Pipeline

```
Seed → Heightmap → Biomes → Hydrology → Cultures → States → Burgs
```

1. **Heightmap Generation** - Terrain elevation using noise algorithms
2. **Biome Assignment** - Climate-based biome distribution
3. **Hydrology** - Water flow, rivers, and lakes
4. **Culture Generation** - Procedural cultures with naming
5. **State Formation** - Political boundaries and territories
6. **Settlement Placement** - Cities and towns (burgs)

### Key Modules

#### Generators
- `HeightmapGenerator` - Terrain elevation
- `BiomeGenerator` - Climate and biome assignment
- `HydrologyGenerator` - Water systems
- `MapGenerator` - Main orchestration

#### Geometry
- `Voronoi` - Voronoi diagram generation
- `Delaunator` - Delaunay triangulation
- `NtsGeometryAdapter` - Advanced spatial operations
- `StateBoundaryGenerator` - Political boundaries

#### Models
- `MapData` - Central data structure
- `Cell` - Voronoi cell with properties
- `Biome`, `Culture`, `State` - Entity models

#### Random
- `IRandomSource` - Abstraction for RNG
- `PcgRandomSource` - PCG algorithm (recommended)
- `SystemRandomSource` - .NET Random wrapper

## Data Flow

```
Settings → MapGenerator
    ↓
Heightmap (2D array)
    ↓
Voronoi Cells (spatial structure)
    ↓
Cell Properties (height, moisture, temperature)
    ↓
Biomes, Rivers, Cultures, States
    ↓
MapData (complete map)
```

## Rendering Architecture

See detailed rendering documentation:
- [Rendering Architecture](../RENDERING_ARCHITECTURE.md)
- [Mapsui Integration Plan](../MAPSUI_INTEGRATION_PLAN.md)
- [Smooth Renderer Implementation](../SMOOTH_RENDERER_IMPLEMENTATION.md)

## Testing Strategy

- **Unit Tests** - Individual component testing
- **Reproducibility Tests** - Deterministic generation verification
- **Snapshot Tests** - Regression detection
- **Cross-Platform Tests** - Platform consistency
- **Statistical Tests** - Output quality validation

## Dependencies

- **NetTopologySuite** - Geometry operations
- **SkiaSharp** - Image rendering
- **Mapsui** - Interactive map rendering (planned)

## Extension Points

- Custom noise algorithms via `INoiseGenerator`
- Custom RNG via `IRandomSource`
- Custom biome rules
- Custom naming generators
- Custom rendering backends
