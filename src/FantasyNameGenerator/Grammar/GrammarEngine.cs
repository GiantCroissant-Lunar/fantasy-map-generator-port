using FantasyNameGenerator.Core;
using FantasyNameGenerator.Morphology;

namespace FantasyNameGenerator.Grammar;

/// <summary>
/// Grammar engine that combines RulePacks with morphology for complex name generation.
/// </summary>
public class GrammarEngine
{
    private readonly MorphologyRules _morphology;
    private readonly Random _random;
    private readonly RulePack _rulePack;

    public GrammarEngine(MorphologyRules morphology, Random random)
    {
        _morphology = morphology;
        _random = random;
        _rulePack = new RulePack(random);
        
        InitializeDefaultRules();
    }

    /// <summary>
    /// Generate a name using a grammar template.
    /// </summary>
    public string Generate(string template)
    {
        // First expand dynamic rules
        var expanded = ExpandDynamic(template);
        
        // Then expand static grammar rules
        expanded = _rulePack.Expand(expanded);
        
        // Finally post-process
        return PostProcess(expanded);
    }

    /// <summary>
    /// Expand dynamic rules (function-based generators).
    /// </summary>
    private string ExpandDynamic(string template, int maxDepth = 10)
    {
        if (maxDepth <= 0)
            return template;

        var result = template;
        var tagStart = result.IndexOf('[');
        
        while (tagStart >= 0)
        {
            var tagEnd = result.IndexOf(']', tagStart);
            if (tagEnd < 0)
                break;

            var tag = result.Substring(tagStart + 1, tagEnd - tagStart - 1);
            
            if (_dynamicRules.TryGetValue(tag, out var generators) && generators.Count > 0)
            {
                var generator = generators[_random.Next(generators.Count)];
                var replacement = generator();
                
                result = result.Substring(0, tagStart) + replacement + result.Substring(tagEnd + 1);
                result = ExpandDynamic(result, maxDepth - 1);
            }
            
            tagStart = result.IndexOf('[', tagStart + 1);
        }

        return result;
    }

    /// <summary>
    /// Add a custom rule to the grammar (static template).
    /// </summary>
    public void AddRule(string tag, string template)
    {
        _rulePack.AddRule(tag, template);
    }

    /// <summary>
    /// Add a dynamic rule generator to the grammar.
    /// </summary>
    public void AddRule(string tag, Func<string> generator)
    {
        if (!_dynamicRules.ContainsKey(tag))
            _dynamicRules[tag] = new List<Func<string>>();
        
        _dynamicRules[tag].Add(generator);
    }

    /// <summary>
    /// Add multiple rules for a tag.
    /// </summary>
    public void AddRules(string tag, params string[] templates)
    {
        _rulePack.AddRules(tag, templates);
    }

    private readonly Dictionary<string, List<Func<string>>> _dynamicRules = new();

    /// <summary>
    /// Initialize default grammar rules for common patterns.
    /// </summary>
    private void InitializeDefaultRules()
    {
        // Basic word generation
        _rulePack.AddRules("word",
            "[syllable]",
            "[syllable][syllable]",
            "[syllable][syllable][syllable]"
        );

        // Compound patterns
        _rulePack.AddRules("compound",
            "[word][word]",
            "[word]-[word]",
            "[word] [word]"
        );

        // Affixed patterns
        _rulePack.AddRules("affixed",
            "[prefix][word]",
            "[word][suffix]",
            "[prefix][word][suffix]"
        );

        // Common name patterns
        _rulePack.AddRules("name",
            "[word]",
            "[compound]",
            "[affixed]"
        );
    }

    /// <summary>
    /// Post-process the generated name (capitalization, cleanup).
    /// </summary>
    private string PostProcess(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Capitalize first letter
        name = char.ToUpper(name[0]) + name.Substring(1);

        // Clean up multiple spaces
        while (name.Contains("  "))
            name = name.Replace("  ", " ");

        return name.Trim();
    }

    /// <summary>
    /// Get the underlying rule pack for advanced customization.
    /// </summary>
    public RulePack RulePack => _rulePack;
}
