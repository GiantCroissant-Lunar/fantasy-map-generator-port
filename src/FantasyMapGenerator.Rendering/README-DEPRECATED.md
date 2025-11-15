# FantasyMapGenerator.Rendering (DEPRECATED)

This project is retained for historical reference and comparison only. It has been superseded by the Map domain rendering stack in the main PigeonPea solution.

## Replacement

Use the Map domain rendering components:
- PigeonPea.Map.Rendering
- PigeonPea.Shared.Rendering

Key types:
- `SkiaMapRasterizer`
- `Tiles` (BruTile-backed tile source)
- `MapDataRenderer`

## Why deprecated?
- Rendering responsibilities are now organized by domain (Map/Dungeon) with clear Core/Control/Rendering boundaries.
- Shared rendering utilities live under `PigeonPea.Shared.Rendering`.
- Modernized pipeline, better test coverage, and ECS integration.

## Migration
See `docs/migrations/fmg-rendering-to-map-rendering.md` for a quick-reference guide with before/after examples.

## Status
- No new development.
- Keep for reference until Phase 6 completes and documentation stabilizes.
