using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Unit tests for the RiverMeandering system
/// </summary>
public class RiverMeanderingTests
{
    [Fact]
    public void GenerateMeanderedPath_CreatesMorePointsThanCells()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 5);
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, 0.5);

        // Meandered path should have more points than cells
        // Typically 3-5x more points
        Assert.True(path.Count > river.Cells.Count, 
            $"Meandered path ({path.Count} points) should have more points than cells ({river.Cells.Count})");
    }

    [Fact]
    public void GenerateMeanderedPath_IsDeterministic()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 5);
        var meandering = new RiverMeandering(map);

        var path1 = meandering.GenerateMeanderedPath(river, 0.5);
        var path2 = meandering.GenerateMeanderedPath(river, 0.5);

        // Should generate identical paths
        Assert.Equal(path1.Count, path2.Count);
        for (int i = 0; i < path1.Count; i++)
        {
            Assert.Equal(path1[i].X, path2[i].X, precision: 5);
            Assert.Equal(path1[i].Y, path2[i].Y, precision: 5);
        }
    }

    [Theory]
    [InlineData(0.0)] // Straight
    [InlineData(0.3)] // Light curves
    [InlineData(0.5)] // Moderate curves
    [InlineData(0.8)] // Heavy curves
    [InlineData(1.0)] // Maximum curves
    public void GenerateMeanderedPath_RespectsMeanderingFactor(double factor)
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 5);
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, factor);

        // Should generate a valid path
        Assert.NotEmpty(path);
        
        // Higher meandering factor should generally produce more points
        // (though this depends on the interpolation algorithm)
        Assert.True(path.Count >= river.Cells.Count);
    }

    [Fact]
    public void GenerateMeanderedPath_HandlesShortRivers()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 1);
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, 0.5);

        // Short rivers (1 cell) should return empty or minimal path
        Assert.True(path.Count <= 1);
    }

    [Fact]
    public void GenerateMeanderedPath_HandlesEmptyRiver()
    {
        var map = CreateTestMap();
        var river = new River { Id = 0, Cells = new List<int>() };
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, 0.5);

        // Empty river should return empty path
        Assert.Empty(path);
    }

    [Fact]
    public void GenerateMeanderedPath_PathStartsAtSource()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 5);
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, 0.5);

        // First point should be at or near the source cell center
        var sourceCell = map.Cells[river.Cells[0]];
        var firstPoint = path[0];
        
        Assert.Equal(sourceCell.Center.X, firstPoint.X, precision: 1);
        Assert.Equal(sourceCell.Center.Y, firstPoint.Y, precision: 1);
    }

    [Fact]
    public void GenerateMeanderedPath_PathEndsAtMouth()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 5);
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, 0.5);

        // Last point should be at or near the mouth cell center
        var mouthCell = map.Cells[river.Cells[^1]];
        var lastPoint = path[^1];
        
        Assert.Equal(mouthCell.Center.X, lastPoint.X, precision: 1);
        Assert.Equal(mouthCell.Center.Y, lastPoint.Y, precision: 1);
    }

    [Fact]
    public void GenerateMeanderedPath_PointsAreOrdered()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 5);
        var meandering = new RiverMeandering(map);

        var path = meandering.GenerateMeanderedPath(river, 0.5);

        // Points should form a continuous path (no huge jumps)
        for (int i = 1; i < path.Count; i++)
        {
            var distance = path[i - 1].DistanceTo(path[i]);
            
            // Adjacent points should be reasonably close
            // (within ~50 units for our test map scale)
            Assert.True(distance < 50, 
                $"Distance between adjacent points ({distance}) should be reasonable");
        }
    }

    [Fact]
    public void MapGeneration_WithMeandering_PopulatesRiverPaths()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableRiverMeandering = true,
            MeanderingFactor = 0.5
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Check that rivers have meandered paths
        if (map.Rivers.Count > 0)
        {
            foreach (var river in map.Rivers)
            {
                Assert.NotEmpty(river.MeanderedPath);
                Assert.True(river.MeanderedPath.Count >= river.Cells.Count,
                    $"River {river.Id} meandered path ({river.MeanderedPath.Count}) should have at least as many points as cells ({river.Cells.Count})");
            }
        }
    }

    [Fact]
    public void MapGeneration_WithMeanderingDisabled_EmptyPaths()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableRiverMeandering = false,
            MeanderingFactor = 0.5
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Check that rivers have empty meandered paths when disabled
        if (map.Rivers.Count > 0)
        {
            foreach (var river in map.Rivers)
            {
                Assert.Empty(river.MeanderedPath);
            }
        }
    }

    [Fact]
    public void MapGeneration_MeanderingIsReproducible()
    {
        var settings1 = new MapGenerationSettings
        {
            Seed = 99999,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableRiverMeandering = true,
            MeanderingFactor = 0.5
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = 99999,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            EnableRiverMeandering = true,
            MeanderingFactor = 0.5
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();
        
        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // Should generate same number of rivers
        Assert.Equal(map1.Rivers.Count, map2.Rivers.Count);

        // Rivers should have identical meandered paths
        for (int i = 0; i < map1.Rivers.Count; i++)
        {
            var river1 = map1.Rivers[i];
            var river2 = map2.Rivers[i];

            Assert.Equal(river1.MeanderedPath.Count, river2.MeanderedPath.Count);
            
            for (int j = 0; j < river1.MeanderedPath.Count; j++)
            {
                Assert.Equal(river1.MeanderedPath[j].X, river2.MeanderedPath[j].X, precision: 5);
                Assert.Equal(river1.MeanderedPath[j].Y, river2.MeanderedPath[j].Y, precision: 5);
            }
        }
    }

    [Fact]
    public void GenerateMeanderedPath_PerformanceTest()
    {
        var map = CreateTestMap();
        var river = CreateTestRiver(map, cellCount: 50); // Typical river length
        var meandering = new RiverMeandering(map);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var path = meandering.GenerateMeanderedPath(river, 0.5);
        stopwatch.Stop();

        // Should complete in less than 100ms for a 50-cell river
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"Meandering generation took {stopwatch.ElapsedMilliseconds}ms, should be < 100ms");
        
        // Should generate a reasonable number of points
        Assert.True(path.Count >= river.Cells.Count);
        Assert.True(path.Count <= river.Cells.Count * 10); // Not too many points
    }

    // Helper methods

    private MapData CreateTestMap()
    {
        var map = new MapData(100, 100, 100);
        
        // Create simple grid of cells
        for (int i = 0; i < 100; i++)
        {
            var x = (i % 10) * 10 + 5;
            var y = (i / 10) * 10 + 5;
            var point = new Point(x, y);
            
            var cell = new Cell(i, point);
            
            // Set neighbors (simple grid connectivity)
            if (i % 10 > 0) cell.Neighbors.Add(i - 1);
            if (i % 10 < 9) cell.Neighbors.Add(i + 1);
            if (i >= 10) cell.Neighbors.Add(i - 10);
            if (i < 90) cell.Neighbors.Add(i + 10);
            
            // Create elevation variation
            int row = i / 10;
            int col = i % 10;
            
            cell.IsBorder = i % 10 == 0 || i % 10 == 9 || i < 10 || i >= 90;
            
            if (cell.IsBorder)
            {
                cell.Height = 0;
            }
            else
            {
                int distFromCenterX = Math.Abs(col - 5);
                int distFromCenterY = Math.Abs(row - 5);
                int distFromCenter = distFromCenterX + distFromCenterY;
                cell.Height = (byte)(60 - distFromCenter * 5);
            }
            
            map.Cells.Add(cell);
        }
        
        return map;
    }

    private River CreateTestRiver(MapData map, int cellCount)
    {
        var river = new River
        {
            Id = 0,
            Cells = new List<int>()
        };

        // Create a river path from center to edge
        // Start at center (cell 55) and flow to edge
        int currentCell = 55; // Center of 10x10 grid
        
        for (int i = 0; i < cellCount && currentCell >= 0; i++)
        {
            river.Cells.Add(currentCell);
            
            // Move to a neighbor (flow downhill)
            var cell = map.Cells[currentCell];
            if (cell.Neighbors.Count > 0)
            {
                // Find lowest neighbor
                int lowestNeighbor = -1;
                int lowestHeight = int.MaxValue;
                
                foreach (var neighborId in cell.Neighbors)
                {
                    var neighbor = map.Cells[neighborId];
                    if (neighbor.Height < lowestHeight)
                    {
                        lowestHeight = neighbor.Height;
                        lowestNeighbor = neighborId;
                    }
                }
                
                currentCell = lowestNeighbor;
            }
            else
            {
                break;
            }
        }

        if (river.Cells.Count > 0)
        {
            river.Source = river.Cells[0];
            river.Mouth = river.Cells[^1];
        }

        return river;
    }
}
