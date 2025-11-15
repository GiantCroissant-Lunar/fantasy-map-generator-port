namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

public class MarkersGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public MarkersGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    public List<Marker> Generate()
    {
        Console.WriteLine("Generating markers...");

        var markers = new List<Marker>();

        // Natural markers
        if (_settings.GenerateNaturalMarkers)
        {
            markers.AddRange(PlaceVolcanoes());
            markers.AddRange(PlaceHotSprings(markers));
            markers.AddRange(PlaceCaves());
        }

        // Historical markers
        if (_settings.GenerateHistoricalMarkers)
        {
            markers.AddRange(PlaceRuins());
            markers.AddRange(PlaceBattlefields());
        }

        // Religious markers
        if (_settings.GenerateReligiousMarkers && _map.Religions.Count > 1)
        {
            markers.AddRange(PlaceSacredSites());
        }

        // Dangerous markers
        if (_settings.GenerateDangerousMarkers)
        {
            markers.AddRange(PlaceMonsterLairs());
        }

        Console.WriteLine($"Generated {markers.Count} markers");
        return markers;
    }

    private List<Marker> PlaceVolcanoes()
    {
        var markers = new List<Marker>();

        // Volcanoes in mountains
        var candidates = _map.Cells
            .Where(c => c.Height >= 70) // Mountains
            .Where(c => c.Temperature > 0) // Not frozen
            .ToList();

        if (!candidates.Any()) return markers;

        // Number based on map size
        int count = (int)(_map.Cells.Count / 5000.0 * _settings.MarkerDensity);
        count = Math.Clamp(count, 1, 10);

        var tree = new QuadTree(_map.Width, _map.Height);
        double spacing = (_map.Width + _map.Height) / 2.0 / count;

        for (int i = 0; i < count * 3 && markers.Count < count; i++)
        {
            var cell = candidates[_random.Next(candidates.Count)];

            if (tree.FindNearest(cell.Center, spacing) != null)
                continue;

            markers.Add(new Marker
            {
                Id = markers.Count + 1,
                Type = MarkerType.Volcano,
                CellId = cell.Id,
                Position = cell.Center,
                Icon = "volcano",
                Name = $"Mount {GenerateName()}",
                Description = "An active volcano"
            });

            tree.Add(cell.Center, markers.Count);
        }

        return markers;
    }

    private List<Marker> PlaceHotSprings(List<Marker> existingMarkers)
    {
        var markers = new List<Marker>();

        // Hot springs near volcanoes
        var volcanoes = existingMarkers
            .Where(m => m.Type == MarkerType.Volcano)
            .ToList();

        foreach (var volcano in volcanoes)
        {
            // 1-2 hot springs near each volcano
            int count = _random.Next(1, 3);

            for (int i = 0; i < count; i++)
            {
                var cell = FindNearbyCell(volcano.CellId, 5, 15);

                if (cell != null && cell.Height >= 20)
                {
                    markers.Add(new Marker
                    {
                        Id = existingMarkers.Count + markers.Count + 1,
                        Type = MarkerType.HotSpring,
                        CellId = cell.Id,
                        Position = cell.Center,
                        Icon = "hotspring",
                        Name = $"{GenerateName()} Springs",
                        Description = "Natural hot springs"
                    });
                }
            }
        }

        return markers;
    }

    private List<Marker> PlaceCaves()
    {
        var markers = new List<Marker>();

        // Caves in mountains and hills
        var candidates = _map.Cells
            .Where(c => c.Height >= 40 && c.Height < 80) // Hills and lower mountains
            .ToList();

        if (!candidates.Any()) return markers;

        int count = (int)(_map.Cells.Count / 4000.0 * _settings.MarkerDensity);
        count = Math.Clamp(count, 2, 15);

        for (int i = 0; i < count; i++)
        {
            var cell = candidates[_random.Next(candidates.Count)];

            markers.Add(new Marker
            {
                Id = markers.Count + 1,
                Type = MarkerType.Cave,
                CellId = cell.Id,
                Position = cell.Center,
                Icon = "cave",
                Name = $"{GenerateName()} Cave",
                Description = "A dark cave"
            });
        }

        return markers;
    }

    private List<Marker> PlaceRuins()
    {
        var markers = new List<Marker>();

        // Ruins in populated or formerly populated areas
        var candidates = _map.Cells
            .Where(c => c.Height >= 20) // Land only
            .Where(c => c.Population > 0 || c.Burg > 0) // Populated areas
            .ToList();

        if (!candidates.Any()) return markers;

        int count = (int)(_map.Cells.Count / 3000.0 * _settings.MarkerDensity);
        count = Math.Clamp(count, 2, 20);

        for (int i = 0; i < count; i++)
        {
            var cell = candidates[_random.Next(candidates.Count)];

            var ruinsType = _random.Next(3) switch
            {
                0 => "Ancient",
                1 => "Forgotten",
                _ => "Lost"
            };

            markers.Add(new Marker
            {
                Id = markers.Count + 1,
                Type = MarkerType.Ruins,
                CellId = cell.Id,
                Position = cell.Center,
                Icon = "ruins",
                Name = $"{ruinsType} Ruins of {GenerateName()}",
                Description = "Crumbling ruins of a forgotten civilization"
            });
        }

        return markers;
    }

    private List<Marker> PlaceBattlefields()
    {
        var markers = new List<Marker>();

        // Battlefields at state borders
        foreach (var state in _map.States.Where(s => s.Id > 0).Take(10))
        {
            // Find border cells
            var borderCells = _map.Cells
                .Where(c => c.StateId == state.Id)
                .Where(c => c.Neighbors.Any(n =>
                    _map.Cells[n].StateId != state.Id && _map.Cells[n].StateId > 0))
                .ToList();

            if (borderCells.Any() && P(0.3)) // 30% chance per state
            {
                var cell = borderCells[_random.Next(borderCells.Count)];

                markers.Add(new Marker
                {
                    Id = markers.Count + 1,
                    Type = MarkerType.Battlefield,
                    CellId = cell.Id,
                    Position = cell.Center,
                    Icon = "battlefield",
                    Name = $"Battle of {GenerateName()}",
                    Description = "Site of a historic battle"
                });
            }
        }

        return markers;
    }

    private List<Marker> PlaceSacredSites()
    {
        var markers = new List<Marker>();

        // Sacred sites for major religions
        foreach (var religion in _map.Religions.Where(r => r.Id > 0).Take(10))
        {
            // 1-2 sacred sites per religion
            int count = _random.Next(1, 3);

            for (int i = 0; i < count; i++)
            {
                // Find cell with this religion
                var candidates = _map.Cells
                    .Where(c => c.ReligionId == religion.Id)
                    .Where(c => c.Height >= 20)
                    .ToList();

                if (!candidates.Any()) continue;

                var cell = candidates[_random.Next(candidates.Count)];

                // Prefer mountains (50% chance)
                if (P(0.5))
                {
                    var mountains = candidates.Where(c => c.Height > 60).ToList();
                    if (mountains.Any())
                        cell = mountains[_random.Next(mountains.Count)];
                }

                markers.Add(new Marker
                {
                    Id = markers.Count + 1,
                    Type = MarkerType.SacredSite,
                    CellId = cell.Id,
                    Position = cell.Center,
                    Icon = "sacredsite",
                    Name = $"Shrine of {GenerateName()}",
                    Description = $"Sacred to {religion.Name}"
                });
            }
        }

        return markers;
    }

    private List<Marker> PlaceMonsterLairs()
    {
        var markers = new List<Marker>();

        // Monster lairs in remote areas
        var candidates = _map.Cells
            .Where(c => c.Height >= 20) // Land
            .Where(c => c.Population == 0) // Unpopulated
            .Where(c => c.Burg == 0) // No settlements
            .Where(c => c.Height > 50) // Mountains/hills
            .ToList();

        if (!candidates.Any()) return markers;

        int count = (int)(_map.Cells.Count / 4000.0 * _settings.MarkerDensity);
        count = Math.Clamp(count, 1, 15);

        for (int i = 0; i < count; i++)
        {
            var cell = candidates[_random.Next(candidates.Count)];

            var monsterType = _random.Next(4) switch
            {
                0 => "Dragon",
                1 => "Giant",
                2 => "Troll",
                _ => "Beast"
            };

            markers.Add(new Marker
            {
                Id = markers.Count + 1,
                Type = MarkerType.MonsterLair,
                CellId = cell.Id,
                Position = cell.Center,
                Icon = "monsterlair",
                Name = $"{monsterType} Lair",
                Description = $"Lair of a dangerous {monsterType.ToLower()}"
            });
        }

        return markers;
    }

    private Cell? FindNearbyCell(int centerCellId, int minDistance, int maxDistance)
    {
        var center = _map.Cells[centerCellId];
        var candidates = new List<Cell>();

        foreach (var cell in _map.Cells)
        {
            double distance = Distance(center.Center, cell.Center);
            if (distance >= minDistance && distance <= maxDistance)
            {
                candidates.Add(cell);
            }
        }

        return candidates.Any() ? candidates[_random.Next(candidates.Count)] : null;
    }

    private double Distance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private bool P(double probability)
    {
        return _random.NextDouble() < probability;
    }

    private string GenerateName()
    {
        // Simple name generation
        var syllables = new[] { "Kar", "Mor", "Dun", "Eld", "Fen", "Grim", "Hal", "Kor", "Lor", "Nar", "Oth", "Ral", "Sil", "Thal", "Vor", "Wyn" };
        return syllables[_random.Next(syllables.Length)] + syllables[_random.Next(syllables.Length)].ToLower();
    }
}
