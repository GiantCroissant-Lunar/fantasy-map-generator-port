namespace FantasyMapGenerator.Core.Data;

using FantasyMapGenerator.Core.Models;

/// <summary>
/// Default culture sets for different themes
/// </summary>
public static class DefaultCultures
{
    public static List<DefaultCulture> GetCultureSet(string setName)
    {
        return setName.ToLowerInvariant() switch
        {
            "european" => GetEuropean(),
            "oriental" => GetOriental(),
            "highfantasy" => GetHighFantasy(),
            "darkfantasy" => GetDarkFantasy(),
            _ => GetEuropean()
        };
    }

    public static List<DefaultCulture> GetEuropean() => new()
    {
        new() { Name = "Shwazen", NameBaseId = 0, Shield = "swiss" },
        new() { Name = "Angshire", NameBaseId = 1, Shield = "wedged" },
        new() { Name = "Luari", NameBaseId = 2, Shield = "french" },
        new() { Name = "Tallian", NameBaseId = 3, Shield = "horsehead" },
        new() { Name = "Astellian", NameBaseId = 4, Shield = "spanish" },
        new() { Name = "Slovan", NameBaseId = 5, Shield = "polish" },
        new() { Name = "Norse", NameBaseId = 6, Shield = "heater" },
        new() { Name = "Elladan", NameBaseId = 7, Shield = "boeotian" },
        new() { Name = "Romian", NameBaseId = 8, Shield = "roman" },
        new() { Name = "Soumi", NameBaseId = 9, Shield = "pavise" },
        new() { Name = "Portuzian", NameBaseId = 13, Shield = "renaissance" },
        new() { Name = "Vengrian", NameBaseId = 15, Shield = "horsehead2" },
        new() { Name = "Turchian", NameBaseId = 16, Probability = 0.05, Shield = "round" },
        new() { Name = "Euskati", NameBaseId = 20, Probability = 0.05, Shield = "oldFrench" },
        new() { Name = "Keltan", NameBaseId = 22, Probability = 0.05, Shield = "oval" }
    };

    public static List<DefaultCulture> GetOriental() => new()
    {
        new() { Name = "Koryo", NameBaseId = 10, Shield = "round" },
        new() { Name = "Hantzu", NameBaseId = 11, Shield = "banner" },
        new() { Name = "Yamoto", NameBaseId = 12, Shield = "round" },
        new() { Name = "Turchian", NameBaseId = 16, Shield = "round" },
        new() { Name = "Berberan", NameBaseId = 17, Probability = 0.2, Shield = "oval" },
        new() { Name = "Eurabic", NameBaseId = 18, Shield = "oval" },
        new() { Name = "Efratic", NameBaseId = 23, Probability = 0.1, Shield = "round" },
        new() { Name = "Tehrani", NameBaseId = 24, Shield = "round" },
        new() { Name = "Maui", NameBaseId = 25, Probability = 0.2, Shield = "vesicaPiscis" },
        new() { Name = "Carnatic", NameBaseId = 26, Probability = 0.5, Shield = "round" },
        new() { Name = "Vietic", NameBaseId = 29, Probability = 0.8, Shield = "banner" },
        new() { Name = "Guantzu", NameBaseId = 30, Probability = 0.5, Shield = "banner" },
        new() { Name = "Ulus", NameBaseId = 31, Shield = "banner" }
    };

    public static List<DefaultCulture> GetHighFantasy() => new()
    {
        new() { Name = "Quenian (Elfish)", NameBaseId = 33, Shield = "gondor" },
        new() { Name = "Eldar (Elfish)", NameBaseId = 33, Shield = "noldor" },
        new() { Name = "Trow (Dark Elfish)", NameBaseId = 34, Probability = 0.9, Shield = "hessen" },
        new() { Name = "Lothian (Dark Elfish)", NameBaseId = 34, Probability = 0.3, Shield = "wedged" },
        new() { Name = "Dunirr (Dwarven)", NameBaseId = 35, Shield = "ironHills" },
        new() { Name = "Khazadur (Dwarven)", NameBaseId = 35, Shield = "erebor" },
        new() { Name = "Kobold (Goblin)", NameBaseId = 36, Shield = "moriaOrc" },
        new() { Name = "Uruk (Orkish)", NameBaseId = 37, Shield = "urukHai" },
        new() { Name = "Ugluk (Orkish)", NameBaseId = 37, Probability = 0.5, Shield = "moriaOrc" },
        new() { Name = "Yotunn (Giants)", NameBaseId = 38, Probability = 0.7, Shield = "pavise" },
        new() { Name = "Rake (Drakonic)", NameBaseId = 39, Probability = 0.7, Shield = "fantasy2" },
        new() { Name = "Arago (Arachnid)", NameBaseId = 40, Probability = 0.7, Shield = "horsehead2" },
        new() { Name = "Aj'Snaga (Serpents)", NameBaseId = 41, Probability = 0.7, Shield = "fantasy1" },
        new() { Name = "Anor (Human)", NameBaseId = 32, Shield = "fantasy5" },
        new() { Name = "Dail (Human)", NameBaseId = 32, Shield = "roman" },
        new() { Name = "Rohand (Human)", NameBaseId = 16, Shield = "round" },
        new() { Name = "Dulandir (Human)", NameBaseId = 31, Shield = "easterling" }
    };

    public static List<DefaultCulture> GetDarkFantasy() => new()
    {
        new() { Name = "Bloodborn", NameBaseId = 42, Shield = "fantasy1" },
        new() { Name = "Shadowkin", NameBaseId = 43, Shield = "fantasy2" },
        new() { Name = "Voidwalkers", NameBaseId = 44, Shield = "fantasy3" },
        new() { Name = "Deathlords", NameBaseId = 45, Shield = "fantasy4" },
        new() { Name = "Cursed", NameBaseId = 46, Shield = "fantasy5" },
        new() { Name = "Forsaken", NameBaseId = 47, Probability = 0.8, Shield = "hessen" },
        new() { Name = "Corrupted", NameBaseId = 48, Probability = 0.6, Shield = "wedged" }
    };
}
