namespace FantasyNameGenerator.NameTypes;

/// <summary>
/// Predefined grammar templates for different name types.
/// </summary>
public static class NameTypeTemplates
{
    /// <summary>
    /// Get grammar templates for a specific name type.
    /// </summary>
    public static string[] GetTemplates(NameType type)
    {
        return type switch
        {
            NameType.Burg => BurgTemplates,
            NameType.State => StateTemplates,
            NameType.Province => ProvinceTemplates,
            NameType.Religion => ReligionTemplates,
            NameType.Culture => CultureTemplates,
            NameType.Person => PersonTemplates,
            NameType.River => RiverTemplates,
            NameType.Mountain => MountainTemplates,
            NameType.Region => RegionTemplates,
            NameType.Forest => ForestTemplates,
            NameType.Lake => LakeTemplates,
            _ => new[] { "[word]" }
        };
    }

    // Settlement names: simple, compound, or with suffixes
    private static readonly string[] BurgTemplates = new[]
    {
        "[word]",
        "[word]burg",
        "[word]ton",
        "[word]ham",
        "[word]ford",
        "[word]port",
        "[word]haven",
        "[word]stead",
        "[word][word]",
        "[word]-[word]",
        "[word] [word]"
    };

    // State names: kingdoms, empires, republics
    private static readonly string[] StateTemplates = new[]
    {
        "[word]",
        "[word]land",
        "[word]ia",
        "Kingdom of [word]",
        "Empire of [word]",
        "Republic of [word]",
        "[word] Kingdom",
        "[word] Empire",
        "The [word] Realm",
        "[word][word]"
    };

    // Province names: regions, territories
    private static readonly string[] ProvinceTemplates = new[]
    {
        "[word]",
        "[word]shire",
        "[word]mark",
        "[word]land",
        "[word] Province",
        "[word] Territory",
        "The [word] Marches",
        "[word][word]"
    };

    // Religion names: faiths, churches, cults
    private static readonly string[] ReligionTemplates = new[]
    {
        "[word]ism",
        "Church of [word]",
        "Faith of [word]",
        "Cult of [word]",
        "The [word] Order",
        "[word] Brotherhood",
        "[word] Covenant",
        "Followers of [word]"
    };

    // Culture names: ethnic groups, peoples
    private static readonly string[] CultureTemplates = new[]
    {
        "[word]",
        "[word]ish",
        "[word]ian",
        "[word]ese",
        "The [word] People",
        "[word] Folk",
        "[word][word]"
    };

    // Person names: simple or compound
    private static readonly string[] PersonTemplates = new[]
    {
        "[word]",
        "[word][word]",
        "[word]-[word]"
    };

    // River names: flowing, water-related
    private static readonly string[] RiverTemplates = new[]
    {
        "[word]",
        "[word] River",
        "River [word]",
        "[word]water",
        "[word]flow",
        "[word][word]"
    };

    // Mountain names: peaks, ranges
    private static readonly string[] MountainTemplates = new[]
    {
        "[word]",
        "Mount [word]",
        "[word] Peak",
        "[word] Mountain",
        "[word]horn",
        "[word]crag",
        "[word][word]"
    };

    // Region names: geographic areas
    private static readonly string[] RegionTemplates = new[]
    {
        "[word]",
        "The [word]",
        "[word]lands",
        "[word] Wastes",
        "[word] Plains",
        "[word] Highlands",
        "[word][word]"
    };

    // Forest names: woods, groves
    private static readonly string[] ForestTemplates = new[]
    {
        "[word]",
        "[word] Forest",
        "[word]wood",
        "[word] Woods",
        "The [word] Grove",
        "[word][word]"
    };

    // Lake names: bodies of water
    private static readonly string[] LakeTemplates = new[]
    {
        "[word]",
        "Lake [word]",
        "[word] Lake",
        "[word]mere",
        "[word]water",
        "[word][word]"
    };
}
