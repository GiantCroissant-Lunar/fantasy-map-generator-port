namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// Wrapper around System.Random for backwards compatibility
/// WARNING: Not cross-platform deterministic
/// </summary>
public class SystemRandomSource : IRandomSource
{
    private readonly global::System.Random _random;

    public SystemRandomSource(int seed)
    {
        _random = new global::System.Random(seed);
    }

    public int Next() => _random.Next();

    public int Next(int maxValue) => _random.Next(maxValue);

    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);

    public double NextDouble() => _random.NextDouble();

    public void NextBytes(byte[] buffer) => _random.NextBytes(buffer);
}
