# Fantasy Map Generator (C# Port)

A C# port of the Fantasy Map Generator, creating procedurally generated fantasy world maps with realistic geography, biomes, cultures, and political boundaries.

## Features

- **Heightmap Generation** - Multiple noise algorithms for terrain generation
- **Biome System** - Temperature and moisture-based biome distribution
- **Hydrology** - Rivers, lakes, and water flow simulation
- **Political Boundaries** - Procedural state and culture generation
- **Geometry Operations** - Voronoi diagrams, Delaunay triangulation
- **Deterministic Generation** - Reproducible maps from seeds

## Quick Start

```bash
# Clone the repository
git clone <repository-url>

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Project Structure

- `src/FantasyMapGenerator.Core` - Core map generation logic
- `src/FantasyMapGenerator.Rendering` - Rendering implementations
- `tests/` - Comprehensive test suite
- `docs/` - Implementation guides and documentation

## Documentation

- [Quick Start Guide](docs/QUICK_START_MISSING_FEATURES.md) - Get started quickly
- [Missing Features Guide](docs/MISSING_FEATURES_GUIDE.md) - Implementation roadmap
- [Architecture Overview](docs/ARCHITECTURE.md) - System design and architecture

### Implementation Guides

- [Noise Generation](docs/noise-generation-guide.md)
- [Geometry Operations](docs/geometry-operations-guide.md)
- [Hydrology System](docs/hydrology-implementation-guide.md)
- [Deterministic Seeding](docs/deterministic-seeding-guide.md)

### Analysis & Planning

- [Code Review Recommendations](docs/CODE_REVIEW_RECOMMENDATIONS.md)
- [Comparison with Original](docs/COMPARISON_WITH_ORIGINAL.md)
- [Critical Issues Summary](docs/CRITICAL_ISSUES_SUMMARY.md)
- [Library Adoption Roadmap](docs/library-adoption-roadmap.md)

## Status

This is an active port of the original JavaScript Fantasy Map Generator. See the [Missing Features Guide](docs/MISSING_FEATURES_GUIDE.md) for current implementation status.

## Original Project

Based on [Azgaar's Fantasy Map Generator](https://github.com/Azgaar/Fantasy-Map-Generator)

## License

[Add license information]
