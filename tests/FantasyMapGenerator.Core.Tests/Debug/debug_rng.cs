using FantasyMapGenerator.Core.Random;

Console.WriteLine("Testing PCG RNG reproducibility...");

// Test basic reproducibility
var rng1 = new PcgRandomSource(42);
var rng2 = new PcgRandomSource(42);

Console.WriteLine($"First RNG - Next(): {rng1.Next()}");
Console.WriteLine($"Second RNG - Next(): {rng2.Next()}");

Console.WriteLine($"First RNG - Next(): {rng1.Next()}");
Console.WriteLine($"Second RNG - Next(): {rng2.Next()}");

// Test Next(int) with edge case
Console.WriteLine($"First RNG - Next(0): {rng1.Next(0)}");
Console.WriteLine($"Second RNG - Next(0): {rng2.Next(0)}");

// Test Next(int, int)
Console.WriteLine($"First RNG - Next(10, 20): {rng1.Next(10, 20)}");
Console.WriteLine($"Second RNG - Next(10, 20): {rng2.Next(10, 20)}");

// Test NextDouble
Console.WriteLine($"First RNG - NextDouble(): {rng1.NextDouble()}");
Console.WriteLine($"Second RNG - NextDouble(): {rng2.NextDouble()}");

Console.WriteLine("Testing complete!");