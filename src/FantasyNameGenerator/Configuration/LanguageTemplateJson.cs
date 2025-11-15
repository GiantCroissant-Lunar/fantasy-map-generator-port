using System.Text.Json.Serialization;

namespace FantasyNameGenerator.Configuration;

/// <summary>
/// JSON representation of a language template.
/// Serializable format for language phonology definitions.
/// </summary>
public class LanguageTemplateJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("inventory")]
    public PhonemeInventoryJson Inventory { get; set; } = new();

    [JsonPropertyName("weights")]
    public Dictionary<string, double>? Weights { get; set; }

    [JsonPropertyName("allophones")]
    public List<AllophoneRuleJson>? Allophones { get; set; }

    [JsonPropertyName("orthography")]
    public Dictionary<string, string>? Orthography { get; set; }

    [JsonPropertyName("phonotactics")]
    public PhonotacticsJson? Phonotactics { get; set; }
}

/// <summary>
/// JSON representation of a phoneme inventory.
/// </summary>
public class PhonemeInventoryJson
{
    [JsonPropertyName("consonants")]
    public string Consonants { get; set; } = string.Empty;

    [JsonPropertyName("vowels")]
    public string Vowels { get; set; } = string.Empty;

    [JsonPropertyName("liquids")]
    public string? Liquids { get; set; }

    [JsonPropertyName("nasals")]
    public string? Nasals { get; set; }

    [JsonPropertyName("fricatives")]
    public string? Fricatives { get; set; }

    [JsonPropertyName("stops")]
    public string? Stops { get; set; }

    [JsonPropertyName("sibilants")]
    public string? Sibilants { get; set; }

    [JsonPropertyName("finals")]
    public string? Finals { get; set; }
}

/// <summary>
/// JSON representation of an allophone rule.
/// </summary>
public class AllophoneRuleJson
{
    [JsonPropertyName("phoneme")]
    public string Phoneme { get; set; } = string.Empty;

    [JsonPropertyName("allophone")]
    public string Allophone { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// JSON representation of phonotactic rules.
/// </summary>
public class PhonotacticsJson
{
    [JsonPropertyName("structures")]
    public List<string>? Structures { get; set; }

    [JsonPropertyName("forbiddenSequences")]
    public List<string>? ForbiddenSequences { get; set; }

    [JsonPropertyName("allowedOnsets")]
    public List<string>? AllowedOnsets { get; set; }

    [JsonPropertyName("allowedCodas")]
    public List<string>? AllowedCodas { get; set; }

    [JsonPropertyName("maxConsonantCluster")]
    public int? MaxConsonantCluster { get; set; }

    [JsonPropertyName("maxVowelCluster")]
    public int? MaxVowelCluster { get; set; }

    [JsonPropertyName("minSyllables")]
    public int? MinSyllables { get; set; }

    [JsonPropertyName("maxSyllables")]
    public int? MaxSyllables { get; set; }

    [JsonPropertyName("enforceSonoritySequencing")]
    public bool? EnforceSonoritySequencing { get; set; }
}
