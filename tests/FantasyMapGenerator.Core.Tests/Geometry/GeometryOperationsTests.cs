using Xunit;
using NetTopologySuite.Geometries;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Generators;

namespace FantasyMapGenerator.Core.Tests.Geometry;

/// <summary>
/// Unit tests for geometry operations and NTS integration
/// </summary>
public class GeometryOperationsTests
{
    private readonly MapData _testMap;
    private readonly NtsGeometryAdapter _adapter;
    private readonly SpatialMapData _spatialMap;

    public GeometryOperationsTests()
    {
        // Create a simple test map for geometry operations
        var generator = new MapGenerator();
        var settings = new MapGenerationSettings
        {
            Width = 100,
            Height = 100,
            NumPoints = 500,
            Seed = 12345
        };
        _testMap = generator.Generate(settings);
        _spatialMap = new SpatialMapData(100, 100, 500);
        
        // Copy data from test map to spatial map
        _spatialMap.Vertices.AddRange(_testMap.Vertices);
        _spatialMap.Cells.AddRange(_testMap.Cells);
        _spatialMap.States.AddRange(_testMap.States);
        _spatialMap.Rivers.AddRange(_testMap.Rivers);
        _spatialMap.Burgs.AddRange(_testMap.Burgs);
        
        _spatialMap.BuildSpatialIndex();
        _adapter = new NtsGeometryAdapter();
    }

    [Fact]
    public void NtsGeometryAdapter_CellToPolygon_ShouldCreateValidPolygon()
    {
        // Arrange
        var cell = _testMap.Cells.First();

        // Act
        var polygon = _adapter.CellToPolygon(cell, _testMap.Vertices);

        // Assert
        Assert.NotNull(polygon);
        Assert.True(polygon.IsValid);
        Assert.True(polygon.Area > 0);
        Assert.Equal(cell.Vertices.Count + 1, polygon.Coordinates.Length); // +1 for closing coordinate
    }

    [Fact]
    public void NtsGeometryAdapter_RiverToLineString_ShouldCreateValidLineString()
    {
        // Arrange
        var river = _testMap.Rivers.FirstOrDefault(r => r.Cells.Count > 1);
        if (river == null)
        {
            // Skip test if no rivers exist
            return;
        }

        // Act
        var lineString = _adapter.RiverToLineString(river, _testMap);

        // Assert
        Assert.NotNull(lineString);
        Assert.True(lineString.IsValid);
        Assert.True(lineString.Length > 0);
        Assert.Equal(river.Cells.Count, lineString.Coordinates.Length);
    }

    [Fact]
    public void SpatialMapData_BuildSpatialIndex_ShouldCreateValidIndexes()
    {
        // Arrange
        var spatialMap = new SpatialMapData(100, 100, 500);
        spatialMap.Vertices.AddRange(_testMap.Vertices);
        spatialMap.Cells.AddRange(_testMap.Cells);
        spatialMap.States.AddRange(_testMap.States);
        spatialMap.Rivers.AddRange(_testMap.Rivers);
        spatialMap.Burgs.AddRange(_testMap.Burgs);

        // Act
        spatialMap.BuildSpatialIndex();

        // Assert
        Assert.True(spatialMap.IsIndexBuilt);
        var stats = spatialMap.GetIndexStatistics();
        Assert.True(stats.IndexBuilt);
        Assert.Equal(_testMap.Cells.Count, stats.IndexedCells);
        Assert.Equal(_testMap.States.Count, stats.IndexedStates);
        Assert.Equal(_testMap.Rivers.Count, stats.IndexedRivers);
        Assert.Equal(_testMap.Burgs.Count, stats.IndexedBurgs);
    }

    [Fact]
    public void SpatialMapData_QueryCellsInRadius_ShouldReturnCorrectCells()
    {
        // Arrange
        var center = _testMap.Cells.First().Center;
        var radius = 10.0;

        // Act
        var results = _spatialMap.QueryCellsInRadius(center, radius);

        // Assert
        Assert.NotEmpty(results);
        foreach (var cell in results)
        {
            var distance = Math.Sqrt(
                Math.Pow(cell.Center.X - center.X, 2) + 
                Math.Pow(cell.Center.Y - center.Y, 2));
            Assert.True(distance <= radius);
        }
    }

    [Fact]
    public void SpatialMapData_QueryCellsInRectangle_ShouldReturnCorrectCells()
    {
        // Arrange - use a larger rectangle that should definitely contain some cells
        var minX = 10.0;
        var minY = 10.0;
        var maxX = 90.0;
        var maxY = 90.0;

        // Act
        var results = _spatialMap.QueryCellsInRectangle(minX, minY, maxX, maxY);

        // Assert
        Assert.NotEmpty(results);
        foreach (var cell in results)
        {
            Assert.True(cell.Center.X >= minX && cell.Center.X <= maxX);
            Assert.True(cell.Center.Y >= minY && cell.Center.Y <= maxY);
        }
    }

    [Fact]
    public void SpatialMapData_FindNearestCell_ShouldReturnClosestCell()
    {
        // Arrange
        var queryPoint = new Models.Point(50.0, 50.0);

        // Act
        var nearest = _spatialMap.FindNearestCell(queryPoint);

        // Assert
        Assert.NotNull(nearest);
        
        // Verify it's actually the nearest
        var allDistances = _testMap.Cells
            .Select(c => Math.Sqrt(Math.Pow(c.Center.X - queryPoint.X, 2) + 
                                 Math.Pow(c.Center.Y - queryPoint.Y, 2)))
            .OrderBy(d => d)
            .ToList();
        
        var nearestDistance = Math.Sqrt(
            Math.Pow(nearest.Center.X - queryPoint.X, 2) + 
            Math.Pow(nearest.Center.Y - queryPoint.Y, 2));
        
        Assert.Equal(allDistances.First(), nearestDistance, 5);
    }

    [Fact]
    public void BiomeSmoothing_ApplySmoothing_ShouldMaintainCellCount()
    {
        // Arrange
        var originalCellCount = _testMap.Cells.Count;
        var biomeGroups = _testMap.Cells.GroupBy(c => c.Biome).ToList();
        var smoother = new BiomeSmoothing();

        // Act
        smoother.SmoothAllBiomeBoundaries(_testMap, 0.5);
        var result = _testMap.Cells;

        // Assert
        Assert.Equal(originalCellCount, result.Count);
        
        // Should have same biomes (though distribution may change)
        var originalBiomes = _testMap.Cells.Select(c => c.Biome).Distinct().ToHashSet();
        var resultBiomes = result.Select(c => c.Biome).Distinct().ToHashSet();
        Assert.True(originalBiomes.SetEquals(resultBiomes));
    }

    [Fact]
    public void StateBoundaryGenerator_GetStateBoundary_ShouldReturnValidGeometry()
    {
        // Arrange
        var state = _testMap.States.FirstOrDefault();
        if (state == null)
        {
            // Skip test if no states exist
            return;
        }

        var boundaryGenerator = new StateBoundaryGenerator();

        // Act
        var boundary = boundaryGenerator.GetStateBoundary(state.Id, _testMap);

        // Assert
        Assert.NotNull(boundary);
        Assert.True(boundary.IsValid);
        Assert.True(boundary.Area > 0);
    }

    [Fact]
    public void RainShadowCalculator_ApplyRainShadowEffects_ShouldModifyMoisture()
    {
        // Arrange
        var spatialMap = new SpatialMapData(100, 100, 500);
        spatialMap.Vertices.AddRange(_testMap.Vertices);
        spatialMap.Cells.AddRange(_testMap.Cells);
        spatialMap.States.AddRange(_testMap.States);
        spatialMap.Rivers.AddRange(_testMap.Rivers);
        spatialMap.Burgs.AddRange(_testMap.Burgs);
        spatialMap.BuildSpatialIndex();

        var calculator = new RainShadowCalculator(spatialMap);
        var originalMoisture = spatialMap.Cells.Select(c => c.Precipitation).ToList();

        // Act - use lower mountain threshold for test data
        var config = new RainShadowCalculator.RainShadowConfig
        {
            MountainThreshold = 0.3 // Lower threshold to ensure some mountains exist
        };
        calculator.ApplyRainShadowEffects(config);

        // Assert
        var newMoisture = spatialMap.Cells.Select(c => c.Precipitation).ToList();
        
        // Some moisture values should have changed
        var changes = originalMoisture.Zip(newMoisture, (old, @new) => Math.Abs(old - @new)).ToList();
        Assert.True(changes.Any(change => change > 0.01));
        
        // All values should still be in valid range
        Assert.All(newMoisture, moisture => Assert.True(moisture >= 0.0 && moisture <= 1.0, $"Moisture {moisture} is not in range [0.0, 1.0]"));
    }

    [Fact]
    public void RainShadowCalculator_GetStatistics_ShouldReturnValidStats()
    {
        // Arrange
        var calculator = new RainShadowCalculator(_spatialMap);
        var config = new RainShadowCalculator.RainShadowConfig();

        // Act
        var stats = calculator.GetStatistics(config);

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.MountainCells >= 0);
        Assert.True(stats.ShadowedCells >= 0);
        Assert.True(stats.WindwardCells >= 0);
        Assert.True(stats.AverageMoistureChange >= -1.0);
        Assert.True(stats.AverageMoistureChange <= 1.0);
    }

    [Fact]
    public void GeoJsonExporter_ExportToGeoJson_ShouldReturnValidJson()
    {
        // Arrange
        var exporter = new GeoJsonExporter();
        var config = new GeoJsonExporter.GeoJsonExportConfig();

        // Act
        var geoJson = exporter.ExportToGeoJson(_testMap, config);

        // Assert
        Assert.NotNull(geoJson);
        Assert.NotEmpty(geoJson);
        
        // Should be valid JSON
        Assert.Contains("\"type\": \"FeatureCollection\"", geoJson);
        Assert.Contains("\"features\"", geoJson);
    }

    [Fact]
    public void GeoJsonExporter_GetExportStatistics_ShouldReturnValidStats()
    {
        // Arrange
        var exporter = new GeoJsonExporter();
        var config = new GeoJsonExporter.GeoJsonExportConfig();

        // Act
        var stats = exporter.GetExportStatistics(_testMap, config);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(_testMap.Cells.Count, stats.CellCount);
        Assert.Equal(_testMap.States.Count, stats.StateCount);
        Assert.Equal(_testMap.Rivers.Count, stats.RiverCount);
        Assert.Equal(_testMap.Burgs.Count, stats.BurgCount);
        Assert.True(stats.EstimatedFileSizeBytes > 0);
        Assert.NotEmpty(stats.EstimatedFileSize);
    }

    [Fact]
    public void SpatialMapData_QueryBurgsInRadius_ShouldReturnCorrectBurgs()
    {
        // Arrange
        if (_testMap.Burgs.Count == 0)
        {
            // Skip test if no burgs exist
            return;
        }

        var center = _testMap.Burgs.First().Position;
        var radius = 15.0;

        // Act
        var results = _spatialMap.QueryBurgsInRadius(center, radius);

        // Assert
        Assert.NotEmpty(results);
        foreach (var burg in results)
        {
            var burgCenter = _testMap.Cells[burg.Cell].Center;
            var distance = Math.Sqrt(
                Math.Pow(burgCenter.X - center.X, 2) + 
                Math.Pow(burgCenter.Y - center.Y, 2));
            Assert.True(distance <= radius);
        }
    }

    [Fact]
    public void SpatialMapData_PerformanceTest_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var spatialMap = new SpatialMapData(200, 200, 2000); // Larger map for performance test
        var generator = new MapGenerator();
        var settings = new MapGenerationSettings
        {
            Width = 200,
            Height = 200,
            NumPoints = 2000,
            Seed = 12345
        };
        var largeMap = generator.Generate(settings);
        
        spatialMap.Vertices.AddRange(largeMap.Vertices);
        spatialMap.Cells.AddRange(largeMap.Cells);
        spatialMap.States.AddRange(largeMap.States);
        spatialMap.Rivers.AddRange(largeMap.Rivers);
        spatialMap.Burgs.AddRange(largeMap.Burgs);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        spatialMap.BuildSpatialIndex();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        Assert.True(spatialMap.IsIndexBuilt);
    }

    [Theory]
    [InlineData(0.1)]  // Small tolerance
    [InlineData(0.5)]  // Medium tolerance
    [InlineData(1.0)]  // Large tolerance
    public void StateBoundaryGenerator_SimplifyBoundaries_ShouldMaintainTopology(double tolerance)
    {
        // Arrange
        var state = _testMap.States.FirstOrDefault();
        if (state == null) return;

        var boundaryGenerator = new StateBoundaryGenerator();

        // Act
        var originalBoundary = boundaryGenerator.GetStateBoundary(state.Id, _testMap);
        var simplifiedBoundary = boundaryGenerator.GetStateBoundary(state.Id, _testMap, tolerance);

        // Assert
        Assert.NotNull(simplifiedBoundary);
        Assert.True(simplifiedBoundary.IsValid);
        Assert.True(simplifiedBoundary.Area > 0);
        
        // Simplified boundary should have fewer or equal points
        Assert.True(simplifiedBoundary.Coordinates.Length <= originalBoundary.Coordinates.Length);
    }
}
