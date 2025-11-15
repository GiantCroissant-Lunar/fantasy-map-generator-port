namespace FantasyMapGenerator.Core.Random;

/// <summary>
/// Alea-compatible PRNG port (matches common JS alea generator behavior)
/// Deterministic across platforms given the same string seed.
/// </summary>
public class AleaRandomSource : IRandomSource
{
    private double _s0, _s1, _s2, _c;

    public AleaRandomSource(string seed)
    {
        // Mash function initialized per JS alea
        double Mash(string data)
        {
            double n = 0xEFC8249D;
            for (int i = 0; i < data.Length; i++)
            {
                n += data[i];
                double h = 0.02519603282416938 * n;
                n = h - (int)h;
                n *= 4294967296.0; // 2^32
            }
            return (int)n * 2.3283064365386963e-10; // 2^-32
        }

        var mashSeed = seed ?? string.Empty;
        var mash = new System.Func<string, double>(Mash);

        _s0 = mash(" ");
        _s1 = mash(" ");
        _s2 = mash(" ");
        _s0 -= mash(mashSeed);
        if (_s0 < 0) _s0 += 1.0;
        _s1 -= mash(mashSeed);
        if (_s1 < 0) _s1 += 1.0;
        _s2 -= mash(mashSeed);
        if (_s2 < 0) _s2 += 1.0;
        _c = 1.0;
    }

    private double NextUnit()
    {
        // JS alea: t = 2091639*s0 + c*2^-32; s0=s1; s1=s2; s2=t - floor(t)
        double t = 2091639.0 * _s0 + _c * 2.3283064365386963e-10;
        _s0 = _s1;
        _s1 = _s2;
        _s2 = t - (int)t;
        _c = (int)t;
        return _s2;
    }

    public int Next()
    {
        // 31-bit non-negative int
        return (int)(NextDouble() * int.MaxValue);
    }

    public int Next(int maxValue)
    {
        if (maxValue <= 0) return 0;
        return (int)(NextDouble() * maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue >= maxValue) return minValue;
        return minValue + (int)(NextDouble() * (maxValue - minValue));
    }

    public double NextDouble()
    {
        return NextUnit();
    }

    public void NextBytes(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(NextDouble() * 256.0);
        }
    }
}

