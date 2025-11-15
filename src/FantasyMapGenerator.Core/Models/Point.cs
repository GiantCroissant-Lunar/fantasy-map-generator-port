namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a 2D point with floating-point coordinates
/// </summary>
public readonly record struct Point(double X, double Y)
{
    public static Point Zero => new(0, 0);

    public double DistanceTo(Point other) => Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));

    public Point Translate(double dx, double dy) => new(X + dx, Y + dy);

    public Point Scale(double factor) => new(X * factor, Y * factor);

    public static Point operator +(Point left, Point right) => new(left.X + right.X, left.Y + right.Y);

    public static Point operator -(Point left, Point right) => new(left.X - right.X, left.Y - right.Y);

    public static Point operator *(Point point, double scalar) => new(point.X * scalar, point.Y * scalar);

    public static implicit operator Point((double x, double y) tuple) => new(tuple.x, tuple.y);
}
