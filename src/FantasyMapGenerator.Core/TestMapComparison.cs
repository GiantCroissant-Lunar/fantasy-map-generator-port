using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core;

/// <summary>
/// Simple comparison to verify FastNoiseLite is being used
/// Run from tests with: dotnet test --filter TestMapComparison
/// </summary>
public class TestMapComparison
{
    public static void Main()
    {
        Console.WriteLine("=== Testing Map Generation ===\n");

        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 1000,
            SeaLevel = 0.3f
        };

        var generator = new MapGenerator();

        // Test 1: OLD system
        Console.WriteLine("\n--- Test 1: OLD System (UseAdvancedNoise = false) ---");
        settings.UseAdvancedNoise = false;
        settings.HeightmapTemplate = null;
        var map1 = generator.Generate(settings);

        // Test 2: NEW system
        Console.WriteLine("\n--- Test 2: NEW System (UseAdvancedNoise = true, island) ---");
        settings.UseAdvancedNoise = true;
        settings.HeightmapTemplate = "island";
        var map2 = generator.Generate(settings);

        Console.WriteLine("\n=== Done ===");
    }
}
