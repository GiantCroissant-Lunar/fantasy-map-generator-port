using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Generates special zones on the map (danger zones, protected areas, special biomes)
/// </summary>
public class ZonesGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public ZonesGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    /// <summary>
    /// Generate all zones
    /// </summary>
    public void Generate()
    {
        if (!_settings.GenerateZones)
            return;

        var zones = new List<Zone>();

        // Generate different zone types
        if (_settings.GenerateDangerZones)
            zones.AddRange(PlaceDangerZones());

        if (_settings.GenerateProtectedAreas)
            zones.AddRange(PlaceProtectedAreas());

        if (_settings.GenerateSpecialZones)
            zones.AddRange(PlaceSpecialZones());

        _map.Zones = zones;
    }

    /// <summary>
    /// Place danger zones in remote, unpopulated areas
    /// </summary>
    private List<Zone> PlaceDangerZones()
    {
        var zones = new List<Zone>();

        // Find candidates: remote, unpopulated land
        var candidates = _map.Cells
            .Where(c => c.Height >= 20) // Land
            .Where(c => c.Population == 0) // Unpopulated
            .Where(c => c.Burg == 0 || c.Burg == -1) // No settlements
            .Where(c => c.Height > 60 || c.BiomeId == 12) // Mountains or wetlands
            .ToList();

        if (!candidates.Any())
            return zones;

        // Number based on map size and density
        int count = Math.Max(1, (int)(_map.Cells.Count / 8000.0 * _settings.ZoneDensity));

        // Use simple spacing to avoid clustering
        var usedCells = new HashSet<int>();
        double minSpacing = Math.Sqrt(_map.Width * _map.Height) / (count * 2.0);

        for (int attempt = 0; attempt < count * 3 && zones.Count < count; attempt++)
        {
            var center = candidates[_random.Next(candidates.Count)];

            // Check spacing
            bool tooClose = zones.Any(z =>
            {
                var zCenter = _map.Cells[z.CenterCellId].Center;
                var dist = Math.Sqrt(
                    Math.Pow(center.Center.X - zCenter.X, 2) +
                    Math.Pow(center.Center.Y - zCenter.Y, 2));
                return dist < minSpacing;
            });

            if (tooClose)
                continue;

            // Expand zone from center
            var zoneCells = ExpandZone(center.Id, 5, 15,
                c => c.Population == 0 && (c.Burg == 0 || c.Burg == -1) && !usedCells.Contains(c.Id));

            if (zoneCells.Count < 3)
                continue;

            // Mark cells as used
            foreach (var cellId in zoneCells)
                usedCells.Add(cellId);

            var zoneType = ChooseDangerType();
            zones.Add(new Zone
            {
                Id = zones.Count + 1,
                Name = GenerateDangerZoneName(zoneType),
                Type = zoneType,
                Cells = zoneCells,
                CenterCellId = center.Id,
                Intensity = _random.NextDouble() * 0.5 + 0.5, // 0.5-1.0
                Description = GenerateDangerDescription(zoneType),
                Color = GetDangerZoneColor(zoneType)
            });
        }

        return zones;
    }

    /// <summary>
    /// Place protected areas in forests or near sacred sites
    /// </summary>
    private List<Zone> PlaceProtectedAreas()
    {
        var zones = new List<Zone>();

        // Find candidates: forests with low population
        var candidates = _map.Cells
            .Where(c => c.Height >= 20)
            .Where(c => c.BiomeId >= 5 && c.BiomeId <= 9) // Forest biomes
            .Where(c => c.Population < 5) // Low population
            .ToList();

        if (!candidates.Any())
            return zones;

        int count = Math.Max(1, (int)(_map.Cells.Count / 10000.0 * _settings.ZoneDensity));

        var usedCells = new HashSet<int>();

        for (int i = 0; i < count * 2 && zones.Count < count; i++)
        {
            var center = candidates[_random.Next(candidates.Count)];

            // Expand zone
            var zoneCells = ExpandZone(center.Id, 3, 10,
                c => c.BiomeId >= 5 && c.BiomeId <= 9 && !usedCells.Contains(c.Id));

            if (zoneCells.Count < 2)
                continue;

            // Mark cells as used
            foreach (var cellId in zoneCells)
                usedCells.Add(cellId);

            var zoneType = ChooseProtectedType();
            zones.Add(new Zone
            {
                Id = _map.Zones.Count + zones.Count + 1,
                Name = GenerateProtectedAreaName(zoneType),
                Type = zoneType,
                Cells = zoneCells,
                CenterCellId = center.Id,
                Intensity = _random.NextDouble() * 0.3 + 0.3, // 0.3-0.6
                Description = GenerateProtectedDescription(zoneType),
                Color = GetProtectedAreaColor(zoneType)
            });
        }

        return zones;
    }

    /// <summary>
    /// Place special zones (magical forests, wastelands, etc.)
    /// </summary>
    private List<Zone> PlaceSpecialZones()
    {
        var zones = new List<Zone>();
        var usedCells = new HashSet<int>();

        // Magical forests
        var forestCandidates = _map.Cells
            .Where(c => c.BiomeId >= 7 && c.BiomeId <= 8) // Dense forests
            .Where(c => c.Population == 0)
            .ToList();

        if (forestCandidates.Any() && P(0.3))
        {
            var center = forestCandidates[_random.Next(forestCandidates.Count)];
            var zoneCells = ExpandZone(center.Id, 5, 20,
                c => c.BiomeId >= 7 && c.BiomeId <= 8 && !usedCells.Contains(c.Id));

            if (zoneCells.Count >= 5)
            {
                foreach (var cellId in zoneCells)
                    usedCells.Add(cellId);

                zones.Add(new Zone
                {
                    Id = _map.Zones.Count + zones.Count + 1,
                    Name = GenerateMagicalForestName(),
                    Type = ZoneType.MagicalForest,
                    Cells = zoneCells,
                    CenterCellId = center.Id,
                    Intensity = _random.NextDouble() * 0.4 + 0.6, // 0.6-1.0
                    Description = "An enchanted forest filled with ancient magic",
                    Color = "#64C86480" // Green with alpha
                });
            }
        }

        // Wastelands
        var wastelandCandidates = _map.Cells
            .Where(c => c.BiomeId == 1 || c.BiomeId == 2) // Deserts
            .Where(c => c.Population == 0)
            .ToList();

        if (wastelandCandidates.Any() && P(0.2))
        {
            var center = wastelandCandidates[_random.Next(wastelandCandidates.Count)];
            var zoneCells = ExpandZone(center.Id, 10, 30,
                c => (c.BiomeId == 1 || c.BiomeId == 2) && !usedCells.Contains(c.Id));

            if (zoneCells.Count >= 10)
            {
                foreach (var cellId in zoneCells)
                    usedCells.Add(cellId);

                zones.Add(new Zone
                {
                    Id = _map.Zones.Count + zones.Count + 1,
                    Name = GenerateWastelandName(),
                    Type = ZoneType.Wasteland,
                    Cells = zoneCells,
                    CenterCellId = center.Id,
                    Intensity = _random.NextDouble() * 0.3 + 0.7, // 0.7-1.0
                    Description = "A desolate wasteland where few dare to tread",
                    Color = "#C8B49680" // Tan with alpha
                });
            }
        }

        return zones;
    }

    /// <summary>
    /// Expand a zone from a center cell
    /// </summary>
    private List<int> ExpandZone(
        int centerCell,
        int minSize,
        int maxSize,
        Func<Cell, bool> predicate)
    {
        var zoneCells = new List<int> { centerCell };
        var candidates = new HashSet<int>();

        // Add neighbors of center
        foreach (var neighbor in _map.Cells[centerCell].Neighbors)
        {
            if (predicate(_map.Cells[neighbor]))
                candidates.Add(neighbor);
        }

        // Expand until we reach desired size
        int targetSize = _random.Next(minSize, maxSize + 1);

        while (zoneCells.Count < targetSize && candidates.Any())
        {
            // Pick random candidate
            var candidateList = candidates.ToList();
            var candidate = candidateList[_random.Next(candidateList.Count)];
            candidates.Remove(candidate);

            // Add to zone
            zoneCells.Add(candidate);

            // Add its neighbors as candidates
            foreach (var neighbor in _map.Cells[candidate].Neighbors)
            {
                if (!zoneCells.Contains(neighbor) &&
                    !candidates.Contains(neighbor) &&
                    predicate(_map.Cells[neighbor]))
                {
                    candidates.Add(neighbor);
                }
            }
        }

        return zoneCells;
    }

    // === Zone Type Selection ===

    private ZoneType ChooseDangerType()
    {
        return WeightedRandom(new Dictionary<ZoneType, int>
        {
            { ZoneType.DangerZone, 40 },
            { ZoneType.Cursed, 20 },
            { ZoneType.Haunted, 20 },
            { ZoneType.Blighted, 20 }
        });
    }

    private ZoneType ChooseProtectedType()
    {
        return WeightedRandom(new Dictionary<ZoneType, int>
        {
            { ZoneType.NatureReserve, 30 },
            { ZoneType.SacredGrove, 25 },
            { ZoneType.RoyalHunt, 25 },
            { ZoneType.Sanctuary, 20 }
        });
    }

    // === Name Generation ===

    private string GenerateDangerZoneName(ZoneType type)
    {
        var prefixes = type switch
        {
            ZoneType.Cursed => new[] { "Cursed", "Doomed", "Forsaken", "Damned" },
            ZoneType.Haunted => new[] { "Haunted", "Spectral", "Ghost", "Phantom" },
            ZoneType.Blighted => new[] { "Blighted", "Diseased", "Corrupted", "Tainted" },
            _ => new[] { "Dark", "Shadow", "Forbidden", "Dread" }
        };

        var suffixes = new[] { "Woods", "Moor", "Wastes", "Lands", "Vale", "Marsh", "Hills", "Forest" };

        return $"{prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
    }

    private string GenerateProtectedAreaName(ZoneType type)
    {
        var prefixes = type switch
        {
            ZoneType.SacredGrove => new[] { "Sacred", "Holy", "Blessed", "Hallowed" },
            ZoneType.RoyalHunt => new[] { "Royal", "King's", "Queen's", "Imperial" },
            ZoneType.Sanctuary => new[] { "Wildlife", "Nature", "Peaceful", "Tranquil" },
            _ => new[] { "Ancient", "Elder", "Primeval", "Old" }
        };

        var suffixes = type switch
        {
            ZoneType.SacredGrove => new[] { "Grove", "Glade", "Sanctuary", "Temple" },
            ZoneType.RoyalHunt => new[] { "Hunt", "Forest", "Woods", "Reserve" },
            _ => new[] { "Reserve", "Sanctuary", "Forest", "Woods", "Glade" }
        };

        return $"{prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
    }

    private string GenerateMagicalForestName()
    {
        var adjectives = new[] { "Enchanted", "Mystic", "Ethereal", "Arcane", "Fey", "Magical" };
        var nouns = new[] { "Forest", "Woods", "Wildwood", "Greenwood", "Grove" };

        return $"{adjectives[_random.Next(adjectives.Length)]} {nouns[_random.Next(nouns.Length)]}";
    }

    private string GenerateWastelandName()
    {
        var adjectives = new[] { "Barren", "Desolate", "Scorched", "Blasted", "Dead", "Lifeless" };
        var nouns = new[] { "Wastes", "Barrens", "Expanse", "Desert", "Lands", "Sands" };

        return $"{adjectives[_random.Next(adjectives.Length)]} {nouns[_random.Next(nouns.Length)]}";
    }

    // === Description Generation ===

    private string GenerateDangerDescription(ZoneType type)
    {
        return type switch
        {
            ZoneType.Cursed => "A cursed land where dark magic lingers",
            ZoneType.Haunted => "Haunted by restless spirits and shadows",
            ZoneType.Blighted => "Corrupted by disease and decay",
            _ => "A dangerous area infested with monsters"
        };
    }

    private string GenerateProtectedDescription(ZoneType type)
    {
        return type switch
        {
            ZoneType.SacredGrove => "A sacred grove protected by ancient traditions",
            ZoneType.RoyalHunt => "Royal hunting grounds reserved for nobility",
            ZoneType.Sanctuary => "A wildlife sanctuary where hunting is forbidden",
            _ => "A protected wilderness area"
        };
    }

    // === Color Generation ===

    private string GetDangerZoneColor(ZoneType type)
    {
        return type switch
        {
            ZoneType.Cursed => "#80008080", // Purple with alpha
            ZoneType.Haunted => "#40404080", // Dark gray with alpha
            ZoneType.Blighted => "#80800080", // Yellow-green with alpha
            _ => "#80000080" // Red with alpha
        };
    }

    private string GetProtectedAreaColor(ZoneType type)
    {
        return type switch
        {
            ZoneType.SacredGrove => "#FFD70080", // Gold with alpha
            ZoneType.RoyalHunt => "#8000FF80", // Royal purple with alpha
            ZoneType.Sanctuary => "#00C80080", // Green with alpha
            _ => "#00800080" // Dark green with alpha
        };
    }

    // === Utility Methods ===

    private T WeightedRandom<T>(Dictionary<T, int> weights) where T : notnull
    {
        int total = weights.Values.Sum();
        int roll = _random.Next(total);

        int cumulative = 0;
        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative)
                return kvp.Key;
        }

        return weights.Keys.First();
    }

    private bool P(double probability)
    {
        return _random.NextDouble() < probability;
    }
}
