using System.Text.RegularExpressions;

namespace FantasyNameGenerator.Phonotactics;

/// <summary>
/// Defines phonotactic rules - legal sound combinations and constraints.
/// Based on conlang-namegen concepts.
/// </summary>
public class PhonotacticRules
{
    /// <summary>
    /// Allowed syllable structure patterns (e.g., "CVC", "CV", "CCVC")
    /// </summary>
    public List<string> AllowedStructures { get; set; } = new();

    /// <summary>
    /// Forbidden phoneme sequences (regex patterns)
    /// Example: "θθ" (no double theta), "nm" (n cannot precede m)
    /// </summary>
    public List<string> ForbiddenSequences { get; set; } = new();

    /// <summary>
    /// Allowed onset clusters (syllable-initial consonant clusters)
    /// Example: ["pl", "tr", "st"]
    /// </summary>
    public List<string> AllowedOnsets { get; set; } = new();

    /// <summary>
    /// Allowed coda clusters (syllable-final consonant clusters)
    /// Example: ["st", "nt", "mp"]
    /// </summary>
    public List<string> AllowedCodas { get; set; } = new();

    /// <summary>
    /// Maximum consonant cluster size
    /// </summary>
    public int MaxConsonantCluster { get; set; } = 2;

    /// <summary>
    /// Maximum vowel cluster size (diphthongs, triphthongs)
    /// </summary>
    public int MaxVowelCluster { get; set; } = 2;

    /// <summary>
    /// Enforce sonority sequencing principle
    /// (consonant clusters must increase in sonority toward vowel)
    /// </summary>
    public bool EnforceSonoritySequencing { get; set; } = true;

    /// <summary>
    /// Minimum syllables per word
    /// </summary>
    public int MinSyllables { get; set; } = 1;

    /// <summary>
    /// Maximum syllables per word
    /// </summary>
    public int MaxSyllables { get; set; } = 3;

    /// <summary>
    /// Check if a syllable is valid according to these rules
    /// </summary>
    public bool IsValidSyllable(string syllable, string vowels, string consonants)
    {
        if (string.IsNullOrEmpty(syllable))
            return false;

        // Check forbidden sequences
        foreach (var forbidden in ForbiddenSequences)
        {
            try
            {
                if (Regex.IsMatch(syllable, forbidden, RegexOptions.IgnoreCase))
                    return false;
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, skip
                continue;
            }
        }

        // Check consonant cluster size
        if (!CheckConsonantClusters(syllable, consonants))
            return false;

        // Check vowel cluster size
        if (!CheckVowelClusters(syllable, vowels))
            return false;

        return true;
    }

    /// <summary>
    /// Check if a word is valid according to these rules
    /// </summary>
    public bool IsValidWord(string word, string vowels, string consonants)
    {
        if (string.IsNullOrEmpty(word))
            return false;

        // Check forbidden sequences
        foreach (var forbidden in ForbiddenSequences)
        {
            try
            {
                if (Regex.IsMatch(word, forbidden, RegexOptions.IgnoreCase))
                    return false;
            }
            catch (ArgumentException)
            {
                continue;
            }
        }

        // Check consonant clusters
        if (!CheckConsonantClusters(word, consonants))
            return false;

        // Check vowel clusters
        if (!CheckVowelClusters(word, vowels))
            return false;

        return true;
    }

    private bool CheckConsonantClusters(string text, string consonants)
    {
        int clusterSize = 0;
        foreach (char c in text)
        {
            if (consonants.Contains(c))
            {
                clusterSize++;
                if (clusterSize > MaxConsonantCluster)
                    return false;
            }
            else
            {
                clusterSize = 0;
            }
        }
        return true;
    }

    private bool CheckVowelClusters(string text, string vowels)
    {
        int clusterSize = 0;
        foreach (char c in text)
        {
            if (vowels.Contains(c))
            {
                clusterSize++;
                if (clusterSize > MaxVowelCluster)
                    return false;
            }
            else
            {
                clusterSize = 0;
            }
        }
        return true;
    }

    /// <summary>
    /// Get a random valid syllable structure
    /// </summary>
    public string GetRandomStructure(Random random)
    {
        if (AllowedStructures.Count == 0)
            return "CV"; // Default

        return AllowedStructures[random.Next(AllowedStructures.Count)];
    }

    /// <summary>
    /// Clone these rules
    /// </summary>
    public PhonotacticRules Clone()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string>(AllowedStructures),
            ForbiddenSequences = new List<string>(ForbiddenSequences),
            AllowedOnsets = new List<string>(AllowedOnsets),
            AllowedCodas = new List<string>(AllowedCodas),
            MaxConsonantCluster = MaxConsonantCluster,
            MaxVowelCluster = MaxVowelCluster,
            EnforceSonoritySequencing = EnforceSonoritySequencing,
            MinSyllables = MinSyllables,
            MaxSyllables = MaxSyllables
        };
    }
}
