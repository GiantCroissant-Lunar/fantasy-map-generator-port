using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Xunit;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Unit tests for the Lloyd Relaxation system
/// </summary>
public class LloydRelaxationTests
{
    [Fact]
    public void ApplyLloydRelaxation_MovesPoints()
    {
        var points = new List<Point>
        {
            new Point(100, 100),
            new Point(300, 100),
            new Point(200, 300),
            new Point(500, 200),
            new Point(400, 400)
        };

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 1);

        // At least some points should have moved
        int movedPoints = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (Math.Abs(points[i].X - relaxed[i].X) > 1 || 
                Math.Abs(points[i].Y - relaxed[i].Y) > 1)
            {
                movedPoints++;
            }
        }

        Assert.True(movedPoints > 0, "At least some points should move during relaxation");
    }

    [Fact]
    public void ApplyLloydRelaxation_KeepsPointsWithinBounds()
    {
        var random = new PcgRandomSource(12345);
        var points = new List<Point>();
        
        for (int i = 0; i < 100; i++)
        {
            points.Add(new Point(
                random.NextDouble() * 800,
                random.NextDouble() * 600
            ));
        }

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 3);

        Assert.All(relaxed, p =>
        {
            Assert.True(p.X >= 0 && p.X <= 800, $"Point X ({p.X}) out of bounds");
            Assert.True(p.Y >= 0 && p.Y <= 600, $"Point Y ({p.Y}) out of bounds");
        });
    }

    [Fact]
    public void ApplyLloydRelaxation_PreservesPointCount()
    {
        var random = new PcgRandomSource(54321);
        var points = new List<Point>();
        
        for (int i = 0; i < 50; i++)
        {
            points.Add(new Point(
                random.NextDouble() * 400,
                random.NextDouble() * 400
            ));
        }

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 400, 400, 2);

        Assert.Equal(points.Count, relaxed.Count);
    }

    [Fact]
    public void ApplyLloydRelaxation_HandlesEmptyList()
    {
        var points = new List<Point>();

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 1);

        Assert.Empty(relaxed);
    }

    [Fact]
    public void ApplyLloydRelaxation_HandlesSinglePoint()
    {
        var points = new List<Point> { new Point(400, 300) };

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 1);

        Assert.Single(relaxed);
        // Single point should stay roughly in place
        Assert.True(Math.Abs(relaxed[0].X - 400) < 100);
        Assert.True(Math.Abs(relaxed[0].Y - 300) < 100);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ApplyLloydRelaxation_DifferentIterations(int iterations)
    {
        var random = new PcgRandomSource(99999);
        var points = new List<Point>();
        
        for (int i = 0; i < 100; i++)
        {
            points.Add(new Point(
                random.NextDouble() * 800,
                random.NextDouble() * 600
            ));
        }

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, iterations);

        Assert.Equal(points.Count, relaxed.Count);
        Assert.All(relaxed, p =>
        {
            Assert.True(p.X >= 0 && p.X <= 800);
            Assert.True(p.Y >= 0 && p.Y <= 600);
        });
    }

    [Fact]
    public void MapGeneration_WithLloydRelaxation_Enabled()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            ApplyLloydRelaxation = true,
            LloydIterations = 2
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        Assert.NotNull(map);
        Assert.True(map.Cells.Count > 0);
        Assert.Equal(map.Points.Count, map.Cells.Count);
    }

    [Fact]
    public void MapGeneration_WithLloydRelaxation_Disabled()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            ApplyLloydRelaxation = false
        };

        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        Assert.NotNull(map);
        Assert.True(map.Cells.Count > 0);
    }

    [Fact]
    public void MapGeneration_LloydRelaxation_IsDeterministic()
    {
        var settings1 = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            ApplyLloydRelaxation = true,
            LloydIterations = 2
        };

        var settings2 = new MapGenerationSettings
        {
            Seed = 77777,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            ApplyLloydRelaxation = true,
            LloydIterations = 2
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var map1 = generator1.Generate(settings1);
        var map2 = generator2.Generate(settings2);

        // Should generate same number of points
        Assert.Equal(map1.Points.Count, map2.Points.Count);

        // Points should be in same positions
        for (int i = 0; i < Math.Min(map1.Points.Count, map2.Points.Count); i++)
        {
            Assert.Equal(map1.Points[i].X, map2.Points[i].X, precision: 2);
            Assert.Equal(map1.Points[i].Y, map2.Points[i].Y, precision: 2);
        }
    }

    [Fact]
    public void MapGeneration_LloydRelaxation_ProducesDifferentResults()
    {
        var settingsWithout = new MapGenerationSettings
        {
            Seed = 33333,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            ApplyLloydRelaxation = false
        };

        var settingsWith = new MapGenerationSettings
        {
            Seed = 33333,
            Width = 200,
            Height = 200,
            NumPoints = 400,
            ApplyLloydRelaxation = true,
            LloydIterations = 2
        };

        var generator1 = new MapGenerator();
        var generator2 = new MapGenerator();

        var mapWithout = generator1.Generate(settingsWithout);
        var mapWith = generator2.Generate(settingsWith);

        // Should have same number of points
        Assert.Equal(mapWithout.Points.Count, mapWith.Points.Count);

        // But points should be in different positions
        int differentPoints = 0;
        for (int i = 0; i < Math.Min(mapWithout.Points.Count, mapWith.Points.Count); i++)
        {
            if (Math.Abs(mapWithout.Points[i].X - mapWith.Points[i].X) > 1 ||
                Math.Abs(mapWithout.Points[i].Y - mapWith.Points[i].Y) > 1)
            {
                differentPoints++;
            }
        }

        // At least 50% of points should be different
        Assert.True(differentPoints > mapWithout.Points.Count * 0.5,
            $"Lloyd relaxation should move most points ({differentPoints}/{mapWithout.Points.Count})");
    }

    [Fact]
    public void ApplyLloydRelaxation_Performance()
    {
        var random = new PcgRandomSource(44444);
        var points = new List<Point>();
        
        for (int i = 0; i < 1000; i++)
        {
            points.Add(new Point(
                random.NextDouble() * 800,
                random.NextDouble() * 600
            ));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 3);

        stopwatch.Stop();

        // Should complete in reasonable time (< 6s for 3 iterations)
        Assert.True(stopwatch.ElapsedMilliseconds < 6000,
            $"Lloyd relaxation took {stopwatch.ElapsedMilliseconds}ms, should be < 6s");

        Assert.Equal(points.Count, relaxed.Count);
    }

    [Fact]
    public void MapGeneration_LloydRelaxation_FullPerformance()
    {
        var settings = new MapGenerationSettings
        {
            Seed = 55555,
            Width = 400,
            Height = 400,
            NumPoints = 2000,
            ApplyLloydRelaxation = true,
            LloydIterations = 2
        };

        var generator = new MapGenerator();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var map = generator.Generate(settings);

        stopwatch.Stop();

        // Full generation should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 20000,
            $"Map generation with Lloyd relaxation took {stopwatch.ElapsedMilliseconds}ms, should be < 20s");

        Assert.NotNull(map);
        Assert.True(map.Cells.Count > 0);
    }

    [Fact]
    public void ApplyLloydRelaxation_WithJitteredGrid()
    {
        var random = new PcgRandomSource(66666);
        var spacing = Math.Sqrt((double)800 * 600 / 400);
        var points = GeometryUtils.GenerateJitteredGridPoints(800, 600, spacing, random);

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 2);

        Assert.Equal(points.Count, relaxed.Count);
        Assert.All(relaxed, p =>
        {
            Assert.True(p.X >= 0 && p.X <= 800);
            Assert.True(p.Y >= 0 && p.Y <= 600);
        });
    }

    [Fact]
    public void ApplyLloydRelaxation_WithPoissonDisk()
    {
        var random = new PcgRandomSource(77777);
        var minDistance = Math.Sqrt((double)800 * 600 / 400);
        var points = GeometryUtils.GeneratePoissonDiskPoints(800, 600, minDistance, random);

        var relaxed = GeometryUtils.ApplyLloydRelaxation(points, 800, 600, 1);

        Assert.Equal(points.Count, relaxed.Count);
        Assert.All(relaxed, p =>
        {
            Assert.True(p.X >= 0 && p.X <= 800);
            Assert.True(p.Y >= 0 && p.Y <= 600);
        });
    }
}
