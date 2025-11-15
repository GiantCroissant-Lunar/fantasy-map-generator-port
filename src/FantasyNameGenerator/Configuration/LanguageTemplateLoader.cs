using System.Text.Json;
using FantasyNameGenerator.Phonology;

namespace FantasyNameGenerator.Configuration;

/// <summary>
/// Loads language templates from JSON files.
/// Supports embedded resources and external file system.
/// </summary>
public class LanguageTemplateLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Load a language template from a JSON file.
    /// </summary>
    public static CulturePhonology LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Language template file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Load a language template from JSON string.
    /// </summary>
    public static CulturePhonology LoadFromJson(string json)
    {
        var template = JsonSerializer.Deserialize<LanguageTemplateJson>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize language template");

        return ConvertFromJson(template);
    }

    /// <summary>
    /// Load a built-in language template by name.
    /// Looks for Templates/{name}.json in embedded resources.
    /// </summary>
    public static CulturePhonology? LoadBuiltIn(string name)
    {
        var resourceName = $"FantasyNameGenerator.Templates.{name.ToLowerInvariant()}.json";
        var assembly = typeof(LanguageTemplateLoader).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return LoadFromJson(json);
    }

    /// <summary>
    /// Get all available built-in template names.
    /// </summary>
    public static string[] GetBuiltInTemplateNames()
    {
        var assembly = typeof(LanguageTemplateLoader).Assembly;
        var prefix = "FantasyNameGenerator.Templates.";
        
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix) && name.EndsWith(".json"))
            .Select(name => name.Substring(prefix.Length, name.Length - prefix.Length - 5))
            .ToArray();
    }

    /// <summary>
    /// Convert JSON model to CulturePhonology.
    /// </summary>
    public static CulturePhonology ConvertFromJson(LanguageTemplateJson json)
    {
        var phonology = new CulturePhonology
        {
            Name = json.Name,
            Inventory = new PhonemeInventory
            {
                Consonants = json.Inventory.Consonants,
                Vowels = json.Inventory.Vowels,
                Liquids = json.Inventory.Liquids ?? string.Empty,
                Nasals = json.Inventory.Nasals ?? string.Empty,
                Fricatives = json.Inventory.Fricatives ?? string.Empty,
                Stops = json.Inventory.Stops ?? string.Empty,
                Sibilants = json.Inventory.Sibilants ?? string.Empty,
                Finals = json.Inventory.Finals ?? string.Empty
            }
        };

        // Convert weights (string keys to char)
        if (json.Weights != null)
        {
            foreach (var (key, value) in json.Weights)
            {
                if (key.Length == 1)
                    phonology.Weights[key[0]] = value;
            }
        }

        // Convert allophones
        if (json.Allophones != null)
        {
            foreach (var rule in json.Allophones)
            {
                if (rule.Phoneme.Length == 1 && rule.Allophone.Length == 1)
                {
                    phonology.Allophones.Add(new AllophoneRule
                    {
                        Phoneme = rule.Phoneme[0],
                        Allophone = rule.Allophone[0],
                        Context = rule.Context
                    });
                }
            }
        }

        // Convert orthography (string keys to char)
        if (json.Orthography != null)
        {
            foreach (var (key, value) in json.Orthography)
            {
                if (key.Length == 1)
                    phonology.Orthography[key[0]] = value;
            }
        }

        return phonology;
    }

    /// <summary>
    /// Convert CulturePhonology to JSON model.
    /// </summary>
    public static LanguageTemplateJson ConvertToJson(CulturePhonology phonology)
    {
        return new LanguageTemplateJson
        {
            Name = phonology.Name,
            Version = "1.0",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = phonology.Inventory.Consonants,
                Vowels = phonology.Inventory.Vowels,
                Liquids = phonology.Inventory.Liquids,
                Nasals = phonology.Inventory.Nasals,
                Fricatives = phonology.Inventory.Fricatives,
                Stops = phonology.Inventory.Stops,
                Sibilants = phonology.Inventory.Sibilants,
                Finals = phonology.Inventory.Finals
            },
            Weights = phonology.Weights.Count > 0 
                ? phonology.Weights.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
                : null,
            Allophones = phonology.Allophones.Count > 0
                ? phonology.Allophones.Select(r => new AllophoneRuleJson
                {
                    Phoneme = r.Phoneme.ToString(),
                    Allophone = r.Allophone.ToString(),
                    Context = r.Context
                }).ToList()
                : null,
            Orthography = phonology.Orthography.Count > 0
                ? phonology.Orthography.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
                : null
        };
    }

    /// <summary>
    /// Convert PhonotacticsJson to PhonotacticRules.
    /// </summary>
    public static Phonotactics.PhonotacticRules? ConvertPhonotacticsFromJson(PhonotacticsJson? json)
    {
        if (json == null)
            return null;

        return new Phonotactics.PhonotacticRules
        {
            AllowedStructures = json.Structures != null ? new List<string>(json.Structures) : new List<string>(),
            ForbiddenSequences = json.ForbiddenSequences != null ? new List<string>(json.ForbiddenSequences) : new List<string>(),
            AllowedOnsets = json.AllowedOnsets != null ? new List<string>(json.AllowedOnsets) : new List<string>(),
            AllowedCodas = json.AllowedCodas != null ? new List<string>(json.AllowedCodas) : new List<string>(),
            MaxConsonantCluster = json.MaxConsonantCluster ?? 3,
            MaxVowelCluster = json.MaxVowelCluster ?? 2,
            MinSyllables = json.MinSyllables ?? 1,
            MaxSyllables = json.MaxSyllables ?? 3,
            EnforceSonoritySequencing = json.EnforceSonoritySequencing ?? true
        };
    }

    /// <summary>
    /// Convert PhonotacticRules to PhonotacticsJson.
    /// </summary>
    public static PhonotacticsJson ConvertPhonotacticsToJson(Phonotactics.PhonotacticRules rules)
    {
        return new PhonotacticsJson
        {
            Structures = rules.AllowedStructures.Count > 0 ? rules.AllowedStructures.ToList() : null,
            ForbiddenSequences = rules.ForbiddenSequences.Count > 0 ? rules.ForbiddenSequences.ToList() : null,
            AllowedOnsets = rules.AllowedOnsets.Count > 0 ? rules.AllowedOnsets.ToList() : null,
            AllowedCodas = rules.AllowedCodas.Count > 0 ? rules.AllowedCodas.ToList() : null,
            MaxConsonantCluster = rules.MaxConsonantCluster,
            MaxVowelCluster = rules.MaxVowelCluster,
            MinSyllables = rules.MinSyllables,
            MaxSyllables = rules.MaxSyllables,
            EnforceSonoritySequencing = rules.EnforceSonoritySequencing
        };
    }

    /// <summary>
    /// Save a language template to JSON file.
    /// </summary>
    public static void SaveToFile(CulturePhonology phonology, string filePath)
    {
        var json = ConvertToJson(phonology);
        var jsonString = JsonSerializer.Serialize(json, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(filePath, jsonString);
    }
}
