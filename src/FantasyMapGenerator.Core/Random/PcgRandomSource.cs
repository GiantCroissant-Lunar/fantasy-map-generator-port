namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// PCG (Permuted Congruential Generator) random number generator
/// Cross-platform deterministic, fast, and statistically robust
/// Based on PCG-XSH-RR variant
/// </summary>
public class PcgRandomSource : IRandomSource
{
    private ulong _state;
    private readonly ulong _increment;

    /// <summary>
    /// Initialize with a 64-bit seed
    /// </summary>
    public PcgRandomSource(ulong seed, ulong sequence = 0)
    {
        _state = 0;
        _increment = (sequence << 1) | 1; // Must be odd

        // Advance state
        Step();
        _state += seed;
        Step();
    }

    /// <summary>
    /// Initialize from signed long (convenience)
    /// </summary>
    public PcgRandomSource(long seed, ulong sequence = 0)
        : this(unchecked((ulong)seed), sequence)
    {
    }

    /// <summary>
    /// Generate next 32-bit random value
    /// </summary>
    private uint NextUInt32()
    {
        ulong oldState = _state;
        Step();

        // PCG-XSH-RR (XorShift high, Random Rotation)
        uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rot = (int)(oldState >> 59);

        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    private void Step()
    {
        // LCG: state = state * multiplier + increment
        _state = _state * 6364136223846793005UL + _increment;
    }

    public int Next()
    {
        // Return non-negative int
        return (int)(NextUInt32() >> 1);
    }

    public int Next(int maxValue)
    {
        if (maxValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be non-negative");
        }

        if (maxValue == 0)
        {
            return 0; // Edge case: return 0 when maxValue is 0
        }

        // Unbiased bounded random (avoids modulo bias)
        uint threshold = (uint)(-maxValue) % (uint)maxValue;

        while (true)
        {
            uint value = NextUInt32();

            if (value >= threshold)
            {
                return (int)(value % (uint)maxValue);
            }
        }
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be <= maxValue");
        }

        long range = (long)maxValue - minValue;

        if (range <= int.MaxValue)
        {
            return Next((int)range) + minValue;
        }

        // Large range - use double precision
        return (int)(NextDouble() * range) + minValue;
    }

    public double NextDouble()
    {
        // Generate 53-bit precision (double mantissa)
        uint high = NextUInt32() >> 5;  // 27 bits
        uint low = NextUInt32() >> 6;   // 26 bits

        ulong combined = ((ulong)high << 26) | low; // 53 bits

        return combined / (double)(1UL << 53);
    }

    public void NextBytes(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            if (i % 4 == 0)
            {
                uint value = NextUInt32();
                buffer[i] = (byte)value;
                if (i + 1 < buffer.Length) buffer[i + 1] = (byte)(value >> 8);
                if (i + 2 < buffer.Length) buffer[i + 2] = (byte)(value >> 16);
                if (i + 3 < buffer.Length) buffer[i + 3] = (byte)(value >> 24);
            }
        }
    }

    /// <summary>
    /// Create a child RNG with derived seed (for subsystems)
    /// </summary>
    public PcgRandomSource CreateChild(ulong offset)
    {
        // Generate derived seed using current state
        ulong childSeed = _state + offset;
        return new PcgRandomSource(childSeed, offset);
    }
}
