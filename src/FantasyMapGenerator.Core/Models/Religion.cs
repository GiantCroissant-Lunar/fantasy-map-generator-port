namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a belief system or religion
/// </summary>
public class Religion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#808080";
    
    // Geography
    public int CenterCellId { get; set; }
    public List<int> Origins { get; set; } = new();
    
    // Type & Characteristics
    public ReligionType Type { get; set; }
    public ExpansionType Expansion { get; set; }
    public string Form { get; set; } = "Pantheon";
    public List<Deity> Deities { get; set; } = new();
    
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
    State,      // State religion
    Culture,    // Cultural religion
    Homeland    // Stays in origin area
}

public class Deity
{
    public string Name { get; set; } = string.Empty;
    public string Sphere { get; set; } = string.Empty;
}
