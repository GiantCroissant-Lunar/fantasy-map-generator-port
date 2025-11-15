namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a cultural group with distinct characteristics and naming
/// </summary>
public class Culture
{
    /// <summary>
    /// Unique identifier (0 = wildlands/no culture)
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Culture name (e.g., "Angshire", "Norse", "Eldar")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 3-letter abbreviation code
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Culture color for map display
    /// </summary>
    public string Color { get; set; } = "#808080";
    
    // Geography
    
    /// <summary>
    /// Origin cell where culture started
    /// </summary>
    public int CenterCellId { get; set; }
    
    /// <summary>
    /// Culture type based on geography
    /// </summary>
    public CultureType Type { get; set; }
    
    /// <summary>
    /// Expansionism factor (0.5-2.0, affects spread rate)
    /// </summary>
    public double Expansionism { get; set; }
    
    // Language & Identity
    
    /// <summary>
    /// Name base ID for linguistic patterns
    /// </summary>
    public int NameBaseId { get; set; }
    
    /// <summary>
    /// Heraldic shield shape (heater, wedged, round, etc.)
    /// </summary>
    public string Shield { get; set; } = "heater";
    
    /// <summary>
    /// Parent culture IDs (for cultural evolution)
    /// </summary>
    public List<int> Origins { get; set; } = new();
    
    // Statistics
    
    /// <summary>
    /// Number of cells with this culture
    /// </summary>
    public int CellCount { get; set; }
    
    /// <summary>
    /// Total area in square kilometers
    /// </summary>
    public double Area { get; set; }
    
    /// <summary>
    /// Rural population (thousands)
    /// </summary>
    public double RuralPopulation { get; set; }
    
    /// <summary>
    /// Urban population (thousands)
    /// </summary>
    public double UrbanPopulation { get; set; }
    
    /// <summary>
    /// True if culture is locked (won't be modified)
    /// </summary>
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// True if culture has been removed
    /// </summary>
    public bool IsRemoved { get; set; }
}

/// <summary>
/// Culture type based on geography and lifestyle
/// </summary>
public enum CultureType
{
    /// <summary>
    /// Generic inland culture
    /// </summary>
    Generic,
    
    /// <summary>
    /// Seafaring coastal culture (low water crossing penalty)
    /// </summary>
    Naval,
    
    /// <summary>
    /// Lake-dwelling culture (lake crossing bonus)
    /// </summary>
    Lake,
    
    /// <summary>
    /// Mountain-dwelling culture (highland bonus)
    /// </summary>
    Highland,
    
    /// <summary>
    /// River-focused culture (river bonus)
    /// </summary>
    River,
    
    /// <summary>
    /// Desert/steppe nomadic culture (avoid forests)
    /// </summary>
    Nomadic,
    
    /// <summary>
    /// Forest hunting culture (forest bonus)
    /// </summary>
    Hunting
}

/// <summary>
/// Default culture definition
/// </summary>
public class DefaultCulture
{
    public string Name { get; set; } = string.Empty;
    public int NameBaseId { get; set; }
    public double Probability { get; set; } = 1.0;
    public string Shield { get; set; } = "heater";
}

public enum CultureFamily
{
    Default,
    European,
    Asian,
    African,
    American,
    Oceanian,
    MiddleEastern,
    Indian,
    EastAsian,
    SoutheastAsian
}

public enum SocialStructure
{
    Tribal,
    Clan,
    Feudal,
    Caste,
    Class,
    Egalitarian
}

public enum EconomyType
{
    HunterGatherer,
    Pastoral,
    Agricultural,
    Trade,
    Industrial,
    Mixed
}

public enum TechnologyLevel
{
    Stone,
    Bronze,
    Iron,
    Medieval,
    Renaissance,
    EarlyModern
}

public enum MilitaryTradition
{
    None,
    Infantry,
    Cavalry,
    Archers,
    Naval,
    Fortified,
    Mobile,
    Guerilla
}
