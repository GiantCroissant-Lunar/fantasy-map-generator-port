using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Minimal deterministic dungeon generator stub (rooms and corridors)
/// </summary>
public static class DungeonGenerator
{
    public static Dungeon GenerateForCell(MapData map, int cellId, IRandomSource rng, string name = "Dungeon")
    {
        var cell = map.GetCell(cellId) ?? throw new ArgumentException("Invalid cell id");
        var dungeon = new Dungeon
        {
            Id = map.Dungeons.Count,
            Name = name,
            Origin = cell.Center,
            AnchorCellId = cellId,
            Width = 48,
            Height = 32,
            Cells = new bool[32, 48]
        };

        // Very simple BSP-ish rooms
        int rooms = 6 + rng.Next(0, 5);
        var rects = new List<(int x, int y, int w, int h)>();
        for (int i = 0; i < rooms; i++)
        {
            int w = 5 + rng.Next(0, 8);
            int h = 4 + rng.Next(0, 6);
            int x = rng.Next(1, Math.Max(2, dungeon.Width - w - 1));
            int y = rng.Next(1, Math.Max(2, dungeon.Height - h - 1));
            rects.Add((x, y, w, h));
            for (int yy = y; yy < y + h && yy < dungeon.Height; yy++)
                for (int xx = x; xx < x + w && xx < dungeon.Width; xx++)
                    dungeon.Cells[yy, xx] = true;
        }

        // Connect rooms with simple corridors between centers
        rects.Sort((a, b) => (a.x + a.w / 2).CompareTo(b.x + b.w / 2));
        for (int i = 1; i < rects.Count; i++)
        {
            var (ax, ay, aw, ah) = rects[i - 1];
            var (bx, by, bw, bh) = rects[i];
            int cx0 = ax + aw / 2;
            int cy0 = ay + ah / 2;
            int cx1 = bx + bw / 2;
            int cy1 = by + bh / 2;
            // carve L-shaped corridor
            for (int x = Math.Min(cx0, cx1); x <= Math.Max(cx0, cx1); x++)
                if (cy0 >= 0 && cy0 < dungeon.Height && x >= 0 && x < dungeon.Width) dungeon.Cells[cy0, x] = true;
            for (int y = Math.Min(cy0, cy1); y <= Math.Max(cy0, cy1); y++)
                if (cx1 >= 0 && cx1 < dungeon.Width && y >= 0 && y < dungeon.Height) dungeon.Cells[y, cx1] = true;
        }

        return dungeon;
    }
}

