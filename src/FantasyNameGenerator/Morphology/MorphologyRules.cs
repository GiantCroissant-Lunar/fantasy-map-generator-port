using FantasyNameGenerator.Core;

namespace FantasyNameGenerator.Morphology;

/// <summary>
/// Defines morphological rules for word formation.
/// Handles morphemes, affixes, compounding, and morphophonemic changes.
/// </summary>
public class MorphologyRules
{
    private readonly Random _random;

    /// <summary>
    /// Morpheme database - maps semantic keys to phonetic forms
    /// Example: "water" → ["aqu", "hyd", "wat"]
    /// </summary>
    public Dictionary<string, List<string>> Morphemes { get; set; } = new();

    /// <summary>
    /// Prefix affixes
    /// </summary>
    public List<Affix> Prefixes { get; set; } = new();

    /// <summary>
    /// Suffix affixes
    /// </summary>
    public List<Affix> Suffixes { get; set; } = new();

    /// <summary>
    /// Infix affixes (rare)
    /// </summary>
    public List<Affix> Infixes { get; set; } = new();

    /// <summary>
    /// Compounding rules
    /// </summary>
    public CompoundingRules Compounding { get; set; } = new();

    /// <summary>
    /// Morphophonemic rules (sound changes at boundaries)
    /// </summary>
    public List<MorphophonemicRule> MorphophonemicRules { get; set; } = new();

    public MorphologyRules(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Get or create a morpheme for a semantic key
    /// </summary>
    public string GetOrCreateMorpheme(
        string semanticKey, 
        SyllableGenerator syllableGenerator,
        Dictionary<char, string> phonemeMap,
        string pattern = "CV")
    {
        if (!Morphemes.ContainsKey(semanticKey))
            Morphemes[semanticKey] = new List<string>();

        var list = Morphemes[semanticKey];
        int extras = string.IsNullOrEmpty(semanticKey) ? 10 : 1;

        // Decide whether to reuse or create new
        int n = _random.Next(list.Count + extras);

        if (n < list.Count)
        {
            // Return existing morpheme
            return list[n];
        }

        // Generate new morpheme
        int attempts = 0;
        while (attempts < 100)
        {
            var morpheme = syllableGenerator.GenerateSyllable(pattern, phonemeMap);

            // Check if already used
            bool alreadyUsed = Morphemes.Values.Any(morphList => morphList.Contains(morpheme));

            if (!alreadyUsed)
            {
                list.Add(morpheme);
                return morpheme;
            }

            attempts++;
        }

        // Fallback: return a syllable even if duplicate
        var fallback = syllableGenerator.GenerateSyllable(pattern, phonemeMap);
        list.Add(fallback);
        return fallback;
    }

    /// <summary>
    /// Apply affixes to a word
    /// </summary>
    public string ApplyAffixes(string word)
    {
        var result = word;

        // Apply prefix
        foreach (var prefix in Prefixes)
        {
            if (_random.NextDouble() < prefix.Frequency)
            {
                result = prefix.Form + result;
                break; // Only one prefix
            }
        }

        // Apply suffix
        foreach (var suffix in Suffixes)
        {
            if (_random.NextDouble() < suffix.Frequency)
            {
                result = result + suffix.Form;
                break; // Only one suffix
            }
        }

        return result;
    }

    /// <summary>
    /// Create a compound word
    /// </summary>
    public string CreateCompound(List<string> words)
    {
        if (words.Count == 0)
            return string.Empty;

        if (words.Count == 1)
            return words[0];

        // Limit to max compound length
        var limitedWords = words.Take(Compounding.MaxCompoundLength).ToList();

        // Order based on head-first/head-final
        if (!Compounding.HeadFirst)
        {
            limitedWords.Reverse();
        }

        // Join with joiner
        var compound = string.Join(Compounding.Joiner, limitedWords);

        // Apply morphophonemic rules
        foreach (var rule in MorphophonemicRules)
        {
            compound = rule.Apply(compound);
        }

        return compound;
    }

    /// <summary>
    /// Check if should create a compound
    /// </summary>
    public bool ShouldCompound()
    {
        return _random.NextDouble() < Compounding.CompoundProbability;
    }

    /// <summary>
    /// Clone these rules
    /// </summary>
    public MorphologyRules Clone(int newSeed)
    {
        return new MorphologyRules(newSeed)
        {
            Morphemes = new Dictionary<string, List<string>>(
                Morphemes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new List<string>(kvp.Value)
                )
            ),
            Prefixes = new List<Affix>(Prefixes),
            Suffixes = new List<Affix>(Suffixes),
            Infixes = new List<Affix>(Infixes),
            Compounding = Compounding.Clone(),
            MorphophonemicRules = new List<MorphophonemicRule>(MorphophonemicRules)
        };
    }
}
