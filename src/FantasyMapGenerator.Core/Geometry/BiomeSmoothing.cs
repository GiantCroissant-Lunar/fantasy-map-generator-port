using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;
using FantasyMapGenerator.Core.Models;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// Smooths biome boundaries using morphological operations
/// </summary>
public class BiomeSmoothing
{
    private readonly NtsGeometryAdapter _adapter;

    public BiomeSmoothing()
    {
        _adapter = new NtsGeometryAdapter();
    }

    /// <summary>
    /// Smooth biome boundaries using morphological closing (buffer + negative buffer)
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="biomeId">The biome ID to smooth</param>
    /// <param name="smoothRadius">Smoothing radius (larger = smoother borders)</param>
    public void SmoothBiomeBoundaries(MapData map, int biomeId, double smoothRadius)
    {
        // Get all cells of this biome
        var biomeCells = map.Cells.Where(c => c.Biome == biomeId).ToList();
        
        if (biomeCells.Count == 0) return;

        // Union into single region
        var region = _adapter.UnionCells(biomeCells, map.Vertices);

        // Smooth via morphological closing (buffer + negative buffer)
        var smoothed = region.Buffer(smoothRadius).Buffer(-smoothRadius);

        // Simplify to reduce vertex count
        smoothed = DouglasPeuckerSimplifier.Simplify(smoothed, smoothRadius * 0.1);

        // Update cells based on new boundary
        UpdateCellsFromGeometry(map, smoothed, biomeId);
    }

    /// <summary>
    /// Smooth all biome boundaries in the map
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="smoothRadius">Smoothing radius (larger = smoother borders)</param>
    public void SmoothAllBiomeBoundaries(MapData map, double smoothRadius)
    {
        // Get all unique biome IDs
        var biomeIds = map.Cells
            .Where(c => c.Biome >= 0)
            .Select(c => c.Biome)
            .Distinct()
            .ToList();

        // Smooth each biome
        foreach (var biomeId in biomeIds)
        {
            SmoothBiomeBoundaries(map, biomeId, smoothRadius);
        }
    }

    /// <summary>
    /// Smooth biome boundaries with adaptive radius based on biome size
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="baseRadius">Base smoothing radius</param>
    /// <param name="minRadius">Minimum smoothing radius</param>
    /// <param name="maxRadius">Maximum smoothing radius</param>
    public void SmoothBiomeBoundariesAdaptive(MapData map, double baseRadius, double minRadius, double maxRadius)
    {
        // Get all unique biome IDs
        var biomeIds = map.Cells
            .Where(c => c.Biome >= 0)
            .Select(c => c.Biome)
            .Distinct()
            .ToList();

        foreach (var biomeId in biomeIds)
        {
            var biomeCells = map.Cells.Where(c => c.Biome == biomeId).ToList();
            if (biomeCells.Count == 0) continue;

            // Calculate adaptive radius based on biome size
            var biomeArea = biomeCells.Count;
            var totalCells = map.Cells.Count;
            var sizeRatio = (double)biomeArea / totalCells;
            
            // Larger biomes get larger smoothing radius
            var adaptiveRadius = Math.Clamp(baseRadius * Math.Sqrt(sizeRatio) * 10, minRadius, maxRadius);

            SmoothBiomeBoundaries(map, biomeId, adaptiveRadius);
        }
    }

    /// <summary>
    /// Apply smoothing only to specific biome types (e.g., coastlines)
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="biomeTypes">Biome types to smooth</param>
    /// <param name="smoothRadius">Smoothing radius</param>
    public void SmoothSpecificBiomes(MapData map, IEnumerable<int> biomeTypes, double smoothRadius)
    {
        foreach (var biomeId in biomeTypes)
        {
            SmoothBiomeBoundaries(map, biomeId, smoothRadius);
        }
    }

    /// <summary>
    /// Smooth coastlines (transition between land and ocean biomes)
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="smoothRadius">Smoothing radius</param>
    public void SmoothCoastlines(MapData map, double smoothRadius)
    {
        // Find coastline cells (land cells adjacent to ocean)
        var coastlineCells = map.Cells.Where(c => c.IsLand && 
            c.Neighbors.Any(n => n >= 0 && map.Cells[n].IsOcean)).ToList();

        if (coastlineCells.Count == 0) return;

        // Create coastline region
        var coastlineRegion = _adapter.UnionCells(coastlineCells, map.Vertices);

        // Smooth coastline
        var smoothed = coastlineRegion.Buffer(smoothRadius).Buffer(-smoothRadius);
        smoothed = DouglasPeuckerSimplifier.Simplify(smoothed, smoothRadius * 0.1);

        // Update coastline cells
        UpdateCellsFromGeometry(map, smoothed, -1, preserveNonCoastal: true);
    }

    /// <summary>
    /// Apply iterative smoothing for more natural borders
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="biomeId">The biome ID to smooth</param>
    /// <param name="smoothRadius">Initial smoothing radius</param>
    /// <param name="iterations">Number of smoothing iterations</param>
    /// <param name="decayFactor">Radius decay factor per iteration</param>
    public void SmoothBiomeBoundariesIterative(MapData map, int biomeId, double smoothRadius, int iterations = 3, double decayFactor = 0.7)
    {
        var currentRadius = smoothRadius;
        
        for (int i = 0; i < iterations; i++)
        {
            SmoothBiomeBoundaries(map, biomeId, currentRadius);
            currentRadius *= decayFactor;
        }
    }

    /// <summary>
    /// Update cell biome assignments based on a smoothed geometry
    /// </summary>
    private void UpdateCellsFromGeometry(MapData map, NtsGeometry geometry, int biomeId, bool preserveNonCoastal = false)
    {
        foreach (var cell in map.Cells)
        {
            // Skip if preserving non-coastal cells and this is not a coastline cell
            if (preserveNonCoastal)
            {
                var isCoastline = cell.IsLand && 
                    cell.Neighbors.Any(n => n >= 0 && map.Cells[n].IsOcean);
                if (!isCoastline) continue;
            }

            var cellPoly = _adapter.CellToPolygon(cell, map.Vertices);
            var cellCenter = cellPoly.Centroid;

            // If cell center is in smoothed geometry, assign biome
            if (geometry.Contains(cellCenter))
            {
                if (biomeId >= 0)
                {
                    cell.Biome = biomeId;
                }
                // For coastline smoothing (biomeId = -1), we don't change biome type
            }
        }
    }

    /// <summary>
    /// Get recommended smoothing radius based on map size
    /// </summary>
    /// <param name="map">The map data</param>
    /// <returns>Recommended smoothing radius</returns>
    public static double GetRecommendedSmoothingRadius(MapData map)
    {
        // Use 1-2% of map dimension as default smoothing radius
        var mapDimension = Math.Sqrt(map.Width * map.Height);
        return mapDimension * 0.015; // 1.5% of map size
    }

    /// <summary>
    /// Validate smoothing parameters
    /// </summary>
    /// <param name="map">The map data</param>
    /// <param name="smoothRadius">Smoothing radius to validate</param>
    /// <returns>True if parameters are valid</returns>
    public static bool ValidateSmoothingParameters(MapData map, double smoothRadius)
    {
        if (smoothRadius <= 0) return false;
        
        // Smoothing radius should not be more than 10% of map dimension
        var mapDimension = Math.Sqrt(map.Width * map.Height);
        var maxRadius = mapDimension * 0.1;
        
        return smoothRadius <= maxRadius;
    }
}