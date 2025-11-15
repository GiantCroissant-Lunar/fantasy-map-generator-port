# Implementation Guide: Using Kiro Specs

## Quick Start

You now have **5 Kiro specs** ready to implement the missing core features!

### 📋 Specs Created

1. **[001-river-meandering-data.md](.kiro/specs/001-river-meandering-data.md)** ⭐⭐⭐⭐⭐
   - Generate meandered path points
   - 2-3 hours
   - Biggest visual impact

2. **[002-river-erosion-algorithm.md](.kiro/specs/002-river-erosion-algorithm.md)** ⭐⭐⭐
   - Rivers carve valleys
   - 1-2 hours
   - Adds terrain depth

3. **[003-lake-evaporation-model.md](.kiro/specs/003-lake-evaporation-model.md)** ⭐⭐
   - Closed basins (salt lakes)
   - 3-4 hours
   - Realistic hydrology

4. **[004-advanced-erosion-algorithm.md](.kiro/specs/004-advanced-erosion-algorithm.md)** ⭐⭐⭐⭐
   - Better terrain shaping
   - 4-6 hours
   - From reference project

5. **[005-lloyd-relaxation.md](.kiro/specs/005-lloyd-relaxation.md)** ⭐⭐⭐
   - Uniform cell distribution
   - 2-3 hours
   - Optional enhancement

**Total**: 13-18 hours to reach 100% completion

---

## Using Kiro Specs

### Option 1: Kiro IDE Workflow (Recommended)

1. **Open a spec** in Kiro IDE
2. **Review** requirements and design
3. **Let Kiro implement** using the spec workflow
4. **Review changes** and test
5. **Mark tasks complete** in the spec
6. **Move to next spec**

### Option 2: Manual Implementation

1. **Read the spec** thoroughly
2. **Follow implementation tasks** in order
3. **Write code** as specified
4. **Write tests** from testing section
5. **Verify success criteria**
6. **Update spec status**

---

## Recommended Implementation Order

### 🔴 Week 1: High Priority (Core Azgaar Features)

**Day 1-2**: Spec 001 - River Meandering
```bash
# Open spec in Kiro
# Implement RiverMeandering.cs
# Update River.cs model
# Integrate with HydrologyGenerator
# Run tests
```

**Day 3**: Spec 002 - River Erosion
```bash
# Add DowncutRivers() method
# Update configuration
# Run tests
```

**Day 4-5**: Spec 003 - Lake Evaporation
```bash
# Create Lake.cs model
# Implement evaporation calculation
# Integrate with hydrology
# Run tests
```

**Result**: 93% complete, core Azgaar features done ✅

### 🟡 Week 2: Medium Priority (Reference Project Algorithms)

**Day 1-2**: Spec 004 - Advanced Erosion
```bash
# Implement ApplyAdvancedErosion()
# Add configuration
# Compare with simple erosion
# Run tests
```

**Day 3**: Spec 005 - Lloyd Relaxation
```bash
# Implement ApplyLloydRelaxation()
# Add to point generation
# Run tests
```

**Day 4-5**: Polish & Documentation
```bash
# Run all tests
# Update documentation
# Performance benchmarks
# Final review
```

**Result**: 100% complete ✅

---

## Testing Each Spec

### Run Tests After Each Implementation

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=RiverMeanderingTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Verify Success Criteria

Each spec has a "Success Criteria" section. Check all items before moving to next spec.

---

## Progress Tracking

### Update Spec Status

After completing a spec, update its front matter:

```markdown
---
title: River Meandering Data Generation
status: completed  # Changed from 'draft'
priority: high
estimated_effort: 2-3 hours
actual_effort: 2.5 hours  # Add actual time
completed_date: 2025-11-15  # Add completion date
---
```

### Update Progress Table

Update `.kiro/specs/README.md`:

```markdown
| Spec | Status | Progress | Completed |
|------|--------|----------|-----------|
| 001-river-meandering-data | Completed | 100% | ✅ |
| 002-river-erosion-algorithm | In Progress | 50% | ⬜ |
```

---

## Documentation Updates

### After Each Spec

Update these files:
- [ ] Spec status (draft → in-progress → completed)
- [ ] `.kiro/specs/README.md` progress table
- [ ] Add implementation notes to spec

### After All Specs Complete

Update these files:
- [ ] `README.md` - Change status to "100% complete"
- [ ] `docs/EXECUTIVE_SUMMARY.md` - Update completion percentage
- [ ] `docs/QUICK_START_MISSING_FEATURES.md` - Mark all features complete
- [ ] `docs/CORE_FOCUSED_ROADMAP.md` - Mark roadmap complete
- [ ] Create `CHANGELOG.md` - Document all additions

---

## Troubleshooting

### If Tests Fail

1. Check the spec's "Testing Strategy" section
2. Review the algorithm implementation
3. Verify configuration settings
4. Check for edge cases
5. Ask Kiro for help debugging

### If Performance Is Slow

1. Check the spec's performance requirements
2. Profile the code
3. Look for unnecessary allocations
4. Consider caching or optimization
5. Review reference implementations

### If Integration Breaks

1. Check the spec's "Integration" section
2. Verify method call order
3. Check configuration flags
4. Run integration tests
5. Review existing code for conflicts

---

## Best Practices

### While Implementing

1. ✅ **Read the entire spec first**
2. ✅ **Follow the implementation tasks in order**
3. ✅ **Write tests as you go** (don't save for later)
4. ✅ **Commit after each phase**
5. ✅ **Update spec status regularly**

### Code Quality

1. ✅ **Add XML documentation** to public methods
2. ✅ **Handle edge cases** (null checks, bounds)
3. ✅ **Add logging** for diagnostics
4. ✅ **Keep methods focused** (single responsibility)
5. ✅ **Credit sources** in comments

### Testing

1. ✅ **Write unit tests first** (TDD approach)
2. ✅ **Test edge cases** (empty lists, null values)
3. ✅ **Test performance** (use Stopwatch)
4. ✅ **Test determinism** (same seed = same result)
5. ✅ **Run all tests** before marking complete

---

## Example: Implementing Spec 001

### Step 1: Read the Spec
```bash
# Open in Kiro or your editor
code .kiro/specs/001-river-meandering-data.md
```

### Step 2: Create Feature Branch
```bash
git checkout -b feature/001-river-meandering
```

### Step 3: Implement Phase by Phase

**Phase 1: Data Model**
```csharp
// Update River.cs
public List<Point> MeanderedPath { get; set; } = new();
```
Commit: "feat: add MeanderedPath property to River model"

**Phase 2: Core Algorithm**
```csharp
// Create RiverMeandering.cs
public class RiverMeandering { /* ... */ }
```
Commit: "feat: implement RiverMeandering class"

**Phase 3: Integration**
```csharp
// Update HydrologyGenerator.cs
private void AddMeanderingToRivers() { /* ... */ }
```
Commit: "feat: integrate river meandering into generation"

**Phase 4: Testing**
```csharp
// Create RiverMeanderingTests.cs
public class RiverMeanderingTests { /* ... */ }
```
Commit: "test: add river meandering tests"

### Step 4: Verify Success Criteria
- [ ] All rivers have populated MeanderedPath
- [ ] Meandered paths have 3-5x more points
- [ ] Generation is deterministic
- [ ] All tests pass

### Step 5: Update Spec Status
```markdown
---
status: completed
actual_effort: 2.5 hours
completed_date: 2025-11-15
---
```

### Step 6: Merge
```bash
git add .
git commit -m "feat: implement river meandering data generation (spec 001)"
git checkout main
git merge feature/001-river-meandering
```

---

## Completion Checklist

### After All Specs Implemented

- [ ] All 5 specs marked as "completed"
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Performance benchmarks run
- [ ] No breaking changes
- [ ] Code reviewed
- [ ] Merged to main
- [ ] Tagged release (v1.0.0)

---

## Questions?

**For spec details**: Read the individual spec files  
**For algorithms**: See `docs/MISSING_FEATURES_GUIDE.md`  
**For comparisons**: See `docs/COMPARISON_WITH_ORIGINAL.md`  
**For roadmap**: See `docs/CORE_FOCUSED_ROADMAP.md`

---

## Summary

You now have **5 production-ready specs** to implement the missing 13% of core features:

1. River meandering (2-3h) ⭐⭐⭐⭐⭐
2. River erosion (1-2h) ⭐⭐⭐
3. Lake evaporation (3-4h) ⭐⭐
4. Advanced erosion (4-6h) ⭐⭐⭐⭐
5. Lloyd relaxation (2-3h) ⭐⭐⭐

**Total**: 13-18 hours to 100% completion

**Start with spec 001 (river meandering) - biggest impact, easiest to implement!** 🚀
