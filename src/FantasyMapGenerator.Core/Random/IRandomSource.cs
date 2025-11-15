namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// Abstraction for random number generation
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a non-negative random integer
    /// </summary>
    int Next();

    /// <summary>
    /// Returns a non-negative random integer less than maxValue
    /// </summary>
    int Next(int maxValue);

    /// <summary>
    /// Returns a random integer within a specified range [minValue, maxValue)
    /// </summary>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// Returns a random floating-point number in [0.0, 1.0)
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Fills the elements of a byte array with random numbers
    /// </summary>
    void NextBytes(byte[] buffer);

    /// <summary>
    /// Returns a random floating-point number in [0.0, 1.0) (alias for NextDouble)
    /// </summary>
    float NextFloat() => (float)NextDouble();
}
