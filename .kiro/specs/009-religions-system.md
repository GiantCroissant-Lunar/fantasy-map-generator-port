# Spec 009: Religions System

## Status
- **State:** ✅ Complete
- **Priority:** ⭐⭐⭐⭐ Important
- **Estimated Effort:** 1 week
- **Dependencies:** Cultures (008)
- **Blocks:** Theocracy states, Temple placement
- **Completed:** November 15, 2025

## Overview

Implement the religion system that defines belief systems, their spread across the map, and their influence on politics and culture. Religions can be organized (hierarchical), folk (traditional), cults (mystery religions), or heresies (splinter groups).

## Goals

1. **Religion Generation** - Create religions with types and characteristics
2. **Religion Placement** - Place religion origins strategically
3. **Deity Generation** - Create pantheons or monotheistic deities
4. **Religion Expansion** - Spread religions based on expansion type
5. **Statistics** - Calculate followers and geographic spread

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/religions-generator.js`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class Religion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SKColor Color { get; set; }
    
    // Geography
    public int CenterCellId { get; set; }
    public List<int> Origins { get; set; } = new();
    
    // Type & Characteristics
    public ReligionType Type { get; set; }
    public ExpansionType Expansion { get; set; }
    public string Form { get; set; } = "Pantheon"; // Pantheon, Monotheism, Dualism, etc.
    public List<Deity> Deities { get; set; } = new();
    
    // Statistics
    public int CellCount { get; set; }
    public double Area { get; set; }
    public double RuralPopulation { get; set; }
    public double UrbanPopulation { get; set; }
}

public enum ReligionType
{
    Organized,  // Major religion with hierarchy (Christianity, Islam)
    Folk,       // Traditional/tribal religion (Shinto, Animism)
    Cult,       // Mystery cult (Eleusinian Mysteries)
    Heresy      // Splinter from organized religion
}

public enum ExpansionType
{
    Global,     // Spreads everywhere (missionary religions)
    State,      // State religion (theocracy)
    Culture,    // Cultural religion (follows culture borders)
    Homeland    // Stays in origin area (ethnic religions)
}

public class Deity
{
    public string Name { get; set; } = string.Empty;
    public string Sphere { get; set; } = string.Empty; // War, Love, Death, etc.
}
```

## Algorithm

### 1. Generate Religions

```csharp
private List<Religion> GenerateReligions(int count)
{
    var religions = new List<Religion>
    {
        new Religion { Id = 0, Name = "No Religion" }
    };
    
    for (int i = 1; i <= count; i++)
    {
        var religion = new Religion
        {
            Id = i,
            Name = GenerateReligionName(),
            Color = GenerateReligionColor(i),
            Type = DetermineReligionType(),
            Expansion = DetermineExpansionType(),
            Form = DetermineReligionForm()
        };
        
        religion.Deities = GenerateDeities(religion.Form);
        religions.Add(religion);
    }
    
    return religions;
}

private ReligionType DetermineReligionType()
{
    return WeightedRandom(new Dictionary<ReligionType, int>
    {
        { ReligionType.Organized, 50 },
        { ReligionType.Folk, 30 },
        { ReligionType.Cult, 15 },
        { ReligionType.Heresy, 5 }
    });
}

private ExpansionType DetermineExpansionType()
{
    return WeightedRandom(new Dictionary<ExpansionType, int>
    {
        { ExpansionType.Global, 20 },
        { ExpansionType.State, 10 },
        { ExpansionType.Culture, 40 },
        { ExpansionType.Homeland, 30 }
    });
}
```

### 2. Place Religion Origins

```csharp
private void PlaceReligionOrigins(List<Religion> religions)
{
    var populated = _map.Cells.Where(c => c.Population > 0).ToList();
    var originTree = new QuadTree(_map.Width, _map.Height);
    
    double spacing = (_map.Width + _map.Height) / 2.0 / religions.Count;
    
    foreach (var religion in religions.Skip(1))
    {
        int centerCell = FindReligionOrigin(populated, originTree, spacing);
        
        religion.CenterCellId = centerCell;
        religion.Origins.Add(_map.Cells[centerCell].CultureId);
        
        _map.Cells[centerCell].ReligionId = religion.Id;
        originTree.Add(_map.Cells[centerCell].Center, religion.Id);
    }
}
```

### 3. Expand Religions

```csharp
private void ExpandReligions(List<Religion> religions)
{
    foreach (var religion in religions.Skip(1))
    {
        switch (religion.Expansion)
        {
            case ExpansionType.Global:
                ExpandGlobal(religion);
                break;
            case ExpansionType.State:
                ExpandByState(religion);
                break;
            case ExpansionType.Culture:
                ExpandByCulture(religion);
                break;
            case ExpansionType.Homeland:
                ExpandHomeland(religion);
                break;
        }
    }
}

private void ExpandGlobal(Religion religion)
{
    // Spreads to all populated cells with distance-based probability
    var center = _map.Cells[religion.CenterCellId];
    
    foreach (var cell in _map.Cells.Where(c => c.Population > 0))
    {
        double distance = Distance(center.Center, cell.Center);
        double maxDistance = Math.Sqrt(_map.Width * _map.Width + _map.Height * _map.Height);
        double probability = 1.0 - (distance / maxDistance);
        
        if (_random.NextDouble() < probability * 0.5)
        {
            cell.ReligionId = religion.Id;
        }
    }
}

private void ExpandByCulture(Religion religion)
{
    // Spreads within culture borders
    var originCulture = _map.Cells[religion.CenterCellId].CultureId;
    
    foreach (var cell in _map.Cells.Where(c => c.CultureId == originCulture))
    {
        if (_random.NextDouble() < 0.8)
        {
            cell.ReligionId = religion.Id;
        }
    }
}

private void ExpandHomeland(Religion religion)
{
    // Stays near origin
    var center = _map.Cells[religion.CenterCellId];
    double maxDistance = (_map.Width + _map.Height) / 10.0;
    
    foreach (var cell in _map.Cells.Where(c => c.Population > 0))
    {
        double distance = Distance(center.Center, cell.Center);
        if (distance < maxDistance && _random.NextDouble() < 0.7)
        {
            cell.ReligionId = religion.Id;
        }
    }
}
```

### 4. Generate Deities

```csharp
private List<Deity> GenerateDeities(string form)
{
    var deities = new List<Deity>();
    var spheres = new[] { "War", "Love", "Death", "Life", "Knowledge", 
                          "Nature", "Sky", "Sea", "Fire", "Earth" };
    
    int count = form switch
    {
        "Monotheism" => 1,
        "Dualism" => 2,
        "Pantheon" => _random.Next(3, 12),
        _ => _random.Next(1, 5)
    };
    
    for (int i = 0; i < count; i++)
    {
        deities.Add(new Deity
        {
            Name = GenerateDeityName(),
            Sphere = spheres[_random.Next(spheres.Length)]
        });
    }
    
    return deities;
}
```

## Implementation Steps

### Step 1: Models (Day 1)
- [x] Create `Religion.cs` model
- [x] Create `ReligionType.cs` enum
- [x] Create `ExpansionType.cs` enum
- [x] Create `Deity.cs` model
- [x] Add `ReligionId` to `Cell.cs`

### Step 2: Generator (Day 2-3)
- [x] Create `ReligionsGenerator.cs`
- [x] Implement `GenerateReligions()`
- [x] Implement `PlaceReligionOrigins()`
- [x] Implement `GenerateDeities()`

### Step 3: Expansion (Day 4-5)
- [x] Implement `ExpandReligions()`
- [x] Implement expansion types
- [x] Handle religion conflicts

### Step 4: Integration & Testing (Day 6-7)
- [x] Add to `MapGenerator.cs`
- [x] Unit tests
- [x] Integration tests
- [x] Documentation

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Number of religions to generate
    /// </summary>
    public int ReligionCount { get; set; } = 5;
}
```

## Success Criteria

- [x] Religions generated with types
- [x] Origins placed strategically
- [x] Deities generated appropriately
- [x] Religions expand based on type
- [x] All tests passing
- [x] Performance < 1 second

## Dependencies

**Required:**
- Cultures (for culture-based expansion)
- Cell population

**Blocks:**
- Theocracy state forms
- Temple placement in burgs

## References

- Original JS: `modules/religions-generator.js`
