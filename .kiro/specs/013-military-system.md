# Spec 013: Military System

## Status
- **State:** Not Started
- **Priority:** ⭐⭐ Nice to have
- **Estimated Effort:** 1 week
- **Dependencies:** States (007), Burgs (006)
- **Blocks:** None (optional feature)

## Overview

Implement the military system that places armies, navies, and garrisons across the map. Military units represent a state's armed forces and are positioned strategically based on threats, borders, and resources.

## Goals

1. **Unit Types** - Infantry, cavalry, archers, navy, siege
2. **Garrison Placement** - Defensive forces in burgs
3. **Army Placement** - Field armies near borders
4. **Navy Placement** - Naval forces at ports
5. **Strength Calculation** - Based on population and resources
6. **Strategic Positioning** - Units placed based on threats

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/military-generator.js`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class MilitaryUnit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StateId { get; set; }
    public int CellId { get; set; }
    public Point Position { get; set; }
    public UnitType Type { get; set; }
    public int Strength { get; set; }
    public UnitStatus Status { get; set; }
    public int? GarrisonBurgId { get; set; }
}

public enum UnitType
{
    Infantry,
    Cavalry,
    Archers,
    Navy,
    Siege,
    Artillery,
    Militia
}

public enum UnitStatus
{
    Garrison,   // Defending a burg
    Field,      // Active field army
    Reserve,    // In reserve
    Patrol      // Patrolling borders
}
```

## Algorithm

### 1. Calculate Military Strength

```csharp
private int CalculateMilitaryStrength(State state)
{
    // Base strength on population and economy
    double totalPopulation = state.RuralPopulation + state.UrbanPopulation;
    
    // Military participation rate (1-5% of population)
    double participationRate = 0.02; // 2% average
    
    // Adjust by state type
    if (state.Type == StateType.Nomadic)
        participationRate *= 1.5; // Nomads are more militarized
    else if (state.Type == StateType.Naval)
        participationRate *= 1.2; // Naval powers need crews
    
    // Adjust by government form
    if (state.Form == StateForm.Empire)
        participationRate *= 1.3;
    else if (state.Form.ToString().Contains("Republic"))
        participationRate *= 0.9;
    
    // Calculate total strength
    int strength = (int)(totalPopulation * participationRate);
    
    // Minimum strength
    return Math.Max(strength, 100);
}
```

### 2. Place Garrisons

```csharp
private List<MilitaryUnit> PlaceGarrisons(State state)
{
    var units = new List<MilitaryUnit>();
    
    // Get all burgs in state
    var burgs = _map.Burgs
        .Where(b => b != null && b.StateId == state.Id)
        .OrderByDescending(b => b.Population)
        .ToList();
    
    if (!burgs.Any()) return units;
    
    // Calculate total garrison strength (30% of military)
    int totalStrength = CalculateMilitaryStrength(state);
    int garrisonStrength = (int)(totalStrength * 0.3);
    
    // Distribute among burgs
    foreach (var burg in burgs)
    {
        // Garrison strength based on burg importance
        double burgShare = burg.Population / burgs.Sum(b => b.Population);
        int strength = (int)(garrisonStrength * burgShare);
        
        if (strength < 10) continue; // Too small
        
        // Determine unit composition
        var unitTypes = DetermineGarrisonComposition(burg, state);
        
        foreach (var (type, percentage) in unitTypes)
        {
            int unitStrength = (int)(strength * percentage);
            if (unitStrength < 5) continue;
            
            units.Add(new MilitaryUnit
            {
                Id = _map.MilitaryUnits.Count + units.Count + 1,
                Name = GenerateUnitName(type, state),
                StateId = state.Id,
                CellId = burg.CellId,
                Position = burg.Position,
                Type = type,
                Strength = unitStrength,
                Status = UnitStatus.Garrison,
                GarrisonBurgId = burg.Id
            });
        }
    }
    
    return units;
}

private List<(UnitType, double)> DetermineGarrisonComposition(Burg burg, State state)
{
    var composition = new List<(UnitType, double)>();
    
    if (burg.IsCapital)
    {
        // Capitals have elite guards
        composition.Add((UnitType.Infantry, 0.4));
        composition.Add((UnitType.Cavalry, 0.3));
        composition.Add((UnitType.Archers, 0.2));
        composition.Add((UnitType.Artillery, 0.1));
    }
    else if (burg.IsPort)
    {
        // Ports have naval garrisons
        composition.Add((UnitType.Navy, 0.4));
        composition.Add((UnitType.Infantry, 0.4));
        composition.Add((UnitType.Archers, 0.2));
    }
    else
    {
        // Regular burgs
        composition.Add((UnitType.Infantry, 0.5));
        composition.Add((UnitType.Archers, 0.3));
        composition.Add((UnitType.Militia, 0.2));
    }
    
    return composition;
}
```

### 3. Place Field Armies

```csharp
private List<MilitaryUnit> PlaceFieldArmies(State state)
{
    var units = new List<MilitaryUnit>();
    
    // Calculate field army strength (50% of military)
    int totalStrength = CalculateMilitaryStrength(state);
    int fieldStrength = (int)(totalStrength * 0.5);
    
    // Number of armies based on state size
    int armyCount = Math.Max(1, state.BurgCount / 3);
    int strengthPerArmy = fieldStrength / armyCount;
    
    // Place armies near borders or threats
    var borderCells = GetBorderCells(state);
    var threats = IdentifyThreats(state);
    
    for (int i = 0; i < armyCount; i++)
    {
        // Find strategic position
        Cell? position = null;
        
        if (threats.Any() && P(0.7))
        {
            // Near threat
            var threat = threats[_random.Next(threats.Count)];
            position = FindNearbyCell(threat.CellId, 3, 10);
        }
        else if (borderCells.Any())
        {
            // On border
            position = borderCells[_random.Next(borderCells.Count)];
        }
        else
        {
            // Near capital
            var capital = _map.Burgs.First(b => b.Id == state.CapitalBurgId);
            position = _map.Cells[capital.CellId];
        }
        
        if (position == null) continue;
        
        // Determine army composition
        var composition = DetermineArmyComposition(state);
        
        foreach (var (type, percentage) in composition)
        {
            int unitStrength = (int)(strengthPerArmy * percentage);
            if (unitStrength < 10) continue;
            
            units.Add(new MilitaryUnit
            {
                Id = _map.MilitaryUnits.Count + units.Count + 1,
                Name = GenerateArmyName(i + 1, state),
                StateId = state.Id,
                CellId = position.Id,
                Position = position.Center,
                Type = type,
                Strength = unitStrength,
                Status = UnitStatus.Field
            });
        }
    }
    
    return units;
}

private List<(UnitType, double)> DetermineArmyComposition(State state)
{
    var composition = new List<(UnitType, double)>();
    
    if (state.Type == StateType.Nomadic)
    {
        // Nomads are cavalry-heavy
        composition.Add((UnitType.Cavalry, 0.6));
        composition.Add((UnitType.Archers, 0.3));
        composition.Add((UnitType.Infantry, 0.1));
    }
    else if (state.Type == StateType.Highland)
    {
        // Highlanders prefer infantry
        composition.Add((UnitType.Infantry, 0.6));
        composition.Add((UnitType.Archers, 0.3));
        composition.Add((UnitType.Cavalry, 0.1));
    }
    else
    {
        // Balanced army
        composition.Add((UnitType.Infantry, 0.4));
        composition.Add((UnitType.Cavalry, 0.3));
        composition.Add((UnitType.Archers, 0.2));
        composition.Add((UnitType.Siege, 0.1));
    }
    
    return composition;
}
```

### 4. Place Navies

```csharp
private List<MilitaryUnit> PlaceNavies(State state)
{
    var units = new List<MilitaryUnit>();
    
    // Only for states with ports
    var ports = _map.Burgs
        .Where(b => b != null && b.StateId == state.Id && b.IsPort)
        .ToList();
    
    if (!ports.Any()) return units;
    
    // Calculate naval strength (20% of military for naval powers)
    int totalStrength = CalculateMilitaryStrength(state);
    double navalPercentage = state.Type == StateType.Naval ? 0.4 : 0.2;
    int navalStrength = (int)(totalStrength * navalPercentage);
    
    // Distribute among ports
    foreach (var port in ports)
    {
        double portShare = port.Population / ports.Sum(p => p.Population);
        int strength = (int)(navalStrength * portShare);
        
        if (strength < 20) continue;
        
        units.Add(new MilitaryUnit
        {
            Id = _map.MilitaryUnits.Count + units.Count + 1,
            Name = GenerateFleetName(port, state),
            StateId = state.Id,
            CellId = port.CellId,
            Position = port.Position,
            Type = UnitType.Navy,
            Strength = strength,
            Status = UnitStatus.Garrison,
            GarrisonBurgId = port.Id
        });
    }
    
    return units;
}
```

### 5. Identify Threats

```csharp
private List<Cell> IdentifyThreats(State state)
{
    var threats = new List<Cell>();
    
    // Check diplomatic relations
    foreach (var (neighborId, status) in state.Diplomacy)
    {
        if (status == DiplomaticStatus.Enemy || status == DiplomaticStatus.Rival)
        {
            // Find border with this neighbor
            var borderCells = _map.Cells
                .Where(c => c.StateId == state.Id)
                .Where(c => c.Neighbors.Any(n => _map.Cells[n].StateId == neighborId))
                .ToList();
            
            threats.AddRange(borderCells);
        }
    }
    
    return threats;
}

private List<Cell> GetBorderCells(State state)
{
    return _map.Cells
        .Where(c => c.StateId == state.Id)
        .Where(c => c.Neighbors.Any(n => _map.Cells[n].StateId != state.Id))
        .ToList();
}
```

### 6. Name Generation

```csharp
private string GenerateUnitName(UnitType type, State state)
{
    var culture = _map.Cultures[state.CultureId];
    
    return type switch
    {
        UnitType.Infantry => $"{culture.Name} Infantry",
        UnitType.Cavalry => $"{culture.Name} Cavalry",
        UnitType.Archers => $"{culture.Name} Archers",
        UnitType.Navy => $"{culture.Name} Fleet",
        UnitType.Siege => $"{culture.Name} Siege Corps",
        UnitType.Artillery => $"{culture.Name} Artillery",
        UnitType.Militia => $"{culture.Name} Militia",
        _ => $"{culture.Name} Regiment"
    };
}

private string GenerateArmyName(int number, State state)
{
    var ordinal = GetOrdinal(number);
    return $"{ordinal} Army of {state.Name}";
}

private string GenerateFleetName(Burg port, State state)
{
    return $"{port.Name} Fleet";
}

private string GetOrdinal(int number)
{
    return number switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{number}th"
    };
}
```

## Implementation Steps

### Step 1: Models (Day 1)
- [ ] Create `MilitaryUnit.cs` model
- [ ] Create `UnitType.cs` enum
- [ ] Create `UnitStatus.cs` enum
- [ ] Add `MilitaryUnits` to `MapData.cs`

### Step 2: Strength Calculation (Day 2)
- [ ] Create `MilitaryGenerator.cs`
- [ ] Implement `CalculateMilitaryStrength()`
- [ ] Implement composition logic

### Step 3: Garrison Placement (Day 3)
- [ ] Implement `PlaceGarrisons()`
- [ ] Implement `DetermineGarrisonComposition()`

### Step 4: Field Armies (Day 4)
- [ ] Implement `PlaceFieldArmies()`
- [ ] Implement `DetermineArmyComposition()`
- [ ] Implement `IdentifyThreats()`

### Step 5: Navies (Day 5)
- [ ] Implement `PlaceNavies()`
- [ ] Implement fleet distribution

### Step 6: Name Generation (Day 6)
- [ ] Implement unit name generators
- [ ] Implement army name generators
- [ ] Implement fleet name generators

### Step 7: Integration & Testing (Day 7)
- [ ] Add to `MapGenerator.cs`
- [ ] Unit tests
- [ ] Integration tests
- [ ] Documentation

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Enable military unit generation
    /// </summary>
    public bool GenerateMilitary { get; set; } = true;
    
    /// <summary>
    /// Military participation rate (percentage of population)
    /// </summary>
    public double MilitaryParticipationRate { get; set; } = 0.02; // 2%
    
    /// <summary>
    /// Garrison percentage (of total military)
    /// </summary>
    public double GarrisonPercentage { get; set; } = 0.3; // 30%
    
    /// <summary>
    /// Field army percentage (of total military)
    /// </summary>
    public double FieldArmyPercentage { get; set; } = 0.5; // 50%
    
    /// <summary>
    /// Naval percentage (of total military)
    /// </summary>
    public double NavalPercentage { get; set; } = 0.2; // 20%
}
```

## Success Criteria

- [ ] Military strength calculated appropriately
- [ ] Garrisons placed in all major burgs
- [ ] Field armies positioned strategically
- [ ] Navies placed at ports
- [ ] Unit composition makes sense
- [ ] All tests passing
- [ ] Performance < 2 seconds

## Dependencies

**Required:**
- States (007)
- Burgs (006)
- Diplomacy (part of States)

**Optional:**
- Campaigns (for historical context)

## Notes

- Military is optional flavor
- Strength should scale with population
- Positioning should be strategic
- Unit types should match culture/terrain
- Names should be culturally appropriate

## References

- Original JS: `modules/military-generator.js`
