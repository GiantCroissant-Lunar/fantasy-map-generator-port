using SkiaSharp;

namespace FantasyMapGenerator.Rendering;

/// <summary>
/// Provides spline and curve smoothing utilities for rendering
/// </summary>
public static class CurveSmoothing
{
    /// <summary>
    /// Creates a smooth Catmull-Rom spline through a set of points
    /// </summary>
    public static SKPath CreateCatmullRomSpline(List<SKPoint> points, bool closed = false, float tension = 0.5f)
    {
        var path = new SKPath();

        if (points.Count < 2)
            return path;

        if (points.Count == 2)
        {
            path.MoveTo(points[0]);
            path.LineTo(points[1]);
            return path;
        }

        path.MoveTo(points[0]);

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : (closed ? points[^1] : points[i]);
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i + 2 < points.Count ? points[i + 2] : (closed ? points[0] : points[i + 1]);

            AddCatmullRomSegment(path, p0, p1, p2, p3, tension);
        }

        if (closed)
        {
            // Connect last point back to first
            var p0 = points[^2];
            var p1 = points[^1];
            var p2 = points[0];
            var p3 = points[1];
            AddCatmullRomSegment(path, p0, p1, p2, p3, tension);
            path.Close();
        }

        return path;
    }

    /// <summary>
    /// Adds a single Catmull-Rom spline segment using cubic Bezier approximation
    /// </summary>
    private static void AddCatmullRomSegment(SKPath path, SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3, float tension)
    {
        // Convert Catmull-Rom to Bezier control points
        // Using the matrix form of Catmull-Rom spline
        float t = tension;

        var c1 = new SKPoint(
            p1.X + (p2.X - p0.X) / 6f * t,
            p1.Y + (p2.Y - p0.Y) / 6f * t
        );

        var c2 = new SKPoint(
            p2.X - (p3.X - p1.X) / 6f * t,
            p2.Y - (p3.Y - p1.Y) / 6f * t
        );

        path.CubicTo(c1, c2, p2);
    }

    /// <summary>
    /// Creates a smooth B-spline through points
    /// </summary>
    public static SKPath CreateBSpline(List<SKPoint> points, int degree = 3)
    {
        var path = new SKPath();

        if (points.Count < degree + 1)
        {
            // Fall back to simple polyline
            if (points.Count > 0)
            {
                path.MoveTo(points[0]);
                foreach (var point in points.Skip(1))
                    path.LineTo(point);
            }
            return path;
        }

        // Simplified cubic B-spline
        path.MoveTo(points[0]);

        for (int i = 0; i < points.Count - 3; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];
            var p2 = points[i + 2];
            var p3 = points[i + 3];

            // B-spline basis functions approximated with Bezier
            var c1 = new SKPoint(
                (2 * p1.X + p2.X) / 3f,
                (2 * p1.Y + p2.Y) / 3f
            );

            var c2 = new SKPoint(
                (p1.X + 2 * p2.X) / 3f,
                (p1.Y + 2 * p2.Y) / 3f
            );

            var end = new SKPoint(
                (p1.X + 4 * p2.X + p3.X) / 6f,
                (p1.Y + 4 * p2.Y + p3.Y) / 6f
            );

            path.CubicTo(c1, c2, end);
        }

        // Connect to last point
        if (points.Count > 0)
            path.LineTo(points[^1]);

        return path;
    }

    /// <summary>
    /// Smooths a path by averaging nearby points (Gaussian-like smoothing)
    /// </summary>
    public static List<SKPoint> SmoothPoints(List<SKPoint> points, int windowSize = 3, int iterations = 1)
    {
        if (points.Count < windowSize || windowSize < 2)
            return new List<SKPoint>(points);

        var smoothed = new List<SKPoint>(points);

        for (int iter = 0; iter < iterations; iter++)
        {
            var temp = new List<SKPoint>();
            int halfWindow = windowSize / 2;

            for (int i = 0; i < smoothed.Count; i++)
            {
                float sumX = 0, sumY = 0;
                int count = 0;

                for (int j = Math.Max(0, i - halfWindow); j <= Math.Min(smoothed.Count - 1, i + halfWindow); j++)
                {
                    sumX += smoothed[j].X;
                    sumY += smoothed[j].Y;
                    count++;
                }

                temp.Add(new SKPoint(sumX / count, sumY / count));
            }

            smoothed = temp;
        }

        return smoothed;
    }

    /// <summary>
    /// Simplifies a path by removing points that don't significantly affect the shape
    /// (Ramer-Douglas-Peucker algorithm)
    /// </summary>
    public static List<SKPoint> SimplifyPath(List<SKPoint> points, float epsilon = 1.0f)
    {
        if (points.Count < 3)
            return new List<SKPoint>(points);

        return DouglasPeucker(points, 0, points.Count - 1, epsilon);
    }

    /// <summary>
    /// Ramer-Douglas-Peucker algorithm implementation
    /// </summary>
    private static List<SKPoint> DouglasPeucker(List<SKPoint> points, int start, int end, float epsilon)
    {
        // Find the point with maximum distance from line segment
        float maxDist = 0;
        int maxIndex = 0;

        for (int i = start + 1; i < end; i++)
        {
            float dist = PerpendicularDistance(points[i], points[start], points[end]);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxIndex = i;
            }
        }

        // If max distance is greater than epsilon, recursively simplify
        if (maxDist > epsilon)
        {
            var left = DouglasPeucker(points, start, maxIndex, epsilon);
            var right = DouglasPeucker(points, maxIndex, end, epsilon);

            // Combine results (removing duplicate at connection point)
            var result = new List<SKPoint>(left);
            result.AddRange(right.Skip(1));
            return result;
        }
        else
        {
            // Base case: just return endpoints
            return new List<SKPoint> { points[start], points[end] };
        }
    }

    /// <summary>
    /// Calculates perpendicular distance from point to line segment
    /// </summary>
    private static float PerpendicularDistance(SKPoint point, SKPoint lineStart, SKPoint lineEnd)
    {
        float dx = lineEnd.X - lineStart.X;
        float dy = lineEnd.Y - lineStart.Y;

        if (dx == 0 && dy == 0)
            return Distance(point, lineStart);

        float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));

        var projection = new SKPoint(
            lineStart.X + t * dx,
            lineStart.Y + t * dy
        );

        return Distance(point, projection);
    }

    /// <summary>
    /// Calculates Euclidean distance between two points
    /// </summary>
    private static float Distance(SKPoint p1, SKPoint p2)
    {
        float dx = p2.X - p1.X;
        float dy = p2.Y - p1.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Subdivides a path by adding interpolated points between existing ones
    /// </summary>
    public static List<SKPoint> SubdividePath(List<SKPoint> points, int subdivisions = 1)
    {
        if (subdivisions <= 0 || points.Count < 2)
            return new List<SKPoint>(points);

        var result = new List<SKPoint>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            result.Add(points[i]);

            // Add interpolated points
            for (int j = 1; j <= subdivisions; j++)
            {
                float t = j / (float)(subdivisions + 1);
                result.Add(new SKPoint(
                    points[i].X + (points[i + 1].X - points[i].X) * t,
                    points[i].Y + (points[i + 1].Y - points[i].Y) * t
                ));
            }
        }

        result.Add(points[^1]);
        return result;
    }

    /// <summary>
    /// Creates a smooth contour by combining subdivision, smoothing, and spline interpolation
    /// </summary>
    public static SKPath CreateSmoothContour(List<SKPoint> boundaryPoints, bool closed = true)
    {
        if (boundaryPoints.Count < 3)
        {
            var simplePath = new SKPath();
            if (boundaryPoints.Count > 0)
            {
                simplePath.MoveTo(boundaryPoints[0]);
                foreach (var pt in boundaryPoints.Skip(1))
                    simplePath.LineTo(pt);
                if (closed) simplePath.Close();
            }
            return simplePath;
        }

        // 1. Simplify to reduce noise
        var simplified = SimplifyPath(boundaryPoints, epsilon: 2.0f);

        // 2. Smooth the points
        var smoothed = SmoothPoints(simplified, windowSize: 3, iterations: 2);

        // 3. Create smooth spline
        var path = CreateCatmullRomSpline(smoothed, closed, tension: 0.6f);

        return path;
    }
}
