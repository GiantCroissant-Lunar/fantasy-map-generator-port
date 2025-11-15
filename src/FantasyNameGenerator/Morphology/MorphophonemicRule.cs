using System.Text.RegularExpressions;

namespace FantasyNameGenerator.Morphology;

/// <summary>
/// Represents a morphophonemic rule - sound changes at morpheme boundaries.
/// Example: "cat" + "s" → "cats" (no change)
///          "bus" + "s" → "buses" (add 'e')
/// </summary>
public class MorphophonemicRule
{
    /// <summary>
    /// Pattern to match (regex)
    /// Example: "s$" (ends with 's')
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Replacement string
    /// Example: "es" (add 'es' instead of 's')
    /// </summary>
    public string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// Context where this rule applies
    /// Example: "before_suffix" or "at_boundary"
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Apply this rule to a word
    /// </summary>
    public string Apply(string word)
    {
        if (string.IsNullOrEmpty(Pattern) || string.IsNullOrEmpty(word))
            return word;

        try
        {
            return Regex.Replace(word, Pattern, Replacement, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            // Invalid regex, return unchanged
            return word;
        }
    }
}
