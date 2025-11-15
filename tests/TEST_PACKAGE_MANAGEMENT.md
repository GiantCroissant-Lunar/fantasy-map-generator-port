# Test Package Management Strategy

## Overview

The test projects now use a **hierarchical package management** structure with test-specific dependencies isolated from production code.

## Structure

```
fantasy-map-generator-port/
├── Directory.Packages.props              # Root: Production dependencies only
└── tests/
    ├── Directory.Packages.props          # Tests: All test dependencies
    ├── FantasyMapGenerator.Core.Tests/   # xUnit tests
    └── FantasyMapGenerator.PropertyTests/ # Expecto + FsCheck tests
```

## Package Distribution

### Root `Directory.Packages.props`
**Production dependencies only:**
- Core: BruTile, NetTopologySuite, System.Text.Json
- SkiaSharp: Rendering libraries
- Avalonia: UI framework
- MVVM: CommunityToolkit.Mvvm

### Tests `Directory.Packages.props`
**All testing dependencies:**
- xUnit: Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio
- Coverage: coverlet.collector
- Benchmarking: BenchmarkDotNet
- Property Testing: Expecto, FsCheck, Expecto.FsCheck

## Benefits

### 1. Separation of Concerns
- Production code doesn't reference test frameworks
- Test dependencies don't pollute production builds
- Clear boundary between runtime and test-time dependencies

### 2. Easier Maintenance
- Update test frameworks without touching production packages
- Test-specific versions can differ from production if needed
- Simpler dependency graph for production builds

### 3. Better Build Performance
- Production projects don't restore test packages
- Smaller dependency closure for production builds
- Faster CI/CD for production-only changes

### 4. Clearer Intent
- Developers immediately know which packages are for testing
- New team members understand the architecture faster
- Package purpose is self-documenting

## How It Works

.NET's Central Package Management (CPM) supports hierarchical `Directory.Packages.props` files:

1. **Root level** - Applies to all projects
2. **Subdirectory level** - Applies to projects in that directory and below
3. **Merge behavior** - Subdirectory packages are added to root packages

In our case:
- Root defines production packages
- `tests/` defines test packages
- Test projects get both sets of packages
- Production projects only get root packages

## Verification

### Check production project dependencies:
```bash
dotnet list src/FantasyMapGenerator.Core/FantasyMapGenerator.Core.csproj package
```

Should NOT include xUnit, Expecto, or FsCheck.

### Check test project dependencies:
```bash
dotnet list tests/FantasyMapGenerator.Core.Tests/FantasyMapGenerator.Core.Tests.csproj package
```

Should include both production AND test packages.

## Adding New Packages

### Production Dependency
Add to **root** `Directory.Packages.props`:
```xml
<PackageVersion Include="NewPackage" Version="1.0.0" />
```

### Test Dependency
Add to **tests** `Directory.Packages.props`:
```xml
<PackageVersion Include="NewTestPackage" Version="1.0.0" />
```

## Migration Notes

Previously, all packages (production + test) were in the root `Directory.Packages.props`. This has been refactored to:

1. Move test packages to `tests/Directory.Packages.props`
2. Keep only production packages in root
3. No changes needed to individual `.csproj` files

## Related Documentation

- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [Directory.Build.props](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory)
- [MSBuild Property Evaluation](https://learn.microsoft.com/visualstudio/msbuild/property-functions)
