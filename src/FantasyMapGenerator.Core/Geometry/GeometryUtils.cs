using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// Utility methods for geometric calculations
/// </summary>
public static class GeometryUtils
{
    /// <summary>
    /// Calculates the distance between two points
    /// </summary>
    public static double Distance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    /// <summary>
    /// Calculates the squared distance between two points (faster than Distance)
    /// </summary>
    public static double DistanceSquared(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Calculates the angle between two points in radians
    /// </summary>
    public static double Angle(Point p1, Point p2)
    {
        return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
    }

    /// <summary>
    /// Converts radians to degrees
    /// </summary>
    public static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    /// <summary>
    /// Converts degrees to radians
    /// </summary>
    public static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Normalizes an angle to [0, 2π) range
    /// </summary>
    public static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 2 * Math.PI;
        while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
        return angle;
    }

    /// <summary>
    /// Calculates the area of a triangle using the cross product
    /// </summary>
    public static double TriangleArea(Point a, Point b, Point c)
    {
        return Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) / 2.0;
    }

    /// <summary>
    /// Calculates the area of a polygon using the shoelace formula
    /// </summary>
    public static double PolygonArea(IReadOnlyList<Point> points)
    {
        if (points.Count < 3) return 0;

        double area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        return Math.Abs(area) / 2.0;
    }

    /// <summary>
    /// Calculates the centroid of a polygon
    /// </summary>
    public static Point PolygonCentroid(IReadOnlyList<Point> points)
    {
        if (points.Count == 0) return Point.Zero;
        if (points.Count == 1) return points[0];

        double cx = 0, cy = 0;
        double area = 0;

        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            double a = points[i].X * points[j].Y - points[j].X * points[i].Y;
            area += a;
            cx += (points[i].X + points[j].X) * a;
            cy += (points[i].Y + points[j].Y) * a;
        }

        area *= 0.5;
        if (Math.Abs(area) < double.Epsilon) return points[0];

        cx /= (6.0 * area);
        cy /= (6.0 * area);

        return new Point(Math.Abs(cx), Math.Abs(cy));
    }

    /// <summary>
    /// Checks if a point is inside a polygon using ray casting
    /// </summary>
    public static bool PointInPolygon(Point point, IReadOnlyList<Point> polygon)
    {
        if (polygon.Count < 3) return false;

        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; i++)
        {
            if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) /
                (polygon[j].Y - polygon[i].Y) + polygon[i].X))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    /// <summary>
    /// Finds the closest point in a collection to a target point
    /// </summary>
    public static Point FindClosest(Point target, IEnumerable<Point> points)
    {
        Point closest = Point.Zero;
        double minDistance = double.MaxValue;

        foreach (var point in points)
        {
            double distance = DistanceSquared(target, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = point;
            }
        }

        return closest;
    }

    /// <summary>
    /// Finds the index of the closest point in a collection to a target point
    /// </summary>
    public static int FindClosestIndex(Point target, IReadOnlyList<Point> points)
    {
        if (points.Count == 0) return -1;

        int closestIndex = 0;
        double minDistance = DistanceSquared(target, points[0]);

        for (int i = 1; i < points.Count; i++)
        {
            double distance = DistanceSquared(target, points[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// Generates points in a Poisson disk distribution
    /// </summary>
    public static List<Point> GeneratePoissonDiskPoints(double width, double height, double minDistance, int maxAttempts = 30)
    {
        return GeneratePoissonDiskPoints(width, height, minDistance, maxAttempts, null);
    }

    /// <summary>
    /// Generates points in a Poisson disk distribution with seeded RNG
    /// </summary>
    public static List<Point> GeneratePoissonDiskPoints(double width, double height, double minDistance, IRandomSource random)
    {
        return GeneratePoissonDiskPoints(width, height, minDistance, 30, random);
    }

    /// <summary>
    /// Internal implementation that can use either default RNG or provided RNG
    /// </summary>
    private static List<Point> GeneratePoissonDiskPoints(double width, double height, double minDistance, int maxAttempts, IRandomSource? random)
    {
        var points = new List<Point>();
        var grid = new List<Point?>();
        var cellSize = minDistance / Math.Sqrt(2);
        var gridWidth = (int)Math.Ceiling(width / cellSize);
        var gridHeight = (int)Math.Ceiling(height / cellSize);

        // Initialize grid
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            grid.Add(null);
        }

        var activePoints = new Queue<Point>();

        // Add first random point
        var firstPoint = random != null
            ? new Point(random.NextDouble() * width, random.NextDouble() * height)
            : new Point(global::System.Random.Shared.NextDouble() * width, global::System.Random.Shared.NextDouble() * height);
        points.Add(firstPoint);
        activePoints.Enqueue(firstPoint);

        int gridX = (int)(firstPoint.X / cellSize);
        int gridY = (int)(firstPoint.Y / cellSize);
        grid[gridY * gridWidth + gridX] = firstPoint;

        while (activePoints.Count > 0)
        {
            var currentPoint = activePoints.Dequeue();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                double angle = random != null
                    ? random.NextDouble() * 2 * Math.PI
                    : global::System.Random.Shared.NextDouble() * 2 * Math.PI;
                double distance = minDistance + (random != null
                    ? random.NextDouble() * minDistance
                    : global::System.Random.Shared.NextDouble() * minDistance);

                var newPoint = new Point(
                    currentPoint.X + Math.Cos(angle) * distance,
                    currentPoint.Y + Math.Sin(angle) * distance
                );

                if (newPoint.X < 0 || newPoint.X >= width || newPoint.Y < 0 || newPoint.Y >= height)
                    continue;

                int newGridX = (int)(newPoint.X / cellSize);
                int newGridY = (int)(newPoint.Y / cellSize);

                if (grid[newGridY * gridWidth + newGridX] != null)
                    continue;

                // Check neighbors
                bool tooClose = false;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int checkX = newGridX + dx;
                        int checkY = newGridY + dy;

                        if (checkX < 0 || checkX >= gridWidth || checkY < 0 || checkY >= gridHeight)
                            continue;

                        var neighbor = grid[checkY * gridWidth + checkX];
                        if (neighbor != null && Distance(newPoint, neighbor.Value) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) break;
                }

                if (!tooClose)
                {
                    points.Add(newPoint);
                    activePoints.Enqueue(newPoint);
                    grid[newGridY * gridWidth + newGridX] = newPoint;
                }
            }
        }

        return points;
    }

    /// <summary>
    /// Generates approximately <paramref name="target"/> points on a jittered uniform grid.
    /// Provides a dense, stable distribution when Poisson-disk fails or as a fallback.
    /// </summary>
    public static List<Point> GenerateUniformGridPoints(double width, double height, int target, IRandomSource? random)
    {
        target = Math.Max(1, target);
        double aspect = width / Math.Max(1.0, height);
        int nx = Math.Max(1, (int)Math.Round(Math.Sqrt(target * aspect)));
        int ny = Math.Max(1, (int)Math.Ceiling((double)target / nx));

        double dx = width / nx;
        double dy = height / ny;
        double jx = 0.25 * dx; // jitter up to 25%
        double jy = 0.25 * dy;

        var pts = new List<Point>(nx * ny);
        for (int iy = 0; iy < ny; iy++)
        {
            for (int ix = 0; ix < nx; ix++)
            {
                double cx = (ix + 0.5) * dx;
                double cy = (iy + 0.5) * dy;
                double ox = random != null ? (random.NextDouble() * 2 - 1) * jx : (global::System.Random.Shared.NextDouble() * 2 - 1) * jx;
                double oy = random != null ? (random.NextDouble() * 2 - 1) * jy : (global::System.Random.Shared.NextDouble() * 2 - 1) * jy;
                double x = Math.Clamp(cx + ox, 0, Math.Max(0.0, width - 1e-6));
                double y = Math.Clamp(cy + oy, 0, Math.Max(0.0, height - 1e-6));
                pts.Add(new Point(x, y));
            }
        }
        return pts;
    }

    /// <summary>
    /// Generate jittered square grid points using a given spacing.
    /// Mirrors FMG's getJitteredGrid behavior (random offset within ~radius).
    /// </summary>
    public static List<Point> GenerateJitteredGridPoints(double width, double height, double spacing, IRandomSource random)
    {
        double radius = spacing / 2.0;
        double jittering = radius * 0.9;
        double doubleJittering = jittering * 2.0;

        var points = new List<Point>();
        for (double y = radius; y < height; y += spacing)
        {
            for (double x = radius; x < width; x += spacing)
            {
                double xj = Math.Min(Math.Round(x + (random.NextDouble() * doubleJittering - jittering), 2), width);
                double yj = Math.Min(Math.Round(y + (random.NextDouble() * doubleJittering - jittering), 2), height);
                points.Add(new Point(xj, yj));
            }
        }
        return points;
    }
}
