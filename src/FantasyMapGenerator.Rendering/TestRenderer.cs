using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Rendering;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Simple test class to verify rendering functionality
/// </summary>
public static class TestRenderer
{
    /// <summary>
    /// Creates a simple test map and renders it
    /// </summary>
    public static async Task TestBasicRendering()
    {
        Console.WriteLine("Creating test map...");
        
        // Create simple test map data
        var mapData = CreateTestMap();
        
        Console.WriteLine($"Map created with {mapData.Cells.Count} cells and {mapData.Burgs.Count} burgs");
        
        // Test rendering
        using var renderer = new MapRenderer();
        using var surface = renderer.RenderMap(mapData, 800, 600);
        
        Console.WriteLine("Map rendered successfully");
        
        // Test export
        using var exporter = new MapExporter(renderer);
        await exporter.ExportToPngAsync(mapData, "test-map.png", 800, 600);
        await exporter.ExportToSvgAsync(mapData, "test-map.svg", 800, 600);
        
        Console.WriteLine("Map exported successfully");
        Console.WriteLine("Files saved: test-map.png, test-map.svg");
    }
    
    /// <summary>
    /// Creates a simple test map with basic data
    /// </summary>
    private static MapData CreateTestMap()
    {
        var mapData = new MapData(800, 600, 100);
        
        // Create some test points
        var random = new Random(42); // Fixed seed for reproducible results
        
        for (int i = 0; i < 100; i++)
        {
            var point = new Point(
                random.NextDouble() * 800,
                random.NextDouble() * 600
            );
            mapData.Points.Add(point);
        }
        
        // Create cells
        for (int i = 0; i < 100; i++)
        {
            var cell = new Cell(i, mapData.Points[i])
            {
                Height = (byte)random.Next(0, 100),
                Biome = random.Next(0, 5),
                State = random.Next(0, 3),
                Culture = random.Next(0, 3),
                Population = random.Next(100, 10000)
            };
            
            // Add some neighbors (simplified)
            for (int j = 0; j < 3; j++)
            {
                var neighbor = random.Next(0, 100);
                if (neighbor != i && !cell.Neighbors.Contains(neighbor))
                {
                    cell.Neighbors.Add(neighbor);
                }
            }
            
            mapData.Cells.Add(cell);
        }
        
        // Create some test burgs
        for (int i = 0; i < 10; i++)
        {
            var burg = new Burg(i, mapData.Points[i], i)
            {
                Name = $"City {i + 1}",
                Population = random.Next(500, 50000),
                Type = (BurgType)random.Next(0, 5),
                IsCapital = i == 0, // First city is capital
                IsPort = random.Next(0, 2) == 1
            };
            
            mapData.Burgs.Add(burg);
        }
        
        // Create some test biomes
        mapData.Biomes.Add(new Biome(0) { Name = "Ocean" });
        mapData.Biomes.Add(new Biome(1) { Name = "Plains" });
        mapData.Biomes.Add(new Biome(2) { Name = "Forest" });
        mapData.Biomes.Add(new Biome(3) { Name = "Hills" });
        mapData.Biomes.Add(new Biome(4) { Name = "Mountains" });
        
        return mapData;
    }
}