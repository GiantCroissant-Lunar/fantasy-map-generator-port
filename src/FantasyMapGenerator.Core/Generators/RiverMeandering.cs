using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates meandered path points for rivers to enable smooth curve rendering.
/// Based on Azgaar's Fantasy Map Generator river meandering algorithm.
/// </summary>
public class RiverMeandering
{
    private readonly MapData _map;

    public RiverMeandering(MapData map)
    {
        _map = map;
    }

    /// <summary>
    /// Generates meandered path points for a river.
    /// Creates natural-looking curves by interpolating between cell centers
    /// and applying sinusoidal offsets that decrease with distance from source.
    /// </summary>
    /// <param name="river">River to process</param>
    /// <param name="baseMeandering">Base meandering factor (0.0 = straight, 1.0 = very curvy, default 0.5)</param>
    /// <returns>List of points forming the meandered path</returns>
    public List<Point> GenerateMeanderedPath(River river, double baseMeandering = 0.5)
    {
        var meanderedPath = new List<Point>();

        if (river.Cells == null || river.Cells.Count < 2)
        {
            // River too short to meander
            return meanderedPath;
        }

        // Process each segment of the river
        for (int i = 0; i < river.Cells.Count; i++)
        {
            var cellId = river.Cells[i];
            var cell = _map.Cells[cellId];
            var currentPoint = cell.Center;

            // Add the current cell center
            meanderedPath.Add(currentPoint);

            // If not the last cell, add interpolated points to next cell
            if (i < river.Cells.Count - 1)
            {
                var nextCellId = river.Cells[i + 1];
                var nextCell = _map.Cells[nextCellId];
                var nextPoint = nextCell.Center;

                // Calculate meandering factor for this segment
                // Decreases with distance from source (natural behavior)
                double meanderingFactor = CalculateMeanderingFactor(i, baseMeandering, cell.Height);

                // Generate interpolated points with meandering
                var interpolated = InterpolatePoints(currentPoint, nextPoint, meanderingFactor);
                meanderedPath.AddRange(interpolated);
            }
        }

        return meanderedPath;
    }

    /// <summary>
    /// Calculates the meandering factor for a river segment.
    /// Meandering decreases with distance from source and in steep terrain.
    /// </summary>
    /// <param name="step">Current step from source (0 = source)</param>
    /// <param name="baseMeandering">Base meandering factor</param>
    /// <param name="height">Terrain height (higher = steeper, less meandering)</param>
    /// <returns>Adjusted meandering factor</returns>
    private double CalculateMeanderingFactor(int step, double baseMeandering, int height)
    {
        // Meandering decreases with distance from source
        // Use exponential decay: starts high, decreases gradually
        double distanceDecay = Math.Exp(-step * 0.05);

        // Meandering reduces in steep terrain (mountains)
        // Height typically ranges from 0 (sea level) to 100 (peaks)
        double terrainFactor = 1.0 - (height / 200.0); // Reduce meandering at high elevations
        terrainFactor = Math.Clamp(terrainFactor, 0.3, 1.0); // Keep some meandering even in mountains

        return baseMeandering * distanceDecay * terrainFactor;
    }

    /// <summary>
    /// Interpolates points between start and end with sinusoidal meandering offset.
    /// Creates natural-looking curves perpendicular to the river direction.
    /// </summary>
    /// <param name="start">Start point</param>
    /// <param name="end">End point</param>
    /// <param name="meander">Meandering intensity (0.0 = straight line)</param>
    /// <returns>List of interpolated points (not including start/end)</returns>
    private List<Point> InterpolatePoints(Point start, Point end, double meander)
    {
        var points = new List<Point>();

        if (meander < 0.01)
        {
            // No meandering, return empty (straight line between cells)
            return points;
        }

        // Calculate segment length and direction
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1.0)
        {
            // Segment too short to interpolate
            return points;
        }

        // Normalize direction vector
        double dirX = dx / length;
        double dirY = dy / length;

        // Perpendicular vector (for meandering offset)
        double perpX = -dirY;
        double perpY = dirX;

        // Number of interpolation points (more points = smoother curves)
        // Typically 3-5 points per segment
        int numPoints = Math.Max(2, (int)(length / 10.0) + 2);

        // Generate interpolated points
        for (int i = 1; i < numPoints; i++)
        {
            double t = (double)i / numPoints; // Parameter from 0 to 1

            // Linear interpolation
            double x = start.X + dx * t;
            double y = start.Y + dy * t;

            // Sinusoidal offset perpendicular to river direction
            // Creates natural S-curves
            double offset = Math.Sin(t * Math.PI) * meander * length * 0.3;

            // Apply offset
            x += perpX * offset;
            y += perpY * offset;

            points.Add(new Point(x, y));
        }

        return points;
    }
}
