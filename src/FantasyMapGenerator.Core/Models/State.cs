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
    /// State color for map display (hex format #RRGGBB)
    /// </summary>
    public string Color { get; set; } = "#808080";
    
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
    public CoatOfArms? CoA { get; set; }
    
    /// <summary>
    /// True if state is locked (won't be modified)
    /// </summary>
    public bool IsLocked { get; set; }
    
    // Legacy properties for backward compatibility
    public int Capital { get => CapitalBurgId; set => CapitalBurgId = value; }
    public int Culture { get => CultureId; set => CultureId = value; }
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

// Legacy enums for backward compatibility
public enum GovernmentType
{
    Monarchy,
    Republic,
    Theocracy,
    Federation,
    Tribe,
    Democracy,
    Oligarchy,
    Empire
}

public enum DiplomaticRelation
{
    Unknown,
    Ally,
    Enemy,
    Vassal,
    Suzerain,
    Neutral,
    Friendly,
    Hostile
}
