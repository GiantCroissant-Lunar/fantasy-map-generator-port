namespace FantasyNameGenerator.Core;

/// <summary>
/// Core syllable construction from phoneme patterns.
/// This is the foundation layer that all other layers build upon.
/// </summary>
public class SyllableGenerator
{
    private readonly Random _random;

    public SyllableGenerator(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Generate a syllable from a pattern string.
    /// Pattern symbols: C=Consonant, V=Vowel, L=Liquid, S=Sibilant, F=Final, ?=Optional
    /// Example: "CVC" -> consonant + vowel + consonant
    /// Example: "CV?" -> consonant + vowel + optional consonant
    /// </summary>
    public string GenerateSyllable(string pattern, Dictionary<char, string> phonemeMap)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        if (phonemeMap == null)
            throw new ArgumentNullException(nameof(phonemeMap));

        var syllable = new System.Text.StringBuilder();
        
        for (int i = 0; i < pattern.Length; i++)
        {
            char symbol = pattern[i];
            
            // Handle optional phoneme marker
            if (symbol == '?')
            {
                // Previous phoneme was optional, skip if random says so
                if (_random.NextDouble() < 0.5 && syllable.Length > 0)
                {
                    syllable.Length--; // Remove last character
                }
                continue;
            }
            
            // Get phoneme set for this symbol
            if (!phonemeMap.TryGetValue(symbol, out string? phonemes))
            {
                // Symbol not in map, skip it
                continue;
            }
            
            if (string.IsNullOrEmpty(phonemes))
            {
                // Empty phoneme set, skip
                continue;
            }
            
            // Select random phoneme from set
            int index = _random.Next(phonemes.Length);
            syllable.Append(phonemes[index]);
        }
        
        return syllable.ToString();
    }

    /// <summary>
    /// Generate a syllable with weighted phoneme selection.
    /// Higher exponent = more bias toward beginning of phoneme string.
    /// </summary>
    public string GenerateSyllableWeighted(
        string pattern, 
        Dictionary<char, string> phonemeMap, 
        double exponent = 1.0)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        if (phonemeMap == null)
            throw new ArgumentNullException(nameof(phonemeMap));

        var syllable = new System.Text.StringBuilder();
        
        for (int i = 0; i < pattern.Length; i++)
        {
            char symbol = pattern[i];
            
            // Handle optional phoneme marker
            if (symbol == '?')
            {
                if (_random.NextDouble() < 0.5 && syllable.Length > 0)
                {
                    syllable.Length--;
                }
                continue;
            }
            
            // Get phoneme set for this symbol
            if (!phonemeMap.TryGetValue(symbol, out string? phonemes))
                continue;
            
            if (string.IsNullOrEmpty(phonemes))
                continue;
            
            // Weighted selection (bias toward beginning)
            int index = (int)Math.Floor(Math.Pow(_random.NextDouble(), exponent) * phonemes.Length);
            syllable.Append(phonemes[index]);
        }
        
        return syllable.ToString();
    }

    /// <summary>
    /// Parse a pattern string and validate it.
    /// Returns true if pattern is valid.
    /// </summary>
    public static bool IsValidPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        var validSymbols = new HashSet<char> { 'C', 'V', 'L', 'S', 'F', '?' };
        
        foreach (char c in pattern)
        {
            if (!validSymbols.Contains(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get all possible syllable structures from a pattern.
    /// Expands optional markers into multiple patterns.
    /// Example: "CV?" -> ["CV", "C"]
    /// </summary>
    public static List<string> ExpandPattern(string pattern)
    {
        var results = new List<string> { "" };

        foreach (char c in pattern)
        {
            if (c == '?')
            {
                // Previous character is optional
                // Duplicate all results: one with last char, one without
                var newResults = new List<string>();
                foreach (var result in results)
                {
                    if (result.Length > 0)
                    {
                        // Add version without last character
                        newResults.Add(result[..^1]);
                        // Add version with last character (keep as is)
                        newResults.Add(result);
                    }
                    else
                    {
                        newResults.Add(result);
                    }
                }
                results = newResults;
            }
            else
            {
                // Add this character to all results
                for (int i = 0; i < results.Count; i++)
                {
                    results[i] += c;
                }
            }
        }

        return results.Distinct().ToList();
    }
}
