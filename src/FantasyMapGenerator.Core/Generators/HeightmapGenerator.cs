using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates heightmaps for fantasy maps using various algorithms and templates
/// Based on the original JavaScript heightmap-generator.js
/// </summary>
public class HeightmapGenerator
{
    private readonly MapData _map;
    private readonly double _blobPower;
    private readonly double _linePower;
    private byte[] _heights = null!;
    
    public HeightmapGenerator(MapData map)
    {
        _map = map;
        _blobPower = GetBlobPower(map.CellsDesired);
        _linePower = GetLinePower(map.CellsDesired);
        _heights = new byte[map.Cells.Count];
    }
    
    /// <summary>
    /// Generates a heightmap from a template
    /// </summary>
    public byte[] FromTemplate(string templateId, IRandomSource random)
    {
        var template = HeightmapTemplates.GetTemplate(templateId);
        if (template == null)
        {
            throw new ArgumentException($"Unknown heightmap template: {templateId}");
        }

        var steps = template.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var step in steps)
        {
            var elements = step.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length < 2)
            {
                throw new ArgumentException($"Invalid heightmap step: {step}");
            }

            ApplyStep(elements, random);
        }

        return _heights;
    }
    
    /// <summary>
    /// Generates a random heightmap using noise functions
    /// </summary>
    public byte[] FromNoise()
    {
        return FromNoise(new SystemRandomSource(42));
    }

    /// <summary>
    /// Generates a random heightmap using noise functions with seeded RNG
    /// </summary>
    public byte[] FromNoise(IRandomSource random)
    {
        // Initialize with random noise
        for (int i = 0; i < _heights.Length; i++)
        {
            _heights[i] = (byte)random.Next(20, 80);
        }
        
        // Apply smoothing
        SmoothHeights(3);
        
        // Add some random peaks
        AddRandomPeaks(5, 90, 100, random);
        
        // Add some random valleys
        AddRandomValleys(3, 0, 20, random);
        
        // Final smoothing
        SmoothHeights(2);
        
        return _heights;
    }
    
    /// <summary>
    /// Applies a single step from a heightmap template
    /// </summary>
    private void ApplyStep(string[] elements, IRandomSource random)
    {
        var operation = elements[0];

        switch (operation)
        {
            case "blob":
                if (elements.Length >= 4)
                {
                    double x = double.Parse(elements[1]);
                    double y = double.Parse(elements[2]);
                    double radius = double.Parse(elements[3]);
                    double blobHeight = elements.Length > 4 ? double.Parse(elements[4]) : 100;
                    AddBlob(x, y, radius, blobHeight);
                }
                break;

            case "line":
                if (elements.Length >= 6)
                {
                    double x1 = double.Parse(elements[1]);
                    double y1 = double.Parse(elements[2]);
                    double x2 = double.Parse(elements[3]);
                    double y2 = double.Parse(elements[4]);
                    double lineHeight = elements.Length > 5 ? double.Parse(elements[5]) : 100;
                    double width = elements.Length > 6 ? double.Parse(elements[6]) : 10;
                    AddLine(x1, y1, x2, y2, lineHeight, width);
                }
                break;

            case "smooth":
                int iterations = elements.Length > 1 ? int.Parse(elements[1]) : 1;
                SmoothHeights(iterations);
                break;

            case "fill":
                byte height = elements.Length > 1 ? byte.Parse(elements[1]) : (byte)50;
                Fill(height);
                break;

            case "noise":
                double amplitude = elements.Length > 1 ? double.Parse(elements[1]) : 10;
                AddNoise(amplitude, random);
                break;

            default:
                throw new ArgumentException($"Unknown heightmap operation: {operation}");
        }
    }
    
    /// <summary>
    /// Adds a blob (circular hill or depression) to the heightmap
    /// </summary>
    private void AddBlob(double x, double y, double radius, double blobHeight)
    {
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            double distance = GeometryUtils.Distance(cell.Center, new Point(x, y));
            
            if (distance <= radius)
            {
                double factor = 1.0 - Math.Pow(distance / radius, _blobPower);
                double newHeight = _heights[i] + blobHeight * factor;
                _heights[i] = (byte)Math.Clamp(newHeight, 0, 100);
            }
        }
    }
    
    /// <summary>
    /// Adds a line (mountain range or ridge) to the heightmap
    /// </summary>
    private void AddLine(double x1, double y1, double x2, double y2, double lineHeight, double width)
    {
        var start = new Point(x1, y1);
        var end = new Point(x2, y2);
        double length = GeometryUtils.Distance(start, end);
        
        for (int i = 0; i < _map.Cells.Count; i++)
        {
            var cell = _map.Cells[i];
            
            // Calculate distance from point to line segment
            double distance = DistanceToLineSegment(cell.Center, start, end);
            
            if (distance <= width)
            {
                // Check if point is within line segment bounds
                double t = ProjectOntoLine(cell.Center, start, end);
                if (t >= 0 && t <= 1)
                {
                    double factor = 1.0 - Math.Pow(distance / width, _linePower);
                    double newHeight = _heights[i] + lineHeight * factor;
                    _heights[i] = (byte)Math.Clamp(newHeight, 0, 100);
                }
            }
        }
    }
    
    /// <summary>
    /// Smooths the heightmap using averaging
    /// </summary>
    private void SmoothHeights(int iterations)
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            var newHeights = new byte[_heights.Length];
            
            for (int i = 0; i < _map.Cells.Count; i++)
            {
                var cell = _map.Cells[i];
                var neighbors = new List<byte> { _heights[i] };
                
                foreach (int neighborId in cell.Neighbors)
                {
                    if (neighborId >= 0 && neighborId < _heights.Length)
                    {
                        neighbors.Add(_heights[neighborId]);
                    }
                }
                
                newHeights[i] = (byte)neighbors.Select(n => (double)n).Average();
            }
            
            _heights = newHeights;
        }
    }
    
    /// <summary>
    /// Fills the entire heightmap with a constant value
    /// </summary>
    private void Fill(byte height)
    {
        Array.Fill(_heights, height);
    }
    
    /// <summary>
    /// Adds random noise to the heightmap
    /// </summary>
    private void AddNoise(double amplitude, IRandomSource random)
    {
        for (int i = 0; i < _heights.Length; i++)
        {
            double noise = (random.NextDouble() - 0.5) * 2 * amplitude;
            double newHeight = _heights[i] + noise;
            _heights[i] = (byte)Math.Clamp(newHeight, 0, 100);
        }
    }

    /// <summary>
    /// Adds random peaks to the heightmap with seeded RNG
    /// </summary>
    private void AddRandomPeaks(int count, byte minHeight, byte maxHeight, IRandomSource random)
    {
        for (int i = 0; i < count; i++)
        {
            int cellIndex = random.Next(0, _map.Cells.Count);
            var cell = _map.Cells[cellIndex];
            byte height = (byte)random.Next(minHeight, maxHeight + 1);
            
            AddBlob(cell.Center.X, cell.Center.Y, 20, height);
        }
    }
    
    /// <summary>
    /// Adds random valleys to the heightmap with seeded RNG
    /// </summary>
    private void AddRandomValleys(int count, byte minHeight, byte maxHeight, IRandomSource random)
    {
        for (int i = 0; i < count; i++)
        {
            int cellIndex = random.Next(0, _map.Cells.Count);
            var cell = _map.Cells[cellIndex];
            byte height = (byte)random.Next(minHeight, maxHeight + 1);
            
            AddBlob(cell.Center.X, cell.Center.Y, 15, height);
        }
    }
    
    /// <summary>
    /// Calculates distance from a point to a line segment
    /// </summary>
    private static double DistanceToLineSegment(Point point, Point lineStart, Point lineEnd)
    {
        double A = point.X - lineStart.X;
        double B = point.Y - lineStart.Y;
        double C = lineEnd.X - lineStart.X;
        double D = lineEnd.Y - lineStart.Y;
        
        double dot = A * C + B * D;
        double lenSq = C * C + D * D;
        double param = lenSq != 0 ? dot / lenSq : -1;
        
        double xx, yy;
        
        if (param < 0)
        {
            xx = lineStart.X;
            yy = lineStart.Y;
        }
        else if (param > 1)
        {
            xx = lineEnd.X;
            yy = lineEnd.Y;
        }
        else
        {
            xx = lineStart.X + param * C;
            yy = lineStart.Y + param * D;
        }
        
        double dx = point.X - xx;
        double dy = point.Y - yy;
        
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// Projects a point onto a line segment
    /// </summary>
    private static double ProjectOntoLine(Point point, Point lineStart, Point lineEnd)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        
        if (dx == 0 && dy == 0)
            return 0;
        
        double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
        return Math.Clamp(t, 0, 1);
    }
    
    /// <summary>
    /// Gets the blob power based on the number of cells
    /// </summary>
    private static double GetBlobPower(int cellsDesired)
    {
        return Math.Max(1.5, 3.0 - Math.Sqrt(cellsDesired / 1000.0));
    }
    
    /// <summary>
    /// Gets the line power based on the number of cells
    /// </summary>
    private static double GetLinePower(int cellsDesired)
    {
        return Math.Max(1.5, 2.5 - Math.Sqrt(cellsDesired / 1000.0));
    }
}

/// <summary>
/// Predefined heightmap templates
/// </summary>
public static class HeightmapTemplates
{
    public static string? GetTemplate(string id)
    {
        return id.ToLower() switch
        {
            "archipelago" => GetArchipelagoTemplate(),
            "continents" => GetContinentsTemplate(),
            "island" => GetIslandTemplate(),
            "pangea" => GetPangeaTemplate(),
            "mediterranean" => GetMediterraneanTemplate(),
            _ => null
        };
    }
    
    private static string GetArchipelagoTemplate()
    {
        return @"
            fill 20
            blob 200 200 100 60
            blob 600 200 80 50
            blob 400 400 70 40
            blob 200 600 90 55
            blob 600 600 85 45
            blob 800 400 75 35
            smooth 2
            noise 5
            smooth 1
        ";
    }
    
    private static string GetContinentsTemplate()
    {
        return @"
            fill 30
            blob 200 300 150 70
            blob 600 300 120 60
            blob 400 500 100 50
            line 100 200 300 400 40 20
            line 500 200 700 400 35 15
            smooth 3
            noise 8
            smooth 2
        ";
    }
    
    private static string GetIslandTemplate()
    {
        return @"
            fill 15
            blob 400 300 120 65
            blob 300 400 80 45
            blob 500 400 70 40
            smooth 2
            noise 3
            smooth 1
        ";
    }
    
    private static string GetPangeaTemplate()
    {
        return @"
            fill 25
            blob 300 300 200 60
            blob 500 300 180 55
            blob 400 500 150 50
            blob 200 400 120 45
            blob 600 400 130 48
            smooth 4
            noise 10
            smooth 2
        ";
    }
    
    private static string GetMediterraneanTemplate()
    {
        return @"
            fill 35
            blob 200 200 100 55
            blob 600 200 90 50
            blob 400 400 80 45
            blob 300 500 70 40
            blob 500 500 75 42
            line 400 200 400 600 -20 30
            smooth 3
            noise 6
            smooth 2
        ";
    }
}