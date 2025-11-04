# Fantasy Map Generator Port - Documentation

This directory contains comprehensive documentation for adopting modern .NET libraries and implementing advanced features in the Fantasy Map Generator port.

## 📚 Documentation Index

### 🗺️ [Library Adoption Roadmap](./library-adoption-roadmap.md)
**Start here!** High-level overview of recommended libraries, migration strategy, and implementation milestones.

**Topics covered**:
- Current implementation analysis
- Library recommendations (FastNoiseLite, NetTopologySuite, PCG RNG)
- Decision matrix for each library
- Phased migration plan (M0-M4)
- Risk assessment and priorities

**When to read**: Before starting any major refactoring or library adoption.

---

### 🎲 [Deterministic Seeding Guide](./deterministic-seeding-guide.md)
**Priority: CRITICAL** - Foundation for all other features

**Topics covered**:
- Problems with current `Random.Shared` usage
- `IRandomSource` abstraction layer
- PCG (Permuted Congruential Generator) implementation
- Threading RNG through all generators
- Cross-platform reproducibility
- Unit tests for determinism

**When to read**: First milestone (M0). Required before implementing other features.

**Implementation effort**: 1-2 days

---

### 🏔️ [Noise Generation Guide](./noise-generation-guide.md)
**Priority: MEDIUM** - Significantly improves terrain quality

**Topics covered**:
- FastNoiseLite integration (drop-in single file)
- Noise types: Perlin, Simplex, Cellular, Value
- Fractal/octave layering for detail
- Domain warping for organic terrain
- Heightmap profiles (island, continents, archipelago)
- Migration strategy (additive vs replacement)

**When to read**: Milestone 1 (M1), after deterministic seeding is complete.

**Implementation effort**: 2-3 days

---

### 🌊 [Hydrology Implementation Guide](./hydrology-implementation-guide.md)
**Priority: HIGH** - Major feature gap vs original FMG

**Topics covered**:
- Flow direction calculation (D8 steepest descent)
- Pit filling algorithm (Priority Flood)
- Flow accumulation (topological sort)
- River generation from high-accumulation cells
- Lake identification
- River width calculation (logarithmic scaling)
- Advanced features: deltas, seasonal rivers, names

**When to read**: Milestone 2 (M2), after deterministic seeding.

**Implementation effort**: 4-5 days

---

### 🔷 [Geometry Operations Guide](./geometry-operations-guide.md)
**Priority: LOW** - Nice-to-have for polish

**Topics covered**:
- NetTopologySuite (NTS) integration
- `NtsGeometryAdapter` for FMG ↔ NTS conversion
- Border smoothing (morphological operations)
- State boundary generation
- Rain shadow calculation
- Spatial indexing (STRtree for fast queries)
- Line simplification (Douglas-Peucker)

**When to read**: Milestone 3 (M3), after core features are working.

**Implementation effort**: 3-4 days

---

## 🚀 Quick Start Guide

### Step 1: Review Current State
1. Read [Library Adoption Roadmap](./library-adoption-roadmap.md) sections:
   - "Current Implementation Analysis"
   - "What's Working Well"
   - "What Needs Improvement"

### Step 2: Implement Deterministic Seeding (M0)
**Why first**: Enables testing, reproducibility, and debugging for all other features.

1. Read [Deterministic Seeding Guide](./deterministic-seeding-guide.md)
2. Implement:
   - `IRandomSource` interface
   - `SystemRandomSource` (backwards compat)
   - `PcgRandomSource` (recommended)
3. Thread RNG through all generators
4. Add reproducibility tests
5. Validate: Same seed = same map

**Success criteria**: `dotnet test --filter "Category=Reproducibility"` passes

---

### Step 3: Add Advanced Noise (M1)
**Why next**: Improves terrain quality without breaking existing code.

1. Read [Noise Generation Guide](./noise-generation-guide.md)
2. Download `FastNoiseLite.cs` → `src/FantasyMapGenerator.Core/Noise/`
3. Create `FastNoiseHeightmapGenerator` (keep existing generator)
4. Add UI toggle for advanced noise
5. Compare visual quality

**Success criteria**: Generate maps with both generators, user can choose

---

### Step 4: Implement Hydrology (M2)
**Why next**: Core feature for map realism.

1. Read [Hydrology Implementation Guide](./hydrology-implementation-guide.md)
2. Create `HydrologyGenerator.cs`
3. Implement:
   - Pit filling
   - Flow direction
   - Flow accumulation
   - River generation
4. Update rendering to draw rivers
5. Test: Rivers flow downhill to ocean

**Success criteria**: 20-100 realistic rivers per map, no uphill flow

---

### Step 5: Add Geometry Operations (M3) - Optional
**Why optional**: Polish feature, not critical for functionality.

1. Read [Geometry Operations Guide](./geometry-operations-guide.md)
2. Add NetTopologySuite NuGet package
3. Create `NtsGeometryAdapter.cs`
4. Implement border smoothing in `BiomeGenerator`
5. Add state boundary export

**Success criteria**: State boundaries are smooth polygons, exportable as GeoJSON

---

## 📊 Feature Comparison Matrix

| Feature | Current | After M0 | After M1 | After M2 | After M3 |
|---------|---------|----------|----------|----------|----------|
| **Reproducibility** | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **Terrain Quality** | ⚠️ Basic | ⚠️ Basic | ✅ Advanced | ✅ Advanced | ✅ Advanced |
| **Rivers** | ❌ None | ❌ None | ❌ None | ✅ Full | ✅ Full |
| **Lakes** | ❌ None | ❌ None | ❌ None | ✅ Auto | ✅ Auto |
| **Border Smoothing** | ❌ None | ❌ None | ❌ None | ❌ None | ✅ Yes |
| **Spatial Queries** | ⚠️ O(n) | ⚠️ O(n) | ⚠️ O(n) | ⚠️ O(n) | ✅ O(log n) |
| **Vector Export** | ❌ None | ❌ None | ❌ None | ❌ None | ✅ GeoJSON |

---

## 🛠️ Implementation Checklist

### Milestone 0: Deterministic Seeding (1-2 days)
- [ ] Create `src/FantasyMapGenerator.Core/Random/IRandomSource.cs`
- [ ] Create `src/FantasyMapGenerator.Core/Random/SystemRandomSource.cs`
- [ ] Create `src/FantasyMapGenerator.Core/Random/PcgRandomSource.cs`
- [ ] Update `MapGenerationSettings` with `CreateRandom()` method
- [ ] Update `MapGenerator` to thread RNG through generators
- [ ] Update `HeightmapGenerator` to take `IRandomSource`
- [ ] Update `BiomeGenerator` to take `IRandomSource`
- [ ] Update `GeometryUtils` to take `IRandomSource`
- [ ] Add `ReproducibilityTests.cs` unit tests
- [ ] Verify: Same seed produces identical maps

### Milestone 1: Noise Generation (2-3 days)
- [ ] Download `FastNoiseLite.cs` to `src/FantasyMapGenerator.Core/Noise/`
- [ ] Create `src/FantasyMapGenerator.Core/Generators/FastNoiseHeightmapGenerator.cs`
- [ ] Add `UseAdvancedNoise` setting to `MapGenerationSettings`
- [ ] Update `MapGenerator` to choose generator based on settings
- [ ] Add UI toggle (Avalonia)
- [ ] Test: Generate maps with both systems
- [ ] Compare: Screenshot template vs noise-based

### Milestone 2: Hydrology (4-5 days)
- [ ] Create `src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs`
- [ ] Implement pit filling (Priority Flood)
- [ ] Implement flow direction calculation
- [ ] Implement flow accumulation
- [ ] Implement river generation
- [ ] Implement lake identification
- [ ] Implement river width calculation
- [ ] Update `River` model with all properties
- [ ] Update `MapRenderer` to draw rivers
- [ ] Add `HydrologyGeneratorTests.cs` unit tests
- [ ] Test: Rivers flow downhill, no loops

### Milestone 3: Geometry Operations (3-4 days) - Optional
- [ ] Add `NetTopologySuite` NuGet package
- [ ] Create `src/FantasyMapGenerator.Core/Geometry/NtsGeometryAdapter.cs`
- [ ] Implement `CellToPolygon()` conversion
- [ ] Implement border smoothing in `BiomeGenerator`
- [ ] Create `StateBoundaryGenerator.cs`
- [ ] (Optional) Add spatial indexing to `MapData`
- [ ] (Optional) Implement rain shadow calculation
- [ ] Add `NtsGeometryAdapterTests.cs` unit tests
- [ ] Test: Smooth borders, no gaps/overlaps

---

## 🎯 Recommended Reading Order

**For developers new to the codebase**:
1. Library Adoption Roadmap (overview)
2. Deterministic Seeding Guide (foundation)
3. Noise Generation Guide (visual improvement)
4. Hydrology Implementation Guide (major feature)
5. Geometry Operations Guide (polish)

**For implementing a specific feature**:
- **Better terrain**: Noise Generation Guide
- **Rivers/lakes**: Hydrology Implementation Guide
- **Smooth borders**: Geometry Operations Guide
- **Reproducible maps**: Deterministic Seeding Guide

**For understanding the migration strategy**:
- Library Adoption Roadmap (entire document)

---

## 📖 Additional Resources

### External Documentation
- [Azgaar's Fantasy Map Generator](https://github.com/Azgaar/Fantasy-Map-Generator) - Original JavaScript implementation
- [FastNoiseLite](https://github.com/Auburn/FastNoiseLite) - Noise library documentation
- [NetTopologySuite](https://nettopologysuite.github.io/NetTopologySuite/) - Geometry library docs
- [PCG Random](https://www.pcg-random.org/) - RNG algorithm details

### Academic Papers
- [Priority Flood Algorithm](https://doi.org/10.1016/j.cageo.2013.04.024) - Pit filling
- [Flow Direction Methods](https://desktop.arcgis.com/en/arcmap/latest/tools/spatial-analyst-toolbox/how-flow-direction-works.htm) - Hydrology
- [River Width Scaling](https://agupubs.onlinelibrary.wiley.com/doi/full/10.1002/2013WR014246) - Hydrology

### Related Projects
- [Red Blob Games - Terrain from Noise](https://www.redblobgames.com/maps/terrain-from-noise/) - Excellent noise tutorial
- [Martin O'Leary - Fantasy Map Generator](https://mewo2.com/notes/terrain/) - Alternative approach
- [Delaunator](https://github.com/mapbox/delaunator) - Original JS triangulation library

---

## 🤝 Contributing

When adding new documentation:
1. Follow existing format (Overview, Why, Implementation, Testing, Resources)
2. Include code examples with full context
3. Add cross-references to related docs
4. Update this README with links
5. Add to recommended reading order

---

## 📝 Questions?

If you have questions about:
- **Architecture decisions**: See ADR (Architecture Decision Records) in `docs/adr/`
- **Feature proposals**: See RFC (Request for Comments) in `docs/rfcs/`
- **Library choices**: See [Library Adoption Roadmap](./library-adoption-roadmap.md)
- **Implementation details**: See specific feature guides

---

**Last Updated**: 2025-11-04
**Maintainer**: Claude Code
**Status**: Complete - Ready for implementation
