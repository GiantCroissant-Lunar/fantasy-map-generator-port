using Xunit;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Tests;

/// <summary>
/// Tests for topological invariants that must hold for any valid map
/// </summary>
public class TopologicalInvariantTests
{
    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Rivers_FlowDownhill_Monotonically(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        foreach (var river in map.Rivers)
        {
            for (int i = 0; i < river.Cells.Count - 1; i++)
            {
                var currentCell = map.Cells[river.Cells[i]];
                var nextCell = map.Cells[river.Cells[i + 1]];

                // Height must decrease (or stay same for lakes)
                Assert.True(
                    currentCell.Height >= nextCell.Height,
                    $"River {river.Id} flows uphill at segment {i}: cell {river.Cells[i]} (height {currentCell.Height}) → cell {river.Cells[i + 1]} (height {nextCell.Height})");
            }
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Coastline_IsBoundaryBetween_LandAndWater(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);
        var coastalCells = map.Cells.Where(c => c.IsLand && HasWaterNeighbor(map, c)).ToList();

        foreach (var cell in coastalCells)
        {
            bool hasLandNeighbor = false;
            bool hasWaterNeighbor = false;

            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = map.Cells[neighborId];
                if (neighbor.IsLand) hasLandNeighbor = true;
                if (neighbor.IsOcean) hasWaterNeighbor = true;
            }

            // Coastal cell must have both land and water neighbors
            Assert.True(hasLandNeighbor && hasWaterNeighbor,
                $"Cell {cell.Id} marked coastal but doesn't border both land and water. Land neighbors: {hasLandNeighbor}, Water neighbors: {hasWaterNeighbor}");
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void VoronoiCells_HaveValidNeighborGraph(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        foreach (var cell in map.Cells)
        {
            // Cell should have at least 3 neighbors (triangulation property)
            Assert.True(cell.Neighbors.Count >= 3,
                $"Cell {cell.Id} has only {cell.Neighbors.Count} neighbors (expected >= 3)");

            // All neighbor IDs should be valid
            foreach (var neighborId in cell.Neighbors)
            {
                Assert.True(neighborId >= 0 && neighborId < map.Cells.Count, 
                    $"Neighbor ID {neighborId} is out of range (0-{map.Cells.Count - 1})");

                // Symmetry: if A neighbors B, then B neighbors A
                var neighbor = map.Cells[neighborId];
                Assert.True(neighbor.Neighbors.Contains(cell.Id),
                    $"Cell {cell.Id} neighbors {neighborId}, but {neighborId} doesn't neighbor {cell.Id}");
            }
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Rivers_HaveNoCircularPaths(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        foreach (var river in map.Rivers)
        {
            var visited = new HashSet<int>();
            foreach (var cellId in river.Cells)
            {
                Assert.False(visited.Contains(cellId),
                    $"River {river.Id} has circular path: cell {cellId} appears twice");
                visited.Add(cellId);
            }
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Rivers_SourceAndMouthAreValid(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        foreach (var river in map.Rivers)
        {
            // Source should be valid
            Assert.True(river.Source >= 0 && river.Source < map.Cells.Count,
                $"River {river.Id} has invalid source {river.Source}");

            // Mouth should be valid
            Assert.True(river.Mouth >= 0 && river.Mouth < map.Cells.Count,
                $"River {river.Id} has invalid mouth {river.Mouth}");

            // Source should be in river cells
            Assert.True(river.Cells.Contains(river.Source),
                $"River {river.Id} source {river.Source} not in river cells");

            // Mouth should be in river cells
            Assert.True(river.Cells.Contains(river.Mouth),
                $"River {river.Id} mouth {river.Mouth} not in river cells");

            // Source should be higher than mouth
            var sourceCell = map.Cells[river.Source];
            var mouthCell = map.Cells[river.Mouth];
            Assert.True(sourceCell.Height >= mouthCell.Height,
                $"River {river.Id} source height {sourceCell.Height} < mouth height {mouthCell.Height}");
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void States_HaveValidCapitals(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        foreach (var state in map.States)
        {
            // Capital should be valid
            Assert.True(state.Capital >= 0 && state.Capital < map.Cells.Count,
                $"State {state.Id} has invalid capital {state.Capital}");

            var capitalCell = map.Cells[state.Capital];

            // Capital should belong to state
            Assert.True(state.Id == capitalCell.State,
                $"State {state.Id} capital cell {state.Capital} belongs to state {capitalCell.State}");

            // Capital should be on land
            Assert.True(capitalCell.IsLand,
                $"State {state.Id} capital {state.Capital} is not on land");
        }
    }

    [Theory]
    [Trait("Category", "Determinism")]
    [InlineData(12345, 8000)]
    [InlineData(67890, 16000)]
    [InlineData(11111, 4000)]
    public void Burgs_HaveValidAssignments(long seed, int numPoints)
    {
        var map = GenerateMap(seed, numPoints);

        foreach (var burg in map.Burgs)
        {
            // Burg should be valid
            Assert.True(burg.Cell >= 0 && burg.Cell < map.Cells.Count,
                $"Burg {burg.Id} has invalid cell {burg.Cell}");

            var burgCell = map.Cells[burg.Cell];

            // Burg should be on land
            Assert.True(burgCell.IsLand,
                $"Burg {burg.Id} is not on land");

            // Burg should belong to a state
            Assert.True(burg.State >= 0 && burg.State < map.States.Count,
                $"Burg {burg.Id} has invalid state {burg.State}");

            // Burg cell should match burg state
            Assert.True(burg.State == burgCell.State,
                $"Burg {burg.Id} state {burg.State} doesn't match cell state {burgCell.State}");
        }
    }

    /// <summary>
    /// Helper method to generate a map for testing
    /// </summary>
    private static MapData GenerateMap(long seed, int numPoints)
    {
        var settings = new MapGenerationSettings
        {
            Seed = seed,
            Width = 1000,
            Height = 1000,
            NumPoints = numPoints,
            RNGMode = RNGMode.PCG
        };

        var generator = new MapGenerator();
        return generator.Generate(settings);
    }

    /// <summary>
    /// Helper method to check if a cell has water neighbors
    /// </summary>
    private static bool HasWaterNeighbor(MapData map, Cell cell)
    {
        foreach (var neighborId in cell.Neighbors)
        {
            var neighbor = map.Cells[neighborId];
            if (neighbor.IsOcean) return true;
        }
        return false;
    }
}
