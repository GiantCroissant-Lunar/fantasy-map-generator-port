using FantasyMapGenerator.Rendering;

namespace FantasyMapGenerator.Test;

/// <summary>
/// Simple test program to verify rendering functionality
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Fantasy Map Generator - Rendering Test");
        Console.WriteLine("=====================================");
        
        try
        {
            await TestRenderer.TestBasicRendering();
            Console.WriteLine("\nTest completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nTest failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}