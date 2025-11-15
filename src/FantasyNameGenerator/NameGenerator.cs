using FantasyNameGenerator.Core;
using FantasyNameGenerator.Grammar;
using FantasyNameGenerator.Morphology;
using FantasyNameGenerator.NameTypes;
using FantasyNameGenerator.Phonology;
using FantasyNameGenerator.Phonotactics;

namespace FantasyNameGenerator;

/// <summary>
/// High-level API for generating names of various types.
/// Combines all layers: syllables, phonology, phonotactics, morphology, and grammar.
/// </summary>
public class NameGenerator
{
    private readonly SyllableGenerator _syllableGen;
    private readonly CulturePhonology _phonology;
    private readonly PhonotacticRules _phonotactics;
    private readonly MorphologyRules _morphology;
    private readonly GrammarEngine _grammar;
    private readonly Random _random;
    private readonly HashSet<string> _usedNames = new();

    public NameGenerator(
        CulturePhonology phonology,
        PhonotacticRules phonotactics,
        MorphologyRules morphology,
        Random random)
    {
        _phonology = phonology;
        _phonotactics = phonotactics;
        _morphology = morphology;
        _random = random;

        // Initialize syllable generator (using random seed)
        _syllableGen = new SyllableGenerator(random.Next());

        // Initialize grammar engine
        _grammar = new GrammarEngine(morphology, random);
        
        // Register syllable generator with grammar
        RegisterGrammarRules();
    }

    /// <summary>
    /// Generate a name of the specified type.
    /// </summary>
    public string Generate(NameType type, int maxAttempts = 100)
    {
        var templates = NameTypeTemplates.GetTemplates(type);
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Pick a random template
            var template = templates[_random.Next(templates.Length)];
            
            // Generate name from template
            var name = _grammar.Generate(template);
            
            // Validate
            if (!IsValid(name))
                continue;
            
            // Check uniqueness
            if (_usedNames.Contains(name.ToLowerInvariant()))
                continue;
            
            _usedNames.Add(name.ToLowerInvariant());
            return name;
        }

        // Fallback: generate simple name
        return GenerateSimpleName();
    }

    /// <summary>
    /// Generate a name using a custom template.
    /// </summary>
    public string GenerateFromTemplate(string template, int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var name = _grammar.Generate(template);
            
            if (!IsValid(name))
                continue;
            
            if (_usedNames.Contains(name.ToLowerInvariant()))
                continue;
            
            _usedNames.Add(name.ToLowerInvariant());
            return name;
        }

        return GenerateSimpleName();
    }

    /// <summary>
    /// Generate a simple word (1-3 syllables).
    /// </summary>
    public string GenerateWord(int minSyllables = 1, int maxSyllables = 3)
    {
        var syllableCount = _random.Next(minSyllables, maxSyllables + 1);
        
        // Build word from syllables
        var word = new System.Text.StringBuilder();
        var phonemeMap = BuildPhonemeMap();
        var structure = _phonotactics.GetRandomStructure(_random);
        
        for (int i = 0; i < syllableCount; i++)
        {
            word.Append(_syllableGen.GenerateSyllable(structure, phonemeMap));
        }
        
        var result = word.ToString();
        
        // Apply orthography
        result = _phonology.ApplyOrthography(result);
        
        // Capitalize
        if (result.Length > 0)
            result = char.ToUpper(result[0]) + result.Substring(1);
        
        return result;
    }

    /// <summary>
    /// Build phoneme map from phonology.
    /// </summary>
    private Dictionary<char, string> BuildPhonemeMap()
    {
        return new Dictionary<char, string>
        {
            ['C'] = _phonology.Inventory.Consonants,
            ['V'] = _phonology.Inventory.Vowels,
            ['L'] = _phonology.Inventory.Liquids,
            ['S'] = _phonology.Inventory.Sibilants,
            ['F'] = _phonology.Inventory.Finals
        };
    }

    /// <summary>
    /// Generate a compound name (two words joined).
    /// </summary>
    public string GenerateCompound(string separator = "")
    {
        var word1 = GenerateWord(1, 2);
        var word2 = GenerateWord(1, 2);
        
        return word1 + separator + word2;
    }

    /// <summary>
    /// Add a custom grammar rule.
    /// </summary>
    public void AddGrammarRule(string tag, string template)
    {
        _grammar.AddRule(tag, template);
    }

    /// <summary>
    /// Clear the used names cache.
    /// </summary>
    public void ClearUsedNames()
    {
        _usedNames.Clear();
    }

    /// <summary>
    /// Check if a name has been used.
    /// </summary>
    public bool IsNameUsed(string name)
    {
        return _usedNames.Contains(name.ToLowerInvariant());
    }

    /// <summary>
    /// Register grammar rules that use the syllable generator.
    /// </summary>
    private void RegisterGrammarRules()
    {
        // Register [syllable] tag
        _grammar.AddRule("syllable", () =>
        {
            var phonemeMap = BuildPhonemeMap();
            var structure = _phonotactics.GetRandomStructure(_random);
            var syllable = _syllableGen.GenerateSyllable(structure, phonemeMap);
            return _phonology.ApplyOrthography(syllable);
        });
        
        // Register [word] tag (1-3 syllables)
        _grammar.AddRule("word", () => GenerateWord(1, 3));
        
        // Register [prefix] and [suffix] tags
        _grammar.AddRule("prefix", () =>
        {
            if (_morphology.Prefixes.Count == 0)
                return "";
            return _morphology.Prefixes[_random.Next(_morphology.Prefixes.Count)].Form;
        });
        
        _grammar.AddRule("suffix", () =>
        {
            if (_morphology.Suffixes.Count == 0)
                return "";
            return _morphology.Suffixes[_random.Next(_morphology.Suffixes.Count)].Form;
        });
    }

    /// <summary>
    /// Validate a generated name.
    /// </summary>
    private bool IsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        // Length check
        if (name.Length < 3 || name.Length > 30)
            return false;
        
        // Must start with a letter
        if (!char.IsLetter(name[0]))
            return false;
        
        return true;
    }

    /// <summary>
    /// Generate a simple fallback name.
    /// </summary>
    private string GenerateSimpleName()
    {
        return GenerateWord(2, 3);
    }

    /// <summary>
    /// Get access to the underlying grammar engine for advanced customization.
    /// </summary>
    public GrammarEngine Grammar => _grammar;

    /// <summary>
    /// Get access to the morphology rules.
    /// </summary>
    public MorphologyRules Morphology => _morphology;
}
