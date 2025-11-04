using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Tests;

[MemoryDiagnoser]
public class RngBenchmarks
{
    private SystemRandomSource _systemRng;
    private PcgRandomSource _pcgRng;

    [GlobalSetup]
    public void Setup()
    {
        _systemRng = new SystemRandomSource(42);
        _pcgRng = new PcgRandomSource(42);
    }

    [Benchmark]
    public int SystemRandom_Next()
    {
        int sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _systemRng.Next();
        }
        return sum;
    }

    [Benchmark]
    public int PcgRandom_Next()
    {
        int sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _pcgRng.Next();
        }
        return sum;
    }

    [Benchmark]
    public double SystemRandom_NextDouble()
    {
        double sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _systemRng.NextDouble();
        }
        return sum;
    }

    [Benchmark]
    public double PcgRandom_NextDouble()
    {
        double sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += _pcgRng.NextDouble();
        }
        return sum;
    }

    [Benchmark]
    public void SystemRandom_NextBytes()
    {
        var buffer = new byte[1024];
        for (int i = 0; i < 100; i++)
        {
            _systemRng.NextBytes(buffer);
        }
    }

    [Benchmark]
    public void PcgRandom_NextBytes()
    {
        var buffer = new byte[1024];
        for (int i = 0; i < 100; i++)
        {
            _pcgRng.NextBytes(buffer);
        }
    }
}