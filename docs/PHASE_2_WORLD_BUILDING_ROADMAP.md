# Phase 2: World-Building Features - Complete Port Roadmap

## Overview

**Current Status:** Core terrain/hydrology features complete (100%)  
**Next Phase:** Port all world-building features from Azgaar's Fantasy Map Generator  
**Estimated Effort:** 12-16 weeks (3-4 months)  
**Goal:** Achieve 100% feature parity with original JavaScript project

---

## Feature Categories

### 1. Political Systems (4-5 weeks)
- Burgs & States
- Provinces
- Diplomacy & Wars
- Military & Campaigns

### 2. Cultural Systems (2-3 weeks)
- Cultures
- Religions
- Name Generation

### 3. Infrastructure (2-3 weeks)
- Routes (roads, sea routes)
- Markers (points of interest)
- Zones (special areas)

### 4. Advanced Generation (1-2 weeks)
- Heightmap Templates
- Advanced Name Generation

---

## Detailed Implementation Plan

### Week 1-2: Burgs (Settlements)

**Priority:** ⭐⭐⭐⭐⭐ (Critical - foundation for everything else)

#### Features to Implement
```csharp
// Models
public class Burg
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CellId { get; set; }
    public Point Position { get; set; }
    public int StateId { get; set; }
    public int CultureId { get; set; }
    public bool IsCapital { get; set; }
    public bool IsPort { get; set; }
    public int? PortFeatureId { get; set; }
    public double Population { get; set; }
    public BurgType Type { get; set; }
    
    // Features
    public bool HasCitadel { get; set; }
    public bool HasPlaza { get; set; }
    public bool HasWalls { get; set; }
    public bool HasShanty { get; set; }
    public bool HasTemple { get; set; }
    
    // Coat of Arms
    public CoatOfArms CoA { get; set; }
}

public enum BurgType
{
    Generic,
    Naval,      // Port city
    Lake,       // On lake shore
    Highland,   // Mountain city
    River,      // Major river crossing
    Nomadic,    // Desert/steppe settlement
    Hunting     // Forest settlement
}

// Generator
public class BurgsGenerator
{
    public void Generate()
    {
        PlaceCapitals();      // One per state
        PlaceTowns();         // Secondary settlements
        SpecifyBurgs();       // Define features
        DefineBurgFeatures(); // Citadel, walls, etc.
    }
    
    private void PlaceCapitals()
    {
        // Use cell score (population * random factor)
        // Ensure minimum spacing between capitals
        // Prefer high-population, accessible locations
    }
    
    private void PlaceTowns()
    {
        // Place based on cell score
        // Avoid clustering near capitals
        // Respect minimum spacing
        // Target: ~1 town per 5-10 cells
    }
    
    private void SpecifyBurgs()
    {
        // Detect ports (coastal burgs with harbors)
        // Calculate population
        // Shift river burgs slightly
        // Generate coat of arms
    }
}
```

**Files to Create:**
- `src/Core/Models/Burg.cs`
- `src/Core/Models/BurgType.cs`
- `src/Core/Generators/BurgsGenerator.cs`
- `tests/Core.Tests/BurgsGeneratorTests.cs`

**Reference:** `modules/burgs-and-states.js` (lines 1-300)

---

### Week 3-4: States (Political Entities)

**Priority:** ⭐⭐⭐⭐⭐ (Critical)

#### Features to Implement
```csharp
public class State
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; } // "Kingdom of Angshire"
    public SKColor Color { get; set; }
    
    // Geography
    public int CapitalBurgId { get; set; }
    public int CenterCellId { get; set; }
    public Point Pole { get; set; } // Pole of inaccessibility
    public List<int> Neighbors { get; set; }
    
    // Culture & Politics
    public int CultureId { get; set; }
    public StateForm Form { get; set; }
    public StateType Type { get; set; }
    public double Expansionism { get; set; }
    
    // Statistics
    public int CellCount { get; set; }
    public double Area { get; set; }
    public int BurgCount { get; set; }
    public double RuralPopulation { get; set; }
    public double UrbanPopulation { get; set; }
    
    // Diplomacy
    public Dictionary<int, DiplomaticStatus> Diplomacy { get; set; }
    public List<Campaign> Campaigns { get; set; }
    
    // Coat of Arms
    public CoatOfArms CoA { get; set; }
}

public enum StateForm
{
    Monarchy,           // Kingdom, Empire, Duchy, etc.
    Republic,           // Republic, Federation, etc.
    Union,              // Union, League, Confederation
    Theocracy,          // Religious state
    Anarchy             // Free territory, commune
}

public enum StateType
{
    Generic,
    Naval,
    Lake,
    Highland,
    River,
    Nomadic,
    Hunting
}

public enum DiplomaticStatus
{
    Ally,
    Friendly,
    Neutral,
    Suspicion,
    Rival,
    Enemy,
    Suzerain,  // Overlord
    Vassal,    // Subject state
    Unknown
}

public class Campaign
{
    public string Name { get; set; }
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public int AttackerId { get; set; }
    public int DefenderId { get; set; }
}

public class StatesGenerator
{
    public void Generate()
    {
        CreateStates();           // One per capital
        ExpandStates();           // Dijkstra expansion
        NormalizeStates();        // Clean borders
        GetPoles();               // Calculate poles
        CollectStatistics();      // Area, population, etc.
        AssignColors();           // Greedy coloring
        DefineStateForms();       // Monarchy, republic, etc.
        GenerateCampaigns();      // Historical wars
        GenerateDiplomacy();      // Relations between states
    }
    
    private void ExpandStates()
    {
        // Dijkstra-like expansion from capitals
        // Cost factors:
        // - Culture match: -9 (bonus)
        // - Population: 20 - population (penalty for empty)
        // - Biome: native=10, non-native=cost*2
        // - Height: mountains=2200, hills=300, water=1000
        // - River: 20-100 based on flux
        // - Type: coastline=20, mainland=100 for naval
    }
    
    private void GenerateDiplomacy()
    {
        // Neighbors: weighted relations (Ally, Rival, etc.)
        // Neighbors of neighbors: mostly neutral
        // Far states: mostly unknown
        // Naval powers: special relations
        // Vassals: copy suzerain's relations
        // Wars: declare based on power ratio
    }
}
```

**Files to Create:**
- `src/Core/Models/State.cs`
- `src/Core/Models/StateForm.cs`
- `src/Core/Models/DiplomaticStatus.cs`
- `src/Core/Models/Campaign.cs`
- `src/Core/Generators/StatesGenerator.cs`
- `tests/Core.Tests/StatesGeneratorTests.cs`

**Reference:** `modules/burgs-and-states.js` (lines 300-887)

---

### Week 5-6: Cultures

**Priority:** ⭐⭐⭐⭐⭐ (Critical - needed for states)

#### Features to Implement
```csharp
public class Culture
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; } // 3-letter abbreviation
    public SKColor Color { get; set; }
    
    // Geography
    public int CenterCellId { get; set; }
    public CultureType Type { get; set; }
    public double Expansionism { get; set; }
    
    // Language
    public int NameBaseId { get; set; }
    public string Shield { get; set; } // Heraldic shield shape
    
    // Origins
    public List<int> Origins { get; set; } // Parent cultures
    
    // Statistics
    public int CellCount { get; set; }
    public double Area { get; set; }
    public double RuralPopulation { get; set; }
    public double UrbanPopulation { get; set; }
}

public enum CultureType
{
    Generic,
    Naval,      // Coastal, seafaring
    Lake,       // Lake-dwelling
    Highland,   // Mountain-dwelling
    River,      // River-focused
    Nomadic,    // Desert/steppe nomads
    Hunting     // Forest hunters
}

public class CulturesGenerator
{
    public void Generate()
    {
        SelectCultures();         // Choose from defaults or random
        PlaceCultureCenters();    // Place origin points
        DefineCultureTypes();     // Naval, Highland, etc.
        ExpandCultures();         // Dijkstra expansion
        CollectStatistics();      // Area, population
    }
    
    private void ExpandCultures()
    {
        // Similar to state expansion but:
        // - Higher biome penalties
        // - Culture-specific costs
        // - Nomads avoid forests
        // - Highlanders prefer mountains
        // - Naval cultures cross water easily
    }
    
    private CultureType DefineCultureType(int centerCell)
    {
        var cell = _map.Cells[centerCell];
        
        // Nomadic: hot/dry biomes, avoid forests
        if (cell.Height < 70 && IsDesertOrSteppe(cell.Biome))
            return CultureType.Nomadic;
        
        // Highland: mountains
        if (cell.Height > 50)
            return CultureType.Highland;
        
        // Lake: large lake nearby
        if (HasLargeNearbyLake(cell))
            return CultureType.Lake;
        
        // Naval: coastal with good harbor
        if (cell.IsCoastal && cell.Harbor > 0)
            return CultureType.Naval;
        
        // River: major river
        if (cell.RiverId > 0 && cell.Flux > 100)
            return CultureType.River;
        
        // Hunting: forest biomes
        if (IsForestBiome(cell.Biome))
            return CultureType.Hunting;
        
        return CultureType.Generic;
    }
}
```

**Default Cultures:**
```csharp
// European set
{ "Shwazen", "Angshire", "Luari", "Tallian", "Astellian", "Slovan", 
  "Norse", "Elladan", "Romian", "Soumi", "Portuzian", "Vengrian", 
  "Turchian", "Euskati", "Keltan" }

// Oriental set
{ "Koryo", "Hantzu", "Yamoto", "Turchian", "Berberan", "Eurabic", 
  "Efratic", "Tehrani", "Maui", "Carnatic", "Vietic", "Guantzu", "Ulus" }

// High Fantasy set
{ "Quenian (Elfish)", "Eldar (Elfish)", "Trow (Dark Elfish)", 
  "Dunirr (Dwarven)", "Kobold (Goblin)", "Uruk (Orkish)", 
  "Yotunn (Giants)", "Rake (Drakonic)", "Arago (Arachnid)" }
```

**Files to Create:**
- `src/Core/Models/Culture.cs`
- `src/Core/Models/CultureType.cs`
- `src/Core/Data/DefaultCultures.cs`
- `src/Core/Generators/CulturesGenerator.cs`
- `tests/Core.Tests/CulturesGeneratorTests.cs`

**Reference:** `modules/cultures-generator.js`

---

### Week 7: Religions

**Priority:** ⭐⭐⭐⭐ (Important)

#### Features to Implement
```csharp
public class Religion
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SKColor Color { get; set; }
    
    // Geography
    public int CenterCellId { get; set; }
    public List<int> Origins { get; set; }
    
    // Type & Expansion
    public ReligionType Type { get; set; }
    public ExpansionType Expansion { get; set; }
    
    // Deities
    public string Form { get; set; } // "Pantheon", "Monotheism", etc.
    public List<Deity> Deities { get; set; }
    
    // Statistics
    public int CellCount { get; set; }
    public double Area { get; set; }
    public double RuralPopulation { get; set; }
    public double UrbanPopulation { get; set; }
}

public enum ReligionType
{
    Organized,  // Major religion with hierarchy
    Folk,       // Traditional/tribal religion
    Cult,       // Mystery cult
    Heresy      // Splinter from organized religion
}

public enum ExpansionType
{
    Global,     // Spreads everywhere
    State,      // State religion (theocracy)
    Culture,    // Cultural religion
    Homeland    // Stays in origin area
}

public class Deity
{
    public string Name { get; set; }
    public string Sphere { get; set; } // "War", "Love", "Death", etc.
}

public class ReligionsGenerator
{
    public void Generate()
    {
        PlaceReligionOrigins();   // Place centers
        DefineReligionTypes();    // Organized, Folk, etc.
        GenerateDeities();        // Create pantheons
        ExpandReligions();        // Spread across map
        CollectStatistics();      // Area, population
    }
    
    private void ExpandReligions()
    {
        // Expansion based on type:
        // - Global: spreads everywhere
        // - State: follows state borders
        // - Culture: follows culture borders
        // - Homeland: stays near origin
    }
}
```

**Files to Create:**
- `src/Core/Models/Religion.cs`
- `src/Core/Models/ReligionType.cs`
- `src/Core/Models/Deity.cs`
- `src/Core/Generators/ReligionsGenerator.cs`
- `tests/Core.Tests/ReligionsGeneratorTests.cs`

**Reference:** `modules/religions-generator.js`

---

### Week 8: Provinces

**Priority:** ⭐⭐⭐ (Medium)

#### Features to Implement
```csharp
public class Province
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int StateId { get; set; }
    public int CapitalBurgId { get; set; }
    public int CenterCellId { get; set; }
    public SKColor Color { get; set; }
    
    // Statistics
    public int CellCount { get; set; }
    public double Area { get; set; }
    public double RuralPopulation { get; set; }
    public double UrbanPopulation { get; set; }
    public int BurgCount { get; set; }
}

public class ProvincesGenerator
{
    public void Generate()
    {
        // For each state:
        // 1. Select province capitals (burgs)
        // 2. Expand provinces (Dijkstra)
        // 3. Normalize borders
        // 4. Generate names
    }
}
```

**Files to Create:**
- `src/Core/Models/Province.cs`
- `src/Core/Generators/ProvincesGenerator.cs`
- `tests/Core.Tests/ProvincesGeneratorTests.cs`

**Reference:** `modules/provinces-generator.js`

---

### Week 9: Routes (Roads & Sea Routes)

**Priority:** ⭐⭐⭐ (Medium)

#### Features to Implement
```csharp
public class Route
{
    public int Id { get; set; }
    public RouteType Type { get; set; }
    public int StartBurgId { get; set; }
    public int EndBurgId { get; set; }
    public List<int> Path { get; set; } // Cell IDs
    public double Length { get; set; }
    public int Feature { get; set; } // Water body for sea routes
}

public enum RouteType
{
    Road,
    Trail,
    SeaRoute
}

public class RoutesGenerator
{
    public void Generate()
    {
        GenerateRoads();      // Land routes between burgs
        GenerateSeaRoutes();  // Naval routes
        OptimizeRoutes();     // Remove redundant paths
    }
    
    private void GenerateRoads()
    {
        // A* pathfinding between burgs
        // Cost factors:
        // - Terrain height (mountains expensive)
        // - Rivers (bridges needed)
        // - Existing roads (prefer reuse)
    }
}
```

**Files to Create:**
- `src/Core/Models/Route.cs`
- `src/Core/Generators/RoutesGenerator.cs`
- `tests/Core.Tests/RoutesGeneratorTests.cs`

**Reference:** `modules/routes-generator.js`

---

### Week 10: Markers (Points of Interest)

**Priority:** ⭐⭐ (Nice to have)

#### Features to Implement
```csharp
public class Marker
{
    public int Id { get; set; }
    public MarkerType Type { get; set; }
    public int CellId { get; set; }
    public Point Position { get; set; }
    public string Icon { get; set; }
}

public enum MarkerType
{
    // Natural
    Volcano, HotSpring, Geyser, Waterfall, Mine,
    
    // Historical
    Ruins, Battlefield, Monument,
    
    // Religious
    SacredSite, Temple, Shrine,
    
    // Dangerous
    DangerZone, MonsterLair
}

public class MarkersGenerator
{
    public void Generate()
    {
        PlaceVolcanoes();     // Near tectonic activity
        PlaceHotSprings();    // Geothermal areas
        PlaceRuins();         // Ancient civilizations
        PlaceBattlefields();  // Historical conflicts
        PlaceSacredSites();   // Religious importance
    }
}
```

**Files to Create:**
- `src/Core/Models/Marker.cs`
- `src/Core/Generators/MarkersGenerator.cs`
- `tests/Core.Tests/MarkersGeneratorTests.cs`

**Reference:** `modules/markers-generator.js`

---

### Week 11: Military & Campaigns

**Priority:** ⭐⭐ (Nice to have)

#### Features to Implement
```csharp
public class MilitaryUnit
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int StateId { get; set; }
    public int CellId { get; set; }
    public UnitType Type { get; set; }
    public int Strength { get; set; }
}

public enum UnitType
{
    Infantry, Cavalry, Archers, Navy, Siege
}

public class MilitaryGenerator
{
    public void Generate()
    {
        PlaceGarrisons();     // In burgs
        PlaceArmies();        // Field armies
        PlaceNavies();        // Naval forces
    }
}
```

**Files to Create:**
- `src/Core/Models/MilitaryUnit.cs`
- `src/Core/Generators/MilitaryGenerator.cs`
- `tests/Core.Tests/MilitaryGeneratorTests.cs`

**Reference:** `modules/military-generator.js`

---

### Week 12: Advanced Name Generation

**Priority:** ⭐⭐⭐⭐ (Important)

#### Features to Implement
```csharp
public class Language
{
    public string Name { get; set; }
    public Dictionary<string, string> Phonemes { get; set; }
    public string Structure { get; set; } // "CVC", "CV", etc.
    public string[] Restricts { get; set; } // Forbidden patterns
    public Dictionary<char, string> Cortho { get; set; } // Consonant orthography
    public Dictionary<char, string> Vortho { get; set; } // Vowel orthography
    public int MinSyll { get; set; }
    public int MaxSyll { get; set; }
    public char Joiner { get; set; }
    public Dictionary<string, List<string>> Morphemes { get; set; }
    public Dictionary<string, List<string>> Words { get; set; }
    public List<string> Names { get; set; }
}

public class LanguageGenerator
{
    public Language MakeRandomLanguage()
    {
        // 1. Select phoneme sets
        // 2. Define syllable structure
        // 3. Set orthographic rules
        // 4. Generate morphemes
        // 5. Build vocabulary
    }
    
    public string MakeName(Language lang, string semanticKey = "")
    {
        // Generate culturally-appropriate name
        // Use morphemes for semantic meaning
        // Apply genitive/definite articles
    }
}
```

**Files to Create:**
- `src/Core/Models/Language.cs`
- `src/Core/Generators/LanguageGenerator.cs`
- `src/Core/Data/PhonemesSets.cs`
- `tests/Core.Tests/LanguageGeneratorTests.cs`

**Reference:** `ref-projects/FantasyMapGenerator/Language/`

---

### Week 13: Zones

**Priority:** ⭐⭐ (Nice to have)

#### Features to Implement
```csharp
public class Zone
{
    public int Id { get; set; }
    public ZoneType Type { get; set; }
    public List<int> Cells { get; set; }
    public string Name { get; set; }
}

public enum ZoneType
{
    DangerZone,
    ProtectedArea,
    SpecialBiome
}
```

**Reference:** `modules/zones-generator.js`

---

### Week 14: Heightmap Templates

**Priority:** ⭐⭐ (Nice to have)

#### Features to Implement
```csharp
public class HeightmapTemplate
{
    public string Name { get; set; }
    public string Template { get; set; } // "Hill 5 30\nRange 3 40\n..."
}

public class HeightmapGenerator
{
    public void FromTemplate(string templateId)
    {
        // Parse template string
        // Apply operations: Hill, Pit, Range, Trough, Strait
        // Apply modifiers: Mask, Invert, Add, Multiply, Smooth
    }
}
```

**Reference:** `modules/heightmap-generator.js`

---

### Week 15-16: Integration & Polish

#### Tasks
1. **Integration Testing**
   - Test full generation pipeline
   - Verify all systems work together
   - Performance optimization

2. **UI Updates**
   - Add controls for new features
   - Visualization for states, cultures, religions
   - Interactive editing

3. **Documentation**
   - Update README
   - API documentation
   - Usage examples

4. **Performance**
   - Profile hot paths
   - Optimize Dijkstra expansions
   - Cache calculations

---

## Implementation Order

### Phase 2A: Foundation (Weeks 1-6)
**Critical path - everything depends on these**
1. Burgs (Week 1-2)
2. States (Week 3-4)
3. Cultures (Week 5-6)

### Phase 2B: World-Building (Weeks 7-11)
**Can be done in parallel**
- Religions (Week 7)
- Provinces (Week 8)
- Routes (Week 9)
- Markers (Week 10)
- Military (Week 11)

### Phase 2C: Advanced Features (Weeks 12-14)
**Enhancement features**
- Name Generation (Week 12)
- Zones (Week 13)
- Heightmap Templates (Week 14)

### Phase 2D: Polish (Weeks 15-16)
**Final integration**
- Testing & optimization
- UI updates
- Documentation

---

## Success Criteria

### Minimum Viable (Phase 2A Complete)
- ✅ Burgs placed and named
- ✅ States generated with borders
- ✅ Cultures spread across map
- ✅ Basic diplomacy

### Full Feature Parity (Phase 2B Complete)
- ✅ All political systems
- ✅ All cultural systems
- ✅ Infrastructure (routes, markers)
- ✅ Complete world simulation

### Enhanced (Phase 2C Complete)
- ✅ Advanced name generation
- ✅ All optional features
- ✅ Template-based generation

---

## Risk Mitigation

### Technical Risks
1. **Performance** - Dijkstra expansion on large maps
   - Mitigation: Optimize with spatial indexing
   
2. **Complexity** - Many interdependent systems
   - Mitigation: Modular design, clear interfaces
   
3. **Testing** - Hard to test emergent behavior
   - Mitigation: Visual regression tests

### Schedule Risks
1. **Scope Creep** - Feature requests during development
   - Mitigation: Stick to roadmap, defer enhancements
   
2. **Dependencies** - Blocked by earlier phases
   - Mitigation: Parallel work where possible

---

## Next Steps

1. **Review this roadmap** - Confirm scope and timeline
2. **Set up project tracking** - GitHub issues/milestones
3. **Start Phase 2A** - Begin with Burgs implementation
4. **Weekly check-ins** - Track progress and adjust

---

## Estimated Completion

- **Start Date:** November 15, 2025
- **Phase 2A Complete:** December 27, 2025 (6 weeks)
- **Phase 2B Complete:** January 31, 2026 (5 weeks)
- **Phase 2C Complete:** February 21, 2026 (3 weeks)
- **Phase 2D Complete:** March 6, 2026 (2 weeks)

**Total Duration:** 16 weeks (4 months)

---

## Questions?

Ready to start? Let's begin with Week 1: Burgs implementation!
