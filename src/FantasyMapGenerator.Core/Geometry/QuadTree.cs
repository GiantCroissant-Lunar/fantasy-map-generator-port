namespace FantasyMapGenerator.Core.Geometry;

using FantasyMapGenerator.Core.Models;

/// <summary>
/// Simple QuadTree for spatial indexing of burgs
/// </summary>
public class QuadTree
{
    private class Node
    {
        public Point Position { get; set; }
        public int BurgId { get; set; }
    }

    private readonly List<Node> _nodes = new();
    private readonly double _width;
    private readonly double _height;

    public QuadTree(double width, double height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Add a burg to the tree
    /// </summary>
    public void Add(Point position, int burgId)
    {
        _nodes.Add(new Node { Position = position, BurgId = burgId });
    }

    /// <summary>
    /// Find nearest burg within given radius
    /// </summary>
    public int? FindNearest(Point position, double maxDistance)
    {
        double minDistSq = maxDistance * maxDistance;
        int? nearest = null;

        foreach (var node in _nodes)
        {
            double dx = node.Position.X - position.X;
            double dy = node.Position.Y - position.Y;
            double distSq = dx * dx + dy * dy;

            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = node.BurgId;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Check if any burg exists within given radius
    /// </summary>
    public bool HasNearby(Point position, double maxDistance)
    {
        return FindNearest(position, maxDistance).HasValue;
    }
}
