# Spec 012: Markers System (Points of Interest)

## Status
- **State:** Not Started
- **Priority:** ⭐⭐ Nice to have
- **Estimated Effort:** 1 week
- **Dependencies:** None (can start anytime)
- **Blocks:** None (optional feature)

## Overview

Implement the markers system that places points of interest across the map. Markers include natural features (volcanoes, hot springs), historical sites (ruins, battlefields), religious locations (sacred sites, temples), and dangerous areas (monster lairs).

## Goals

1. **Natural Markers** - Volcanoes, hot springs, geysers, waterfalls, mines
2. **Historical Markers** - Ruins, battlefields, monuments
3. **Religious Markers** - Sacred sites, temples, shrines
4. **Dangerous Markers** - Monster lairs, danger zones
5. **Density Control** - Biome-based marker density
6. **Strategic Placement** - Markers placed based on terrain and context

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/markers-generator.js`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class Marker
{
    public int Id { get; set; }
    public MarkerType Type { get; set; }
    public int CellId { get; set; }
    public Point Position { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public enum MarkerType
{
    // Natural Features
    Volcano,
    HotSpring,
    Geyser,
    Waterfall,
    Mine,
    Cave,
    
    // Historical
    Ruins,
    Battlefield,
    Monument,
    AncientTree,
    
    // Religious
    SacredSite,
    Temple,
    Shrine,
    Pilgrimage,
    
    // Dangerous
    MonsterLair,
    DangerZone,
    Haunted,
    Cursed
}
```

## Algorithm

### 1. Place Volcanoes

```csharp
private List<Marker> PlaceVolcanoes()
{
    var markers = new List<Marker>();
    
    // Volcanoes in mountains near tectonic activity
    var candidates = _map.Cells
        .Where(c => c.Height >= 70) // Mountains
        .Where(c => c.Temperature > 0) // Not frozen
        .ToList();
    
    if (!candidates.Any()) return markers;
    
    // Number based on map size
    int count = (int)(_map.Cells.Count / 5000.0);
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
            Name = GenerateVolcanoName()
        });
        
        tree.Add(cell.Center, markers.Count);
    }
    
    return markers;
}
```

### 2. Place Hot Springs

```csharp
private List<Marker> PlaceHotSprings()
{
    var markers = new List<Marker>();
    
    // Hot springs near volcanoes or in geothermal areas
    var volcanoes = _map.Markers
        .Where(m => m.Type == MarkerType.Volcano)
        .ToList();
    
    foreach (var volcano in volcanoes)
    {
        // 1-3 hot springs near each volcano
        int count = _random.Next(1, 4);
        
        for (int i = 0; i < count; i++)
        {
            var cell = FindNearbyCell(volcano.CellId, 5, 15);
            
            if (cell != null && cell.Height >= 20)
            {
                markers.Add(new Marker
                {
                    Id = _map.Markers.Count + markers.Count + 1,
                    Type = MarkerType.HotSpring,
                    CellId = cell.Id,
                    Position = cell.Center,
                    Icon = "hotspring",
                    Name = GenerateHotSpringName()
                });
            }
        }
    }
    
    return markers;
}
```

### 3. Place Ruins

```csharp
private List<Marker> PlaceRuins()
{
    var markers = new List<Marker>();
    
    // Ruins in areas with history (near old burgs or battlefields)
    var candidates = _map.Cells
        .Where(c => c.Height >= 20) // Land only
        .Where(c => c.Population > 0 || c.BurgId > 0) // Populated areas
        .ToList();
    
    if (!candidates.Any()) return markers;
    
    // Number based on map age and size
    int count = (int)(_map.Cells.Count / 3000.0);
    count = Math.Clamp(count, 2, 20);
    
    for (int i = 0; i < count; i++)
    {
        var cell = candidates[_random.Next(candidates.Count)];
        
        markers.Add(new Marker
        {
            Id = _map.Markers.Count + markers.Count + 1,
            Type = MarkerType.Ruins,
            CellId = cell.Id,
            Position = cell.Center,
            Icon = "ruins",
            Name = GenerateRuinsName(),
            Description = GenerateRuinsDescription()
        });
    }
    
    return markers;
}
```

### 4. Place Battlefields

```csharp
private List<Marker> PlaceBattlefields()
{
    var markers = new List<Marker>();
    
    // Battlefields at state borders or historical war sites
    foreach (var state in _map.States.Where(s => s.Id > 0))
    {
        // Check for wars in state history
        var wars = state.Campaigns
            .Where(c => c.EndYear < _settings.CurrentYear - 10)
            .ToList();
        
        foreach (var war in wars.Take(3)) // Max 3 per state
        {
            // Find border cell with enemy
            var borderCells = _map.Cells
                .Where(c => c.StateId == state.Id)
                .Where(c => c.Neighbors.Any(n => 
                    _map.Cells[n].StateId != state.Id))
                .ToList();
            
            if (borderCells.Any())
            {
                var cell = borderCells[_random.Next(borderCells.Count)];
                
                markers.Add(new Marker
                {
                    Id = _map.Markers.Count + markers.Count + 1,
                    Type = MarkerType.Battlefield,
                    CellId = cell.Id,
                    Position = cell.Center,
                    Icon = "battlefield",
                    Name = $"Battle of {war.Name}",
                    Description = $"Site of the {war.Name} ({war.StartYear}-{war.EndYear})"
                });
            }
        }
    }
    
    return markers;
}
```

### 5. Place Sacred Sites

```csharp
private List<Marker> PlaceSacredSites()
{
    var markers = new List<Marker>();
    
    // Sacred sites for each major religion
    foreach (var religion in _map.Religions.Where(r => r.Id > 0))
    {
        // 1-3 sacred sites per religion
        int count = _random.Next(1, 4);
        
        for (int i = 0; i < count; i++)
        {
            // Find cell with this religion
            var candidates = _map.Cells
                .Where(c => c.ReligionId == religion.Id)
                .Where(c => c.Height >= 20)
                .ToList();
            
            if (!candidates.Any()) continue;
            
            var cell = candidates[_random.Next(candidates.Count)];
            
            // Prefer mountains or special terrain
            if (P(0.5))
            {
                var mountains = candidates.Where(c => c.Height > 60).ToList();
                if (mountains.Any())
                    cell = mountains[_random.Next(mountains.Count)];
            }
            
            markers.Add(new Marker
            {
                Id = _map.Markers.Count + markers.Count + 1,
                Type = MarkerType.SacredSite,
                CellId = cell.Id,
                Position = cell.Center,
                Icon = "sacredsite",
                Name = GenerateSacredSiteName(religion),
                Description = $"Sacred to {religion.Name}"
            });
        }
    }
    
    return markers;
}
```

### 6. Place Monster Lairs

```csharp
private List<Marker> PlaceMonsterLairs()
{
    var markers = new List<Marker>();
    
    // Monster lairs in remote, dangerous areas
    var candidates = _map.Cells
        .Where(c => c.Height >= 20) // Land
        .Where(c => c.Population == 0) // Unpopulated
        .Where(c => c.BurgId == 0) // No settlements
        .Where(c => c.Height > 50 || c.BiomeId == 12) // Mountains or wetlands
        .ToList();
    
    if (!candidates.Any()) return markers;
    
    int count = (int)(_map.Cells.Count / 4000.0);
    count = Math.Clamp(count, 1, 15);
    
    for (int i = 0; i < count; i++)
    {
        var cell = candidates[_random.Next(candidates.Count)];
        
        markers.Add(new Marker
        {
            Id = _map.Markers.Count + markers.Count + 1,
            Type = MarkerType.MonsterLair,
            CellId = cell.Id,
            Position = cell.Center,
            Icon = "monsterlair",
            Name = GenerateMonsterLairName(),
            Description = GenerateMonsterDescription()
        });
    }
    
    return markers;
}
```

## Implementation Steps

### Step 1: Models (Day 1)
- [ ] Create `Marker.cs` model
- [ ] Create `MarkerType.cs` enum
- [ ] Add `Markers` to `MapData.cs`

### Step 2: Generator (Day 2-3)
- [ ] Create `MarkersGenerator.cs`
- [ ] Implement `PlaceVolcanoes()`
- [ ] Implement `PlaceHotSprings()`
- [ ] Implement `PlaceGeysers()`

### Step 3: Historical Markers (Day 4)
- [ ] Implement `PlaceRuins()`
- [ ] Implement `PlaceBattlefields()`
- [ ] Implement `PlaceMonuments()`

### Step 4: Religious & Dangerous (Day 5)
- [ ] Implement `PlaceSacredSites()`
- [ ] Implement `PlaceMonsterLairs()`
- [ ] Implement `PlaceDangerZones()`

### Step 5: Name Generation (Day 6)
- [ ] Implement marker name generators
- [ ] Implement description generators

### Step 6: Integration & Testing (Day 7)
- [ ] Add to `MapGenerator.cs`
- [ ] Unit tests
- [ ] Integration tests
- [ ] Documentation

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Enable marker generation
    /// </summary>
    public bool GenerateMarkers { get; set; } = true;
    
    /// <summary>
    /// Marker density multiplier
    /// </summary>
    public double MarkerDensity { get; set; } = 1.0;
    
    /// <summary>
    /// Enable natural markers (volcanoes, hot springs)
    /// </summary>
    public bool GenerateNaturalMarkers { get; set; } = true;
    
    /// <summary>
    /// Enable historical markers (ruins, battlefields)
    /// </summary>
    public bool GenerateHistoricalMarkers { get; set; } = true;
    
    /// <summary>
    /// Enable religious markers (sacred sites)
    /// </summary>
    public bool GenerateReligiousMarkers { get; set; } = true;
    
    /// <summary>
    /// Enable dangerous markers (monster lairs)
    /// </summary>
    public bool GenerateDangerousMarkers { get; set; } = true;
}
```

## Success Criteria

- [ ] Markers placed strategically
- [ ] Appropriate density per biome
- [ ] Names generated appropriately
- [ ] No markers in invalid locations
- [ ] All tests passing
- [ ] Performance < 1 second

## Dependencies

**Required:**
- Biomes (for placement logic)
- States (for battlefields)
- Religions (for sacred sites)

**Optional:**
- Campaigns (for battlefield placement)

## Notes

- Markers are optional flavor
- Density should be configurable
- Names should be culturally appropriate
- Placement should make sense (volcanoes in mountains, etc.)

## References

- Original JS: `modules/markers-generator.js`
