# Task: Implement Advanced Hydrology Features

## Context
The basic hydrology system (rivers, lakes, flow) is complete and working. Now implement 3 optional advanced features from `docs/hydrology-implementation-guide.md` (sections marked "Advanced Features").

## What to Implement

### Feature 1: River Deltas (Guide Section: "River Mouth Delta")
- Create multi-channel deltas for large rivers (accumulation > 500)
- Generate 2-4 distributary channels spreading from main river mouth
- Use `FindCoastalCells()` helper to locate delta outlets within radius 5
- Add `CreateDeltaChannel()` method to trace distributary paths

**Files to modify:**
- `src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs`

### Feature 2: Seasonal Rivers (Guide Section: "Seasonal Rivers")
- Mark rivers as seasonal based on average precipitation along path
- Threshold: precipitation < 30 = seasonal
- Reduce width by 50% for seasonal rivers
- Already have `river.IsSeasonal` property - just implement the logic

**Files to modify:**
- `src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs`

### Feature 3: River Names (Guide Section: "River Names")
- Generate names for top 20 rivers by length/width
- Use provided name arrays (prefixes, names, suffixes)
- Major rivers (width >= 5): "{prefix} {name}" format
- Minor rivers: "{name}{suffix}" format
- Already have `river.Name` property - just implement generation

**Files to modify:**
- `src/FantasyMapGenerator.Core/Generators/HydrologyGenerator.cs`

## Implementation Steps

1. Copy code examples from guide sections 1, 2, 3 under "Advanced Features"
2. Add the three methods to HydrologyGenerator class
3. Call them from `Generate()` method after `CalculateRiverWidths()`
4. Run `dotnet test` - all existing tests must still pass
5. Add 3 simple tests (one per feature) to verify functionality

## Definition of Done

✅ **Code Complete:**
- [ ] `GenerateDeltas()` method added and working
- [ ] `IdentifySeasonalRivers()` method added and working  
- [ ] `GenerateRiverNames()` method added with IRandomSource parameter
- [ ] All three methods called in proper order in `Generate()`

✅ **Quality:**
- [ ] All existing 49 tests still pass (`dotnet test`)
- [ ] 3 new tests added to HydrologyGeneratorTests.cs:
  - [ ] `Deltas_GenerateForLargeRivers` - verify rivers with high accumulation get deltas
  - [ ] `SeasonalRivers_IdentifiedCorrectly` - verify low precipitation = seasonal flag
  - [ ] `RiverNames_GeneratedForMajorRivers` - verify top 20 rivers get names
- [ ] Code builds without warnings in modified files
- [ ] Methods have XML doc comments (copy from guide)

✅ **Constraints:**
- Must follow existing code style in HydrologyGenerator.cs
- Must use guide code examples (lines 526-664) - don't reinvent
- No changes to MapData.cs, River model, or other files
- Total implementation time: ~30-45 minutes

## Testing Command
```bash
dotnet test
```

## Reference
See `docs/hydrology-implementation-guide.md` lines 522-664 for complete code examples.
