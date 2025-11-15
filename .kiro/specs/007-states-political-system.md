# Spec 007: States (Political System)

## Status
- **State:** Not Started
- **Priority:** ⭐⭐⭐⭐⭐ Critical (Core political feature)
- **Estimated Effort:** 2 weeks
- **Dependencies:** Burgs (006), Cultures (008)
- **Blocks:** Provinces (009), Diplomacy, Military

## Overview

Implement the state (nation/kingdom) system including state creation, territorial expansion, diplomacy, and political forms. States are the primary political entities that control territory and interact with each other.

## Goals

1. **State Creation** - Create states from capitals with culture and type
2. **Territorial Expansion** - Expand state borders using Dijkstra algorithm
3. **Border Normalization** - Clean up irregular borders
4. **Diplomacy** - Generate relationships between states
5. **State Forms** - Assign government types (monarchy, republic, etc.)
6. **Statistics** - Calculate area, population, and other metrics

## Reference Implementation

**Source:** `ref-projects/Fantasy-Map-Generator/modules/burgs-and-states.js`
- Lines 300-500: State creation and expansion
- Lines 500-700: Diplomacy generation
- Lines 700-887: State forms and statistics

## Data Models

### State Model

```csharp
namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a political state (nation, kingdom, empire, etc.)
/// </summary>
public class State
{
    /// <summary>
    /// Unique identifier (0 = neutral/wildlands)
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// State name (e.g., "Angshire")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Full formal name (e.g., "Kingdom of Angshire")
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// State color for map display
    /// </summary>
    public SKColor Color { get; set; }
    
    // Geography
    
    /// <summary>
    /// Capital burg ID
    /// </summary>
    public int CapitalBurgId { get; set; }
    
    /// <summary>
    /// Center cell (capital location)
    /// </summary>
    public int CenterCellId { get; set; }
    
    /// <summary>
    /// Pole of inaccessibility (geometric center)
    /// </summary>
    public Point Pole { get; set; }
    
    /// <summary>
    /// Neighboring state IDs
    /// </summary>
    public List<int> Neighbors { get; set; } = new();
    
    // Culture & Politics
    
    /// <summary>
    /// Primary culture ID
    /// </summary>
    public int CultureId { get; set; }
    
    /// <summary>
    /// Government form (monarchy, republic, etc.)
    /// </summary>
    public StateForm Form { get; set; }
    
    /// <summary>
    /// State type based on geography
    /// </summary>
    public StateType Type { get; set; }
    
    /// <summary>
    /// Expansionism factor (0.5-2.0, affects growth)
    /// </summary>
    public double Expansionism { get; set; }
    
    // Statistics
    
    /// <summary>
    /// Number of cells controlled
    /// </summary>
    public int CellCount { get; set; }
    
    /// <summary>
    /// Total area in square kilometers
    /// </summary>
    public double Area { get; set; }
    
    /// <summary>
    /// Number of burgs in state
    /// </summary>
    public int BurgCount { get; set; }
    
    /// <summary>
    /// Rural population (thousands)
    /// </summary>
    public double RuralPopulation { get; set; }
    
    /// <summary>
    /// Urban population (thousands)
    /// </summary>
    public double UrbanPopulation { get; set; }
    
    // Diplomacy
    
    /// <summary>
    /// Diplomatic relations with other states
    /// Key = state ID, Value = diplomatic status
    /// </summary>
    public Dictionary<int, DiplomaticStatus> Diplomacy { get; set; } = new();
    
    /// <summary>
    /// Historical military campaigns
    /// </summary>
    public List<Campaign> Campaigns { get; set; } = new();
    
    /// <summary>
    /// Coat of arms
    /// </summary>
    public CoatOfArms CoA { get; set; } = new();
    
    /// <summary>
    /// True if state is locked (won't be modified)
    /// </summary>
    public bool IsLocked { get; set; }
}

/// <summary>
/// Government form/type
/// </summary>
public enum StateForm
{
    // Monarchies (by size)
    Duchy,              // Small monarchy
    GrandDuchy,         // Medium monarchy
    Principality,       // Medium monarchy
    Kingdom,            // Large monarchy
    Empire,             // Huge monarchy
    
    // Republics
    Republic,
    Federation,
    TradeCompany,
    MostSereneRepublic,
    Oligarchy,
    Tetrarchy,
    Triumvirate,
    Diarchy,
    Junta,
    
    // Unions
    Union,
    League,
    Confederation,
    UnitedKingdom,
    UnitedRepublic,
    UnitedProvinces,
    Commonwealth,
    Heptarchy,
    
    // Theocracies
    Theocracy,
    Brotherhood,
    Thearchy,
    See,
    HolyState,
    
    // Anarchies
    FreeTerritory,
    Council,
    Commune,
    Community
}

/// <summary>
/// State type based on geography and culture
/// </summary>
public enum StateType
{
    Generic,
    Naval,      // Seafaring state
    Lake,       // Lake-focused state
    Highland,   // Mountain state
    River,      // River-focused state
    Nomadic,    // Desert/steppe nomads
    Hunting     // Forest hunters
}

/// <summary>
/// Diplomatic relationship status
/// </summary>
public enum DiplomaticStatus
{
    Ally,       // Military alliance
    Friendly,   // Good relations
    Neutral,    // No special relationship
    Suspicion,  // Distrustful
    Rival,      // Competing interests
    Enemy,      // At war
    Suzerain,   // Overlord of vassal
    Vassal,     // Subject to suzerain
    Unknown     // No contact
}

/// <summary>
/// Historical military campaign
/// </summary>
public class Campaign
{
    public string Name { get; set; } = string.Empty;
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public int AttackerId { get; set; }
    public int DefenderId { get; set; }
}
```

### Cell Extensions

```csharp
// Add to Cell.cs
public class Cell
{
    // ... existing properties ...
    
    /// <summary>
    /// State ID this cell belongs to (0 = neutral)
    /// </summary>
    public int StateId { get; set; }
}
```

## Algorithm

### 1. Create States

```csharp
private List<State> CreateStates(List<Burg> capitals)
{
    var states = new List<State>
    {
        new State { Id = 0, Name = "Neutrals" } // Index 0 = neutral
    };
    
    var colors = GenerateStateColors(capitals.Count - 1);
    
    for (int i = 1; i < capitals.Count; i++)
    {
        var capital = capitals[i];
        var cell = _map.Cells[capital.CellId];
        var culture = _map.Cultures[capital.CultureId];
        
        // Calculate expansionism
        double expansionism = _random.NextDouble() * _settings.SizeVariety + 1.0;
        
        // Generate state name
        string basename = capital.Name.Length < 9 && Every5th(capital.CellId) 
            ? capital.Name 
            : GenerateCultureName(capital.CultureId);
        string stateName = GenerateStateName(basename, capital.CultureId);
        
        var state = new State
        {
            Id = i,
            Name = stateName,
            Color = colors[i - 1],
            CapitalBurgId = capital.Id,
            CenterCellId = capital.CellId,
            CultureId = capital.CultureId,
            Type = DetermineStateType(cell, culture),
            Expansionism = expansionism,
            CoA = GenerateCoatOfArms(culture.Type)
        };
        
        states.Add(state);
        
        // Assign capital cell to state
        cell.StateId = i;
        capital.StateId = i;
    }
    
    return states;
}

private StateType DetermineStateType(Cell cell, Culture culture)
{
    // Inherit from culture type
    return culture.Type switch
    {
        CultureType.Naval => StateType.Naval,
        CultureType.Lake => StateType.Lake,
        CultureType.Highland => StateType.Highland,
        CultureType.River => StateType.River,
        CultureType.Nomadic => StateType.Nomadic,
        CultureType.Hunting => StateType.Hunting,
        _ => StateType.Generic
    };
}
```

### 2. Expand States (Dijkstra Algorithm)

```csharp
private void ExpandStates(List<State> states)
{
    var queue = new PriorityQueue<ExpansionNode, double>();
    var costs = new Dictionary<int, double>();
    
    double maxCost = _map.Cells.Count * 0.6 * _settings.GrowthRate;
    
    // Initialize: add capital cells to queue
    foreach (var state in states.Where(s => s.Id > 0))
    {
        var cell = _map.Cells[state.CenterCellId];
        var culture = _map.Cultures[state.CultureId];
        
        queue.Enqueue(
            new ExpansionNode(state.CenterCellId, state.Id, 0, cell.BiomeId),
            0);
        costs[state.CenterCellId] = 0;
    }
    
    // Expand using Dijkstra
    while (queue.Count > 0)
    {
        var node = queue.Dequeue();
        var cell = _map.Cells[node.CellId];
        var state = states[node.StateId];
        
        // Expand to neighbors
        foreach (var neighborId in cell.Neighbors)
        {
            var neighbor = _map.Cells[neighborId];
            
            // Skip if locked state
            if (neighbor.StateId > 0 && states[neighbor.StateId].IsLocked)
                continue;
            
            // Skip if capital of another state
            if (neighbor.BurgId > 0 && _map.Burgs[neighbor.BurgId].IsCapital)
                continue;
            
            // Calculate expansion cost
            double cost = CalculateExpansionCost(
                cell, neighbor, state, node.NativeBiome);
            
            double totalCost = node.Cost + cost;
            
            // Stop if too expensive
            if (totalCost > maxCost)
                continue;
            
            // Update if better path found
            if (!costs.ContainsKey(neighborId) || totalCost < costs[neighborId])
            {
                if (neighbor.Height >= 20) // Land only
                    neighbor.StateId = node.StateId;
                
                costs[neighborId] = totalCost;
                queue.Enqueue(
                    new ExpansionNode(neighborId, node.StateId, totalCost, node.NativeBiome),
                    totalCost);
            }
        }
    }
    
    // Update burg states
    foreach (var burg in _map.Burgs.Where(b => b != null && !b.IsCapital))
    {
        burg.StateId = _map.Cells[burg.CellId].StateId;
    }
}

private double CalculateExpansionCost(
    Cell from, Cell to, State state, int nativeBiome)
{
    // Culture cost
    double cultureCost = to.CultureId == state.CultureId ? -9 : 100;
    
    // Population cost (prefer populated areas)
    double populationCost = to.Height < 20 ? 0 
        : to.Population > 0 ? Math.Max(20 - to.Population, 0) 
        : 5000;
    
    // Biome cost
    double biomeCost = GetBiomeCost(nativeBiome, to.BiomeId, state.Type);
    
    // Height cost
    double heightCost = GetHeightCost(to, state.Type);
    
    // River cost
    double riverCost = GetRiverCost(to, state.Type);
    
    // Type cost (coastline vs inland)
    double typeCost = GetTypeCost(to, state.Type);
    
    double totalCost = cultureCost + populationCost + biomeCost + 
                      heightCost + riverCost + typeCost;
    
    return Math.Max(totalCost, 0) / state.Expansionism;
}

private double GetBiomeCost(int nativeBiome, int biome, StateType type)
{
    if (nativeBiome == biome) return 10; // Native biome bonus
    
    var biomeCost = _map.Biomes[biome].Cost;
    
    if (type == StateType.Hunting)
        return biomeCost * 2; // Hunters prefer native biome
    
    if (type == StateType.Nomadic && biome >= 5 && biome <= 9)
        return biomeCost * 3; // Nomads avoid forests
    
    return biomeCost;
}

private double GetHeightCost(Cell cell, StateType type)
{
    var feature = _map.Features[cell.FeatureId];
    
    if (type == StateType.Lake && feature.Type == FeatureType.Lake)
        return 10; // Lake cultures cross lakes easily
    
    if (type == StateType.Naval && cell.Height < 20)
        return 300; // Naval states cross seas
    
    if (type == StateType.Nomadic && cell.Height < 20)
        return 10000; // Nomads can't cross water
    
    if (cell.Height < 20)
        return 1000; // General water crossing penalty
    
    if (type == StateType.Highland && cell.Height < 62)
        return 1100; // Highlanders prefer mountains
    
    if (type == StateType.Highland)
        return 0; // No penalty for highlands
    
    if (cell.Height >= 67)
        return 2200; // Mountain crossing
    
    if (cell.Height >= 44)
        return 300; // Hill crossing
    
    return 0;
}

private double GetRiverCost(Cell cell, StateType type)
{
    if (type == StateType.River)
        return cell.RiverId > 0 ? 0 : 100;
    
    if (cell.RiverId == 0)
        return 0;
    
    return Math.Clamp(cell.Flux / 10.0, 20, 100);
}

private double GetTypeCost(Cell cell, StateType type)
{
    if (cell.CoastDistance == 1) // Coastline
    {
        if (type == StateType.Naval || type == StateType.Lake)
            return 0;
        if (type == StateType.Nomadic)
            return 60;
        return 20;
    }
    
    if (cell.CoastDistance == 2) // Near coast
    {
        if (type == StateType.Naval || type == StateType.Nomadic)
            return 30;
        return 0;
    }
    
    if (cell.CoastDistance > 2) // Inland
    {
        if (type == StateType.Naval || type == StateType.Lake)
            return 100;
        return 0;
    }
    
    return 0;
}

private record ExpansionNode(int CellId, int StateId, double Cost, int NativeBiome);
```

### 3. Normalize State Borders

```csharp
private void NormalizeStates(List<State> states)
{
    // Clean up irregular borders by reassigning cells
    // that are surrounded by a different state
    
    foreach (var cell in _map.Cells.Where(c => c.Height >= 20))
    {
        if (cell.BurgId > 0) continue; // Don't touch burg cells
        if (states[cell.StateId].IsLocked) continue;
        
        // Check if capital is nearby
        bool nearCapital = cell.Neighbors.Any(nId =>
        {
            var neighbor = _map.Cells[nId];
            return neighbor.BurgId > 0 && _map.Burgs[neighbor.BurgId].IsCapital;
        });
        
        if (nearCapital) continue;
        
        // Count neighbors by state
        var neighborStates = cell.Neighbors
            .Where(nId => _map.Cells[nId].Height >= 20)
            .Select(nId => _map.Cells[nId].StateId)
            .Where(sId => !states[sId].IsLocked)
            .ToList();
        
        if (neighborStates.Count < 2) continue;
        
        var adversaries = neighborStates.Where(sId => sId != cell.StateId).ToList();
        var buddies = neighborStates.Where(sId => sId == cell.StateId).ToList();
        
        // Reassign if surrounded by different state
        if (adversaries.Count >= 2 && buddies.Count <= 2 && 
            adversaries.Count > buddies.Count)
        {
            cell.StateId = adversaries[0];
        }
    }
}
```

### 4. Generate Diplomacy

```csharp
private void GenerateDiplomacy(List<State> states)
{
    var validStates = states.Where(s => s.Id > 0).ToList();
    if (validStates.Count < 2) return;
    
    double avgArea = validStates.Average(s => s.Area);
    
    // Initialize diplomacy
    foreach (var state in validStates)
    {
        state.Diplomacy = Enumerable.Range(0, states.Count)
            .ToDictionary(i => i, i => DiplomaticStatus.Unknown);
        state.Diplomacy[state.Id] = DiplomaticStatus.Neutral; // Self
    }
    
    // Set relations
    foreach (var state in validStates)
    {
        // Neighbors
        foreach (var neighborId in state.Neighbors)
        {
            if (state.Diplomacy[neighborId] != DiplomaticStatus.Unknown)
                continue;
            
            var status = WeightedRandom(new Dictionary<DiplomaticStatus, int>
            {
                { DiplomaticStatus.Ally, 1 },
                { DiplomaticStatus.Friendly, 2 },
                { DiplomaticStatus.Neutral, 1 },
                { DiplomaticStatus.Suspicion, 10 },
                { DiplomaticStatus.Rival, 9 }
            });
            
            // Check for vassalage
            var neighbor = states[neighborId];
            if (P(0.8) && state.Area > avgArea && neighbor.Area < avgArea &&
                state.Area / neighbor.Area > 2)
            {
                status = DiplomaticStatus.Vassal;
                state.Diplomacy[neighborId] = DiplomaticStatus.Suzerain;
                neighbor.Diplomacy[state.Id] = DiplomaticStatus.Vassal;
            }
            else
            {
                state.Diplomacy[neighborId] = status;
                neighbor.Diplomacy[state.Id] = status;
            }
        }
        
        // Neighbors of neighbors
        var neighborsOfNeighbors = state.Neighbors
            .SelectMany(nId => states[nId].Neighbors)
            .Distinct()
            .Where(nId => nId != state.Id && !state.Neighbors.Contains(nId))
            .ToList();
        
        foreach (var nId in neighborsOfNeighbors)
        {
            if (state.Diplomacy[nId] != DiplomaticStatus.Unknown)
                continue;
            
            var status = WeightedRandom(new Dictionary<DiplomaticStatus, int>
            {
                { DiplomaticStatus.Ally, 10 },
                { DiplomaticStatus.Friendly, 8 },
                { DiplomaticStatus.Neutral, 5 },
                { DiplomaticStatus.Suspicion, 1 }
            });
            
            state.Diplomacy[nId] = status;
            states[nId].Diplomacy[state.Id] = status;
        }
        
        // Naval powers (different water bodies)
        if (state.Type == StateType.Naval)
        {
            foreach (var other in validStates.Where(s => 
                s.Type == StateType.Naval && s.Id != state.Id))
            {
                if (state.Diplomacy[other.Id] != DiplomaticStatus.Unknown)
                    continue;
                
                // Check if on different water bodies
                var stateFeature = _map.Features[_map.Cells[state.CenterCellId].FeatureId];
                var otherFeature = _map.Features[_map.Cells[other.CenterCellId].FeatureId];
                
                if (stateFeature.Id != otherFeature.Id)
                {
                    var status = WeightedRandom(new Dictionary<DiplomaticStatus, int>
                    {
                        { DiplomaticStatus.Neutral, 1 },
                        { DiplomaticStatus.Suspicion, 2 },
                        { DiplomaticStatus.Rival, 1 },
                        { DiplomaticStatus.Unknown, 1 }
                    });
                    
                    state.Diplomacy[other.Id] = status;
                    other.Diplomacy[state.Id] = status;
                }
            }
        }
    }
    
    // Declare wars
    DeclareWars(states);
}

private void DeclareWars(List<State> states)
{
    foreach (var attacker in states.Where(s => s.Id > 0))
    {
        // Must have rivals and not be at war already
        if (!attacker.Diplomacy.Values.Contains(DiplomaticStatus.Rival))
            continue;
        if (attacker.Diplomacy.Values.Contains(DiplomaticStatus.Enemy))
            continue;
        if (attacker.Diplomacy.Values.Contains(DiplomaticStatus.Vassal))
            continue;
        
        // Find independent rival
        var rivals = attacker.Diplomacy
            .Where(kvp => kvp.Value == DiplomaticStatus.Rival)
            .Where(kvp => !states[kvp.Key].Diplomacy.Values.Contains(DiplomaticStatus.Vassal))
            .Select(kvp => kvp.Key)
            .ToList();
        
        if (!rivals.Any()) continue;
        
        int defenderId = rivals[_random.Next(rivals.Count)];
        var defender = states[defenderId];
        
        // Check power balance
        double attackerPower = attacker.Area * attacker.Expansionism;
        double defenderPower = defender.Area * defender.Expansionism;
        
        if (attackerPower < defenderPower * Gauss(1.6, 0.8, 0, 10, 2))
            continue; // Defender too strong
        
        // Declare war
        attacker.Diplomacy[defenderId] = DiplomaticStatus.Enemy;
        defender.Diplomacy[attacker.Id] = DiplomaticStatus.Enemy;
        
        // Create campaign
        string warName = $"{attacker.Name}-{TrimVowels(defender.Name)}ian War";
        int startYear = _settings.CurrentYear - _random.Next(2, 10);
        int endYear = startYear + _random.Next(1, 5);
        
        var campaign = new Campaign
        {
            Name = warName,
            StartYear = startYear,
            EndYear = endYear,
            AttackerId = attacker.Id,
            DefenderId = defenderId
        };
        
        attacker.Campaigns.Add(campaign);
        defender.Campaigns.Add(campaign);
    }
}
```

## Implementation Steps

### Step 1: Create Models (Day 1)
- [ ] Create `State.cs` model
- [ ] Create `StateForm.cs` enum
- [ ] Create `StateType.cs` enum
- [ ] Create `DiplomaticStatus.cs` enum
- [ ] Create `Campaign.cs` model
- [ ] Add `StateId` to `Cell.cs`

### Step 2: Create Generator (Day 2-4)
- [ ] Create `StatesGenerator.cs`
- [ ] Implement `CreateStates()`
- [ ] Implement `ExpandStates()` with Dijkstra
- [ ] Implement cost calculation methods

### Step 3: Border & Statistics (Day 5-6)
- [ ] Implement `NormalizeStates()`
- [ ] Implement `GetPoles()` (pole of inaccessibility)
- [ ] Implement `CollectStatistics()`
- [ ] Implement `AssignColors()` (greedy coloring)

### Step 4: Diplomacy (Day 7-8)
- [ ] Implement `GenerateDiplomacy()`
- [ ] Implement `DeclareWars()`
- [ ] Implement `GenerateCampaigns()`

### Step 5: State Forms (Day 9)
- [ ] Implement `DefineStateForms()`
- [ ] Monarchy tier system
- [ ] Republic/Union/Theocracy selection

### Step 6: Integration (Day 10)
- [ ] Add to `MapGenerator.cs` pipeline
- [ ] Update `MapData.cs`
- [ ] Add configuration options

### Step 7: Testing (Day 11-13)
- [ ] Unit tests for state creation
- [ ] Unit tests for expansion
- [ ] Unit tests for diplomacy
- [ ] Integration tests
- [ ] Performance tests

### Step 8: Documentation (Day 14)
- [ ] Update README
- [ ] Add usage examples
- [ ] Document algorithms

## Configuration

```csharp
public class MapGenerationSettings
{
    // ... existing properties ...
    
    /// <summary>
    /// Size variety factor (affects expansionism)
    /// </summary>
    public double SizeVariety { get; set; } = 1.0;
    
    /// <summary>
    /// Growth rate multiplier
    /// </summary>
    public double GrowthRate { get; set; } = 1.0;
    
    /// <summary>
    /// Current year for campaign generation
    /// </summary>
    public int CurrentYear { get; set; } = 1000;
}
```

## Success Criteria

- [ ] States created from capitals
- [ ] Borders expand realistically
- [ ] No isolated cells (normalized borders)
- [ ] Diplomatic relations generated
- [ ] Wars declared appropriately
- [ ] State forms assigned correctly
- [ ] All tests passing
- [ ] Performance < 5 seconds for typical map

## Dependencies

**Required:**
- Burgs (capitals)
- Cultures
- Biomes
- Features (for water bodies)

**Blocks:**
- Provinces
- Routes
- Military

## Notes

- State ID 0 is neutral/wildlands
- Dijkstra expansion is performance-critical
- Diplomacy should create interesting conflicts
- Vassalage adds political complexity
- State forms should match culture and size

## References

- Original JS: `modules/burgs-and-states.js` (lines 300-887)
- Algorithm: Dijkstra shortest path with custom costs
- Diplomacy: Weighted random with neighbor relationships
