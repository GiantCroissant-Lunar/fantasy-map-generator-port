namespace FantasyNameGenerator.Grammar;

/// <summary>
/// RimWorld-style rule pack for grammar-based name generation.
/// Supports recursive rule expansion with tags like [word], [adjective], etc.
/// </summary>
public class RulePack
{
    private readonly Dictionary<string, List<string>> _rules = new();
    private readonly Random _random;

    public RulePack(Random random)
    {
        _random = random;
    }

    /// <summary>
    /// Add a rule to the pack. Rules can reference other rules using [tag] syntax.
    /// </summary>
    public void AddRule(string tag, string template)
    {
        if (!_rules.ContainsKey(tag))
            _rules[tag] = new List<string>();
        
        _rules[tag].Add(template);
    }

    /// <summary>
    /// Add multiple rules for the same tag.
    /// </summary>
    public void AddRules(string tag, params string[] templates)
    {
        foreach (var template in templates)
            AddRule(tag, template);
    }

    /// <summary>
    /// Expand a template by recursively replacing [tag] references.
    /// </summary>
    public string Expand(string template, int maxDepth = 10)
    {
        if (maxDepth <= 0)
            return template; // Prevent infinite recursion

        var result = template;
        var tagStart = result.IndexOf('[');
        
        while (tagStart >= 0)
        {
            var tagEnd = result.IndexOf(']', tagStart);
            if (tagEnd < 0)
                break;

            var tag = result.Substring(tagStart + 1, tagEnd - tagStart - 1);
            var replacement = GetRandomRule(tag);
            
            if (replacement != null)
            {
                result = result.Substring(0, tagStart) + replacement + result.Substring(tagEnd + 1);
                // Recursively expand the replacement
                result = Expand(result, maxDepth - 1);
            }
            else
            {
                // Tag not found, leave it as-is and continue
                tagStart = result.IndexOf('[', tagEnd);
            }
            
            if (tagStart >= 0)
                tagStart = result.IndexOf('[', tagStart);
            else
                break;
        }

        return result;
    }

    /// <summary>
    /// Get a random rule for the given tag.
    /// </summary>
    private string? GetRandomRule(string tag)
    {
        if (!_rules.ContainsKey(tag) || _rules[tag].Count == 0)
            return null;

        var options = _rules[tag];
        return options[_random.Next(options.Count)];
    }

    /// <summary>
    /// Check if a tag exists in the rule pack.
    /// </summary>
    public bool HasTag(string tag) => _rules.ContainsKey(tag) && _rules[tag].Count > 0;

    /// <summary>
    /// Get all templates for a tag.
    /// </summary>
    public IReadOnlyList<string> GetRules(string tag)
    {
        return _rules.TryGetValue(tag, out var rules) ? rules : Array.Empty<string>();
    }
}
