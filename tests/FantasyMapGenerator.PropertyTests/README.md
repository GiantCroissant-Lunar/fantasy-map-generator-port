# Fantasy Map Generator - Property-Based Tests

This project contains property-based tests using **Expecto + FsCheck** to verify invariants and correctness of the Fantasy Map Generator procedural generation system.

## Why Property-Based Testing?

Procedural generation systems like FMG are perfect candidates for property-based testing because:

- **Bugs appear under specific random seeds** - Traditional unit tests can't catch edge cases that only appear with certain seeds
- **Invariants must hold across all inputs** - Rivers must flow downhill, geometry must be valid, etc.
- **Determinism is critical** - Same seed must always produce the same map
- **FsCheck automatically shrinks failing cases** - When a test fails, FsCheck finds the minimal failing input

## Test Suites

### 1. Hydrology Property Tests (`HydrologyPropertyTests.fs`)

Verifies water system invariants:

- **Rivers flow downhill** - Each river segment must be at equal or lower elevation than the previous
- **Valid river cell indices** - All river cells reference valid cell IDs
- **Source higher than mouth** - River sources must be at higher elevation than mouths
- **Minimum river length** - Rivers must have at least 2 cells

### 2. Geometry Property Tests (`GeometryPropertyTests.fs`)

Verifies Voronoi diagram and mesh properties:

- **Valid neighbor references** - All cell neighbors reference valid cell indices
- **Non-degenerate cells** - Each cell has 3-10 neighbors (triangulation property)
- **Height range** - All heights are within 0-100 byte range
- **Biomes assigned** - All cells have valid biome assignments
- **No self-references** - Cells don't reference themselves as neighbors

### 3. Determinism Property Tests (`DeterminismPropertyTests.fs`)

Verifies reproducibility:

- **Same seed = identical maps** - Running generation twice with same seed produces identical results
- **Different seeds = different maps** - Different seeds produce different maps
- **Cell count consistency** - Generated cell count is within expected range

## Running the Tests

### Run all property tests:
```bash
dotnet run --project tests/FantasyMapGenerator.PropertyTests
```

### Run with more iterations (stress test):
```bash
dotnet run --project tests/FantasyMapGenerator.PropertyTests -- --fscheck-max-tests 1000
```

### Run specific test:
```bash
dotnet run --project tests/FantasyMapGenerator.PropertyTests -- --filter "rivers flow downhill"
```

### Run with verbose output:
```bash
dotnet run --project tests/FantasyMapGenerator.PropertyTests -- --verbosity detailed
```

## Understanding FsCheck Output

When a property test fails, FsCheck automatically shrinks the failing input to find the minimal counterexample:

```
Falsifiable, after 13 tests (8 shrinks) (StdGen (1383,296)):
Original failing input: 189023
Shrunk failing input: 679
```

This means:
- Test failed on seed `189023`
- FsCheck found a simpler failing case: seed `679`
- You can now debug with `seed = 679` to reproduce the issue

## Adding New Property Tests

1. Create a new test function:
```fsharp
let myNewProperty (seed: int) =
    let settings = MapGenerationSettings(Seed = seed, Width = 800, Height = 600, NumPoints = 1000)
    let generator = MapGenerator()
    let mapData = generator.Generate(settings)
    
    // Your invariant check here
    mapData.Cells |> Seq.forall (fun c -> c.Height >= 0uy)
```

2. Add it to the test list:
```fsharp
[<Tests>]
let myTests =
    testList "My Tests" [
        testProperty "my property description" myNewProperty
    ]
```

3. Register in `Program.fs`:
```fsharp
let allTests =
    testList "Fantasy Map Generator Property Tests" [
        HydrologyPropertyTests.hydrologyTests
        DeterminismPropertyTests.determinismTests
        GeometryPropertyTests.geometryTests
        MyTests.myTests  // Add your new suite
    ]
```

## Performance Considerations

Property tests run map generation many times (default: 100 iterations per property). Each test:
- Generates a complete map (800x600, 1000 points)
- Takes ~100-500ms per iteration
- Total runtime: ~1-5 minutes for full suite

For faster feedback during development:
```bash
# Run with fewer iterations
dotnet run --project tests/FantasyMapGenerator.PropertyTests -- --fscheck-max-tests 10
```

## Integration with CI/CD

Add to your CI pipeline:
```yaml
- name: Run Property Tests
  run: dotnet run --project tests/FantasyMapGenerator.PropertyTests -- --fscheck-max-tests 100
```

## Further Reading

- [Expecto Documentation](https://github.com/haf/expecto)
- [FsCheck Documentation](https://fscheck.github.io/FsCheck/)
- [Property-Based Testing Guide](https://fsharpforfunandprofit.com/posts/property-based-testing/)
