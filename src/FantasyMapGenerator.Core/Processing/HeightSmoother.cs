using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Processing;

/// <summary>
/// Smooths height values across Voronoi cells using neighbor averaging
/// This is an optional pre-processing step that can improve terrain smoothness
/// </summary>
public class HeightSmoother
{
    /// <summary>
    /// Smooths cell heights by averaging with neighbors
    /// This mimics the approach used in the original JavaScript implementation
    /// </summary>
    /// <param name="mapData">Map data containing cells to smooth</param>
    /// <param name="iterations">Number of smoothing passes (more = smoother)</param>
    /// <param name="strength">Blending strength 0.0-1.0 (higher = more aggressive smoothing)</param>
    public void SmoothHeights(MapData mapData, int iterations = 3, double strength = 0.5)
    {
        if (iterations <= 0 || strength <= 0)
            return;

        Console.WriteLine($"[HeightSmoother] Smoothing heights: {iterations} iterations, strength={strength:F2}");

        strength = Math.Clamp(strength, 0.0, 1.0);

        for (int iter = 0; iter < iterations; iter++)
        {
            var newHeights = new byte[mapData.Cells.Count];

            // Calculate smoothed heights for all cells
            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                var cell = mapData.Cells[i];
                var neighbors = cell.Neighbors;

                if (neighbors.Count == 0)
                {
                    // No neighbors, keep original height
                    newHeights[i] = cell.Height;
                    continue;
                }

                // Calculate weighted average with neighbors
                double totalHeight = cell.Height * (1.0 - strength);
                double neighborAvg = 0;

                foreach (var neighborId in neighbors)
                {
                    if (neighborId >= 0 && neighborId < mapData.Cells.Count)
                    {
                        neighborAvg += mapData.Cells[neighborId].Height;
                    }
                }

                neighborAvg /= neighbors.Count;
                totalHeight += neighborAvg * strength;

                newHeights[i] = (byte)Math.Clamp((int)Math.Round(totalHeight), 0, 100);
            }

            // Apply smoothed heights
            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                mapData.Cells[i].Height = newHeights[i];
            }

            Console.WriteLine($"[HeightSmoother] Completed iteration {iter + 1}/{iterations}");
        }

        Console.WriteLine("[HeightSmoother] Smoothing complete!");
    }

    /// <summary>
    /// Smooths heights only for land cells, preserving ocean depths
    /// </summary>
    public void SmoothLandHeights(MapData mapData, int iterations = 3, double strength = 0.5)
    {
        Console.WriteLine($"[HeightSmoother] Smoothing land heights only: {iterations} iterations");

        strength = Math.Clamp(strength, 0.0, 1.0);

        for (int iter = 0; iter < iterations; iter++)
        {
            var newHeights = new byte[mapData.Cells.Count];

            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                var cell = mapData.Cells[i];

                // Skip ocean cells
                if (!cell.IsLand)
                {
                    newHeights[i] = cell.Height;
                    continue;
                }

                var neighbors = cell.Neighbors;
                if (neighbors.Count == 0)
                {
                    newHeights[i] = cell.Height;
                    continue;
                }

                // Only average with land neighbors
                double totalHeight = cell.Height * (1.0 - strength);
                double neighborAvg = 0;
                int landNeighborCount = 0;

                foreach (var neighborId in neighbors)
                {
                    if (neighborId >= 0 && neighborId < mapData.Cells.Count)
                    {
                        var neighbor = mapData.Cells[neighborId];
                        if (neighbor.IsLand)
                        {
                            neighborAvg += neighbor.Height;
                            landNeighborCount++;
                        }
                    }
                }

                if (landNeighborCount > 0)
                {
                    neighborAvg /= landNeighborCount;
                    totalHeight += neighborAvg * strength;
                }
                else
                {
                    // No land neighbors, keep original
                    totalHeight = cell.Height;
                }

                newHeights[i] = (byte)Math.Clamp((int)Math.Round(totalHeight), 0, 100);
            }

            // Apply smoothed heights
            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                if (mapData.Cells[i].IsLand)
                {
                    mapData.Cells[i].Height = newHeights[i];
                }
            }
        }

        Console.WriteLine("[HeightSmoother] Land smoothing complete!");
    }

    /// <summary>
    /// Applies median filter for noise reduction without blurring sharp features
    /// </summary>
    public void ApplyMedianFilter(MapData mapData, int radius = 1)
    {
        Console.WriteLine($"[HeightSmoother] Applying median filter: radius={radius}");

        var newHeights = new byte[mapData.Cells.Count];

        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            var cell = mapData.Cells[i];
            var heightValues = new List<byte> { cell.Height };

            // Collect heights from neighbors within radius
            CollectNeighborHeights(mapData, i, radius, heightValues);

            // Sort and take median
            heightValues.Sort();
            newHeights[i] = heightValues[heightValues.Count / 2];
        }

        // Apply filtered heights
        for (int i = 0; i < mapData.Cells.Count; i++)
        {
            mapData.Cells[i].Height = newHeights[i];
        }

        Console.WriteLine("[HeightSmoother] Median filter complete!");
    }

    /// <summary>
    /// Recursively collects neighbor heights within given radius
    /// </summary>
    private void CollectNeighborHeights(MapData mapData, int cellId, int radius, List<byte> heights, HashSet<int>? visited = null)
    {
        if (radius <= 0)
            return;

        visited ??= new HashSet<int> { cellId };

        var cell = mapData.Cells[cellId];
        foreach (var neighborId in cell.Neighbors)
        {
            if (neighborId >= 0 && neighborId < mapData.Cells.Count && visited.Add(neighborId))
            {
                heights.Add(mapData.Cells[neighborId].Height);

                if (radius > 1)
                {
                    CollectNeighborHeights(mapData, neighborId, radius - 1, heights, visited);
                }
            }
        }
    }

    /// <summary>
    /// Smooths heights with distance-based weighting
    /// Closer neighbors have more influence
    /// </summary>
    public void SmoothWithDistanceWeighting(MapData mapData, int iterations = 3, double maxDistance = 50.0)
    {
        Console.WriteLine($"[HeightSmoother] Smoothing with distance weighting: {iterations} iterations, maxDist={maxDistance}");

        for (int iter = 0; iter < iterations; iter++)
        {
            var newHeights = new double[mapData.Cells.Count];

            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                var cell = mapData.Cells[i];
                double totalWeight = 1.0;
                double weightedHeight = cell.Height;

                foreach (var neighborId in cell.Neighbors)
                {
                    if (neighborId >= 0 && neighborId < mapData.Cells.Count)
                    {
                        var neighbor = mapData.Cells[neighborId];
                        double distance = Distance(cell.Center, neighbor.Center);

                        if (distance < maxDistance)
                        {
                            // Inverse distance weighting
                            double weight = 1.0 / (1.0 + distance);
                            weightedHeight += neighbor.Height * weight;
                            totalWeight += weight;
                        }
                    }
                }

                newHeights[i] = weightedHeight / totalWeight;
            }

            // Apply smoothed heights
            for (int i = 0; i < mapData.Cells.Count; i++)
            {
                mapData.Cells[i].Height = (byte)Math.Clamp((int)Math.Round(newHeights[i]), 0, 100);
            }
        }

        Console.WriteLine("[HeightSmoother] Distance-weighted smoothing complete!");
    }

    /// <summary>
    /// Calculates Euclidean distance between two points
    /// </summary>
    private static double Distance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
