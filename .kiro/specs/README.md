# Kiro Specs: Missing Core Features

This directory contains Kiro specs for implementing the missing 13% of core features to reach 100% completion.

## Overview

**Current Status**: 87% complete  
**Target**: 100% complete  
**Estimated Time**: 2 weeks (10-15 hours total)  
**Scope**: Core map generation only (no rendering)

## Specs

### High Priority (Week 1)

#### [001: River Meandering Data Generation](./001-river-meandering-data.md)
**Status**: Draft  
**Priority**: High ⭐⭐⭐⭐⭐  
**Effort**: 2-3 hours  
**Impact**: Huge visual improvement for rendering projects

Generate meandered path points for rivers to enable smooth curve rendering.

**Key Deliverables**:
- `River.MeanderedPath` property
- `RiverMeandering` class
- Integration with `HydrologyGenerator`

---

#### [002: River Erosion Algorithm](./002-river-erosion-algorithm.md)
**Status**: Draft  
**Priority**: High ⭐⭐⭐  
**Effort**: 1-2 hours  
**Impact**: Adds terrain depth (valleys)

Implement river erosion to carve valleys into terrain based on water flux.

**Key Deliverables**:
- `DowncutRivers()` method
- Erosion configuration settings
- Modified `Cell.Height` values

---

#### [003: Lake Evaporation Model](./003-lake-evaporation-model.md)
**Status**: Draft  
**Priority**: Medium ⭐⭐  
**Effort**: 3-4 hours  
**Impact**: Closed basins (Dead Sea style)

Calculate lake evaporation to create closed basins (salt lakes).

**Key Deliverables**:
- `Lake` class with evaporation properties
- Lake identification and classification
- `MapData.Lakes` collection

---

### Medium Priority (Week 2)

#### [004: Advanced Erosion Algorithm](./004-advanced-erosion-algorithm.md)
**Status**: Draft  
**Priority**: Medium ⭐⭐⭐⭐  
**Effort**: 4-6 hours  
**Impact**: More realistic terrain  
**Dependencies**: 002-river-erosion-algorithm

Implement neighbor-based erosion algorithm from reference project.

**Key Deliverables**:
- `ApplyAdvancedErosion()` method
- Iterative erosion refinement
- Alternative to simple erosion

---

#### [005: Lloyd Relaxation](./005-lloyd-relaxation.md)
**Status**: Draft  
**Priority**: Low ⭐⭐⭐  
**Effort**: 2-3 hours  
**Impact**: Better point distribution

Apply Lloyd relaxation to improve Voronoi cell uniformity.

**Key Deliverables**:
- `ApplyLloydRelaxation()` in `GeometryUtils`
- Optional feature (disabled by default)
- Reduced cell size variance

---

## Implementation Order

### Recommended Sequence

1. **001: River Meandering** (2-3 hours)
   - Biggest visual impact
   - Easiest to implement
   - No dependencies

2. **002: River Erosion** (1-2 hours)
   - Complements meandering
   - Simple algorithm
   - No dependencies

3. **003: Lake Evaporation** (3-4 hours)
   - New data model
   - Moderate complexity
   - No dependencies

4. **004: Advanced Erosion** (4-6 hours)
   - Depends on 002
   - More complex
   - Optional enhancement

5. **005: Lloyd Relaxation** (2-3 hours)
   - Independent feature
   - Optional enhancement
   - Can be done anytime

### Alternative Sequence (Parallel Work)

**Track A** (Rivers):
1. 001: River Meandering
2. 002: River Erosion
3. 004: Advanced Erosion

**Track B** (Water Bodies):
1. 003: Lake Evaporation

**Track C** (Geometry):
1. 005: Lloyd Relaxation

## Progress Tracking

| Spec | Status | Progress | Completed |
|------|--------|----------|-----------|
| 001-river-meandering-data | Draft | 0% | ⬜ |
| 002-river-erosion-algorithm | Draft | 0% | ⬜ |
| 003-lake-evaporation-model | Draft | 0% | ⬜ |
| 004-advanced-erosion-algorithm | Draft | 0% | ⬜ |
| 005-lloyd-relaxation | Draft | 0% | ⬜ |

**Overall Progress**: 0/5 specs completed (0%)

## Using These Specs

### With Kiro IDE

1. Open a spec file in Kiro
2. Review requirements and design
3. Use Kiro's spec workflow to implement
4. Mark tasks as complete
5. Run tests to verify

### Manual Implementation

1. Read the spec thoroughly
2. Follow the implementation tasks in order
3. Write tests as you go
4. Update the spec status
5. Move to next spec

## Testing Strategy

Each spec includes:
- Unit tests for core functionality
- Integration tests for full generation
- Performance tests for efficiency
- Quality tests for output validation

Run all tests after each spec:
```bash
dotnet test
```

## Documentation

After completing specs, update:
- [ ] `docs/EXECUTIVE_SUMMARY.md` - Update completion percentage
- [ ] `docs/QUICK_START_MISSING_FEATURES.md` - Mark features as complete
- [ ] `docs/CORE_FOCUSED_ROADMAP.md` - Update progress
- [ ] `README.md` - Update status

## Success Criteria

**Project is 100% complete when**:
- ✅ All 5 specs implemented
- ✅ All tests passing
- ✅ Documentation updated
- ✅ No breaking changes to existing API
- ✅ Performance targets met

## References

- **Original Azgaar**: `ref-projects/Fantasy-Map-Generator/`
- **Reference C# Port**: `ref-projects/FantasyMapGenerator/`
- **Documentation**: `docs/`
- **Comparison**: `docs/COMPARISON_WITH_ORIGINAL.md`
- **Analysis**: `docs/REFERENCE_PROJECT_ANALYSIS.md`

## Notes

- All specs focus on **core data generation** only
- No rendering code in these specs
- External projects (HyacinthBean.MapViewer) handle visualization
- Maintain clean separation of concerns
- Keep Core library rendering-agnostic

---

**Ready to start? Begin with [001: River Meandering Data Generation](./001-river-meandering-data.md)!** 🚀
