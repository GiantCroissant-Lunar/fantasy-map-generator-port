namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a political state or kingdom
/// </summary>
public class State
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FormalName { get; set; }
    public int Capital { get; set; } = -1; // Burg ID of capital
    public int Culture { get; set; } = -1;
    public int Religion { get; set; } = -1;

    // Territory
    public List<int> Provinces { get; set; } = new();
    public List<int> Burgs { get; set; } = new();
    public double Area { get; set; }
    public int Population { get; set; }

    // Government
    public GovernmentType Government { get; set; }
    public string? Title { get; set; } // Ruler title (King, Emperor, etc.)
    public string? Ruler { get; set; } // Ruler name
    public string? Dynasty { get; set; }

    // Military
    public int Military { get; set; }
    public int Army { get; set; }
    public int Navy { get; set; }

    // Economy
    public int Wealth { get; set; }
    public int Trade { get; set; }
    public int Tax { get; set; }

    // Diplomacy
    public Dictionary<int, DiplomaticRelation> Diplomacy { get; set; } = new();
    public List<int> Allies { get; set; } = new();
    public List<int> Enemies { get; set; } = new();
    public List<int> Vassals { get; set; } = new();
    public int Suzerain { get; set; } = -1;

    // Visual
    public string Color { get; set; } = "#000000";
    public string? Emblem { get; set; }

    // Expansion
    public int Expansionism { get; set; } // 0-100 expansion tendency
    public DateTime Founded { get; set; }
    public int Age => (DateTime.Now - Founded).Days / 365;

    public State(int id)
    {
        Id = id;
        // Default founding date - should be set by MapGenerator with deterministic RNG
        Founded = DateTime.Now.AddYears(-100);
    }
}

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
