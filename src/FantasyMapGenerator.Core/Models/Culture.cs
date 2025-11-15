namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a cultural group
/// </summary>
public class Culture
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Base { get; set; } // Base culture name
    public int State { get; set; } = -1; // Primary state

    // Territory
    public List<int> Cells { get; set; } = new();
    public List<int> Burgs { get; set; } = new();
    public double Area { get; set; }
    public int Population { get; set; }

    // Characteristics
    public CultureType Type { get; set; }
    public CultureFamily Family { get; set; }
    public string? Race { get; set; }

    // Names
    public Dictionary<string, string> Names { get; set; } = new(); // Name templates
    public string? MaleNames { get; set; }
    public string? FemaleNames { get; set; }
    public string? BurgNames { get; set; }
    public string? StateNames { get; set; }

    // Religion
    public int Religion { get; set; } = -1;
    public double Religiosity { get; set; } // 0-1 religious fervor

    // Social
    public SocialStructure Social { get; set; }
    public EconomyType Economy { get; set; }
    public TechnologyLevel Technology { get; set; }

    // Military
    public MilitaryTradition Military { get; set; }
    public string? Units { get; set; } // Unit types

    // Visual
    public string Color { get; set; } = "#000000";
    public string? Emblem { get; set; }

    // Expansion
    public double Expansionism { get; set; } // 0-1 expansion tendency
    public int Assimilation { get; set; } // 0-100 assimilation rate

    public Culture(int id)
    {
        Id = id;
    }
}

public enum CultureType
{
    Generic,
    Nomadic,
    Highland,
    Naval,
    Forest,
    Desert,
    Arctic,
    Tropical,
    Civilized,
    Barbarian
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
