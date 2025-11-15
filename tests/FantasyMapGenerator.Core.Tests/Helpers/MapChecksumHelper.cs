using System.Security.Cryptography;
using System.Text;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Core.Tests.Helpers;

/// <summary>
/// Helper class for computing deterministic checksums of map data
/// </summary>
public static class MapChecksumHelper
{
    /// <summary>
    /// Computes a SHA256 checksum of map data for deterministic verification
    /// </summary>
    /// <param name="map">The map data to checksum</param>
    /// <returns>Lowercase hex string of SHA256 hash</returns>
    public static string ComputeMapChecksum(MapData map)
    {
        using var sha256 = SHA256.Create();
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write map metadata
        writer.Write(map.Width);
        writer.Write(map.Height);
        writer.Write(map.CellsDesired);
        writer.Write(map.Scale);

        // Write heights array
        if (map.Heights != null)
        {
            writer.Write(map.Heights.Length);
            foreach (var height in map.Heights)
            {
                writer.Write(height);
            }
        }
        else
        {
            writer.Write(0);
        }

        // Write cells (sorted by ID for deterministic order)
        var sortedCells = map.Cells.OrderBy(c => c.Id).ToList();
        writer.Write(sortedCells.Count);
        foreach (var cell in sortedCells)
        {
            writer.Write(cell.Id);
            writer.Write(cell.Height);
            writer.Write(cell.Biome);
            writer.Write(cell.State);
            writer.Write(cell.Culture);
            writer.Write(cell.IsLand);
            writer.Write(cell.IsOcean);
            writer.Write(cell.IsBorder);
            writer.Write(cell.HasRiver);
            writer.Write(cell.HasRoad);
            writer.Write(cell.Population);
            writer.Write(cell.Temperature);
            writer.Write(cell.Precipitation);
            
            // Write neighbor IDs (sorted for determinism)
            var sortedNeighbors = cell.Neighbors.OrderBy(n => n).ToList();
            writer.Write(sortedNeighbors.Count);
            foreach (var neighborId in sortedNeighbors)
            {
                writer.Write(neighborId);
            }
        }

        // Write rivers (sorted by ID)
        var sortedRivers = map.Rivers.OrderBy(r => r.Id).ToList();
        writer.Write(sortedRivers.Count);
        foreach (var river in sortedRivers)
        {
            writer.Write(river.Id);
            writer.Write(river.Source);
            writer.Write(river.Mouth);
            writer.Write(river.Width);
            writer.Write(river.Length);
            writer.Write(river.IsSeasonal);
            writer.Write(river.ParentRiver ?? -1);
            
            // Write river cells
            writer.Write(river.Cells.Count);
            foreach (var cellId in river.Cells)
            {
                writer.Write(cellId);
            }
            
            // Write tributaries
            var sortedTributaries = river.Tributaries.OrderBy(t => t).ToList();
            writer.Write(sortedTributaries.Count);
            foreach (var tributaryId in sortedTributaries)
            {
                writer.Write(tributaryId);
            }
        }

        // Write states (sorted by ID)
        var sortedStates = map.States.OrderBy(s => s.Id).ToList();
        writer.Write(sortedStates.Count);
        foreach (var state in sortedStates)
        {
            writer.Write(state.Id);
            writer.Write(state.Capital);
            writer.Write(state.Culture);
            writer.Write(state.Color ?? "");
            writer.Write(state.Founded.Ticks);
        }

        // Write burgs (sorted by ID)
        var sortedBurgs = map.Burgs.OrderBy(b => b.Id).ToList();
        writer.Write(sortedBurgs.Count);
        foreach (var burg in sortedBurgs)
        {
            writer.Write(burg.Id);
            writer.Write(burg.Name ?? "");
            writer.Write(burg.Cell);
            writer.Write(burg.State);
            writer.Write(burg.Culture);
            writer.Write(burg.IsCapital);
        }

        ms.Position = 0;
        var hash = sha256.ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Computes a simple checksum for testing purposes (faster than SHA256)
    /// </summary>
    /// <param name="map">The map data to checksum</param>
    /// <returns>Simple hash code as string</returns>
    public static string ComputeSimpleChecksum(MapData map)
    {
        var hash = 0;
        
        // Include basic map properties
        hash = CombineHash(hash, map.Width);
        hash = CombineHash(hash, map.Height);
        hash = CombineHash(hash, map.CellsDesired);
        
        // Include heights
        if (map.Heights != null)
        {
            foreach (var height in map.Heights)
            {
                hash = CombineHash(hash, height);
            }
        }
        
        // Include cell properties
        foreach (var cell in map.Cells.OrderBy(c => c.Id))
        {
            hash = CombineHash(hash, cell.Id);
            hash = CombineHash(hash, cell.Height);
            hash = CombineHash(hash, cell.Biome);
            hash = CombineHash(hash, cell.State);
        }
        
        // Include rivers
        foreach (var river in map.Rivers.OrderBy(r => r.Id))
        {
            hash = CombineHash(hash, river.Id);
            hash = CombineHash(hash, river.Source);
            hash = CombineHash(hash, river.Mouth);
            foreach (var cellId in river.Cells)
            {
                hash = CombineHash(hash, cellId);
            }
        }
        
        return hash.ToString("X");
    }
    
    private static int CombineHash(int hash, int value)
    {
        return ((hash << 5) + hash) ^ value;
    }
}
