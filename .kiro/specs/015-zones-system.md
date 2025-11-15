# Spec 015: Zones System

## Status
- **State:** Not Started
- **Priority:** ⭐⭐ Nice to have
- **Estimated Effort:** 3-4 days
- **Dependencies:** None (can start anytime)
- **Blocks:** None (optional feature)

## Overview

Implement the zones system that defines special areas on the map with unique characteristics. Zones include danger zones (monster-infested areas), protected areas (nature reserves, sacred groves), and special biomes (magical forests, cursed lands).

## Goals

1. **Zone Types** - Danger, protected, special biome
2. **Zone Placement** - Strategic placement based on terrain
3. **Zone Properties** - Size, intensity, effects
4. **Zone Boundaries** - Define zone extents
5. **Zone Names** - Generate appropriate names

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/zones-generator.js`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ZoneType Type { get; set; }
    public List<int> Cells { get; set; } = new();
    public int CenterCellId { get; set; }
    public double Intensity { get; set; } // 0.0-1.0
    public string Description { get; set; } = string.Empty;
    public SKColor Color { get; set; }
}

public enum ZoneType
{
    // Dangerous
    DangerZone,      // Monster-infested
    Cursed,          // Cursed land
    Haunted,         // Haunted area
    Blighted,        // Diseased/corrupted
    
    // Protected
    NatureReserve,   // Protected wilderness
    SacredGrove,     // Religious protection
    RoyalHunt,       // Royal hunting grounds
    Sanctuary,       // Wildlife sanctuary
    
    // Special
    MagicalForest,   // Enchanted woods
    AncientRuins,    // Extensive ruins
    Wasteland,       // Barren wasteland
    Frontier         // Unexplored frontier
}
```

## Algorithm

### 1. Place Danger Zones

```csharp
private List<Zone> PlaceDangerZones()
{
    var zones = new List<Zone>();
    
    // Danger zones in remote, unpopulated areas
    var candidates = _map.Cells
        .Where(c => c.Height >= 20) // Land
        .Where(c => c.Population == 0) // Unpopulated
        .Where(c => c.BurgId == 0) // No settlements
        .Where(c => c.Height > 60 || c.BiomeId == 12) // Mountains or wetlands
        .ToList();
    
    if (!candidates.Any()) return zones;
    
    // Number based on map size
    int count = Math.Max(1, _map.Cells.Count / 8000);
    
    var tree = new QuadTree(_map.Width, _map.Height);
    double spacing = (_map.Width + _map.Height) / 2.0 / count;
    
    for (int i = 0; i < count * 3 && zones.Count < count; i++)
    {
        var center = candidates[_random.Next(candidates.Count)];
        
        if (tree.FindNearest(center.Center, spacing) != null)
            continue;
        
        // Expand zone from center
        var zoneCells = ExpandZone(center.Id, 5, 15, 
            c => c.Population == 0 && c.BurgId == 0);
        
        if (zoneCells.Count < 3) continue;
        
        zones.Add(new Zone
        {
            Id = zones.Count + 1,
            Name = GenerateDangerZoneName(),
            Type = ChooseDangerType(),
            Cells = zoneCells,
            CenterCellId = center.Id,
            Intensity = _random.NextDouble() * 0.5 + 0.5, // 0.5-1.0
            Description = GenerateDangerDescription(),
            Color = GetDangerZoneColor()
        });
        
        tree.Add(center.Center, zones.Count);
    }
    
    return zones;
}

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
```

### 2. Place Protected Areas

```csharp
private List<Zone> PlaceProtectedAreas()
{
    var zones = new List<Zone>();
    
    // Protected areas in forests or near sacred sites
    var candidates = _map.Cells
        .Where(c => c.Height >= 20)
        .Where(c => c.BiomeId >= 5 && c.BiomeId <= 9) // Forest biomes
        .Where(c => c.Population < 5) // Low population
        .ToList();
    
    if (!candidates.Any()) return zones;
    
    int count = Math.Max(1, _map.Cells.Count / 10000);
    
    for (int i = 0; i < count; i++)
    {
        var center = candidates[_random.Next(candidates.Count)];
        
        // Expand zone
        var zoneCells = ExpandZone(center.Id, 3, 10,
            c => c.BiomeId >= 5 && c.BiomeId <= 9);
        
        if (zoneCells.Count < 2) continue;
        
        zones.Add(new Zone
        {
            Id = _map.Zones.Count + zones.Count + 1,
            Name = GenerateProtectedAreaName(),
            Type = ChooseProtectedType(),
            Cells = zoneCells,
            CenterCellId = center.Id,
            Intensity = _random.NextDouble() * 0.3 + 0.3, // 0.3-0.6
            Description = GenerateProtectedDescription(),
            Color = GetProtectedAreaColor()
        });
    }
    
    return zones;
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
```

### 3. Place Special Zones

```csharp
private List<Zone> PlaceSpecialZones()
{
    var zones = new List<Zone>();
    
    // Magical forests
    var forestCandidates = _map.Cells
        .Where(c => c.BiomeId >= 7 && c.BiomeId <= 8) // Dense forests
        .Where(c => c.Population == 0)
        .ToList();
    
    if (forestCandidates.Any() && P(0.3))
    {
        var center = forestCandidates[_random.Next(forestCandidates.Count)];
        var zoneCells = ExpandZone(center.Id, 5, 20,
            c => c.BiomeId >= 7 && c.BiomeId <= 8);
        
        if (zoneCells.Count >= 5)
        {
            zones.Add(new Zone
            {
                Id = _map.Zones.Count + zones.Count + 1,
                Name = GenerateMagicalForestName(),
                Type = ZoneType.MagicalForest,
                Cells = zoneCells,
                CenterCellId = center.Id,
                Intensity = _random.NextDouble() * 0.4 + 0.6, // 0.6-1.0
                Description = "An enchanted forest filled with ancient magic",
                Color = new SKColor(100, 200, 100, 128)
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
            c => c.BiomeId == 1 || c.BiomeId == 2);
        
        if (zoneCells.Count >= 10)
        {
            zones.Add(new Zone
            {
                Id = _map.Zones.Count + zones.Count + 1,
                Name = GenerateWastelandName(),
                Type = ZoneType.Wasteland,
                Cells = zoneCells,
                CenterCellId = center.Id,
                Intensity = _random.NextDouble() * 0.3 + 0.7, // 0.7-1.0
                Description = "A desolate wasteland where few dare to tread",
                Color = new SKColor(200, 180, 150, 128)
            });
        }
    }
    
    return zones;
}
```

### 4. Expand Zone

```csharp
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
        var candidate = candidates.ElementAt(_random.Next(candidates.Count));
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
```

### 5. Name Generation

```csharp
private string GenerateDangerZoneName()
{
    var prefixes = new[] { "Dark", "Shadow", "Cursed", "Blighted", "Haunted", "Forbidden" };
    var suffixes = new[] { "Woods", "Moor", "Wastes", "Lands", "Vale", "Marsh" };
    
    return $"{prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
}

private string GenerateProtectedAreaName()
{
    var prefixes = new[] { "Sacred", "Ancient", "Royal", "Elder", "Blessed", "Hallowed" };
    var suffixes = new[] { "Grove", "Forest", "Woods", "Glade", "Sanctuary", "Reserve" };
    
    return $"{prefixes[_random.Next(prefixes.Length)]} {suffixes[_random.Next(suffixes.Length)]}";
}

private string GenerateMagicalForestName()
{
    var adjectives = new[] { "Enchanted", "Mystic", "Ethereal", "Arcane", "Fey" };
    var nouns = new[] { "Forest", "Woods", "Wildwood", "Greenwood" };
    
    return $"{adjectives[_random.Next(adjectives.Length)]} {nouns[_random.Next(nouns.Length)]}";
}

private string GenerateWastelandName()
{
    var adjectives = new[] { "Barren", "Desolate", "Scorched", "Blasted", "Dead" };
    var nouns = new[] { "Wastes", "Barrens", "Expanse", "Desert", "Lands" };
    
    return $"{adjectives[_random.Next(adjectives.Length)]} {nouns[_random.Next(nouns.Length)]}";
}
```

## Implementation Steps

### Step 1: Models (Day 1)
- [ ] Create `Zone.cs` model
- [ ] Create `ZoneType.cs` enum
- [ ] Add `Zones` to `MapData.cs`

### Step 2: Generator (Day 2)
- [ ] Create `ZonesGenerator.cs`
- [ ] Implement `ExpandZone()`
- [ ] Implement `PlaceDangerZones()`

### Step 3: Zone Types (Day 3)
- [ ] Implement `PlaceProtectedAreas()`
- [ ] Implement `PlaceSpecialZones()`
- [ ] Implement zone expansion logic

### Step 4: Integration & Testing (Day 4)
- [ ] Add to `MapGenerator.cs`
- [ ] Implement name generation
- [ ] Unit tests
- [ ] Integration tests
- [ ] Documentation

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Enable zone generation
    /// </summary>
    public bool GenerateZones { get; set; } = true;
    
    /// <summary>
    /// Enable danger zones
    /// </summary>
    public bool GenerateDangerZones { get; set; } = true;
    
    /// <summary>
    /// Enable protected areas
    /// </summary>
    public bool GenerateProtectedAreas { get; set; } = true;
    
    /// <summary>
    /// Enable special zones
    /// </summary>
    public bool GenerateSpecialZones { get; set; } = true;
    
    /// <summary>
    /// Zone density multiplier
    /// </summary>
    public double ZoneDensity { get; set; } = 1.0;
}
```

## Success Criteria

- [ ] Zones placed appropriately
- [ ] Zone sizes reasonable
- [ ] No overlapping zones
- [ ] Names generated appropriately
- [ ] All tests passing
- [ ] Performance < 500ms

## Dependencies

**Required:**
- Biomes (for placement logic)

**Optional:**
- Markers (danger zones near monster lairs)
- Religions (sacred groves near sacred sites)

## Notes

- Zones are optional flavor
- Should not overlap with settlements
- Intensity affects gameplay/rendering
- Names should match zone type
- Colors should be semi-transparent overlays

## References

- Original JS: `modules/zones-generator.js`
