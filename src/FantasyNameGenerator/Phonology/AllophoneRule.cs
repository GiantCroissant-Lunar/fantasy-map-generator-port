using System.Text.RegularExpressions;

namespace FantasyNameGenerator.Phonology;

/// <summary>
/// Represents an allophonic rule - contextual sound changes.
/// Example: /t/ becomes [ɾ] between vowels (like in "water" -> "waɾer")
/// </summary>
public class AllophoneRule
{
    /// <summary>
    /// The base phoneme
    /// </summary>
    public char Phoneme { get; set; }

    /// <summary>
    /// The allophone (variant) to use in specific contexts
    /// </summary>
    public char Allophone { get; set; }

    /// <summary>
    /// Context pattern (regex) where this rule applies
    /// Example: "V_V" means between vowels (V = vowel, _ = position)
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Apply this rule to a word
    /// </summary>
    public string Apply(string word, string vowels)
    {
        if (string.IsNullOrEmpty(Context))
            return word;

        // Convert context pattern to regex
        // V_V -> vowel + phoneme + vowel
        var pattern = Context
            .Replace("V", $"[{vowels}]")
            .Replace("C", "[^aeiouəɛɔæɑɪʊ]") // Not a vowel
            .Replace("_", Phoneme.ToString());

        try
        {
            return Regex.Replace(word, pattern, match =>
            {
                // Replace the phoneme with allophone
                return match.Value.Replace(Phoneme, Allophone);
            }, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            // Invalid regex, return unchanged
            return word;
        }
    }
}
