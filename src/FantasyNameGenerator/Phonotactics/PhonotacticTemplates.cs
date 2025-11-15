namespace FantasyNameGenerator.Phonotactics;

/// <summary>
/// Predefined phonotactic rule templates for different language types.
/// </summary>
public static class PhonotacticTemplates
{
    /// <summary>
    /// Germanic phonotactics - allows complex consonant clusters
    /// </summary>
    public static PhonotacticRules Germanic()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CVC", "CCVC", "CVCC", "CV" },
            ForbiddenSequences = new List<string>
            {
                "θθ", "ðð", "ʃʃ", // No double fricatives
                "nm", "ŋm", "ŋn"  // Nasal restrictions
            },
            AllowedOnsets = new List<string>
            {
                "pl", "pr", "tr", "kr", "kl", "br", "bl", "dr",
                "gr", "gl", "fr", "fl", "θr", "ʃr", "st", "sp", "sk"
            },
            AllowedCodas = new List<string>
            {
                "st", "nt", "nd", "mp", "ŋk", "lt", "rt", "ft"
            },
            MaxConsonantCluster = 3,
            MaxVowelCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3,
            EnforceSonoritySequencing = true
        };
    }

    /// <summary>
    /// Romance phonotactics - simpler, more open syllables
    /// </summary>
    public static PhonotacticRules Romance()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CV", "CVC", "V" },
            ForbiddenSequences = new List<string>
            {
                "ʃʃ", "ss", // No double sibilants
                "nm", "ŋ"   // No velar nasal
            },
            AllowedOnsets = new List<string>
            {
                "pl", "pr", "tr", "kr", "kl", "br", "bl", "dr",
                "gr", "gl", "fr", "fl"
            },
            AllowedCodas = new List<string>
            {
                "n", "m", "r", "l", "s"
            },
            MaxConsonantCluster = 2,
            MaxVowelCluster = 2,
            MinSyllables = 2,
            MaxSyllables = 4,
            EnforceSonoritySequencing = true
        };
    }

    /// <summary>
    /// Slavic phonotactics - complex consonant clusters
    /// </summary>
    public static PhonotacticRules Slavic()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CVC", "CCVC", "CCCVC", "CV" },
            ForbiddenSequences = new List<string>
            {
                "θ", "ð" // No dental fricatives
            },
            AllowedOnsets = new List<string>
            {
                "pl", "pr", "tr", "kr", "kl", "br", "bl", "dr",
                "gr", "gl", "fr", "fl", "st", "sp", "sk", "str",
                "spr", "skr", "zv", "zd"
            },
            AllowedCodas = new List<string>
            {
                "st", "nt", "nd", "mp", "sk", "lt", "rt", "ft", "kt"
            },
            MaxConsonantCluster = 4,
            MaxVowelCluster = 1,
            MinSyllables = 2,
            MaxSyllables = 4,
            EnforceSonoritySequencing = false // Slavic allows violations
        };
    }

    /// <summary>
    /// Elvish phonotactics - flowing, melodic
    /// </summary>
    public static PhonotacticRules Elvish()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CV", "CVC", "V" },
            ForbiddenSequences = new List<string>
            {
                "θθ", "ðð", "ww", "yy", // No double liquids/fricatives
                "nm", "ŋ"  // No certain nasals
            },
            AllowedOnsets = new List<string>
            {
                "l", "r", "w", "y", "f", "θ", "m", "n"
            },
            AllowedCodas = new List<string>
            {
                "n", "m", "l", "r"
            },
            MaxConsonantCluster = 1, // Very simple
            MaxVowelCluster = 2,
            MinSyllables = 2,
            MaxSyllables = 4,
            EnforceSonoritySequencing = true
        };
    }

    /// <summary>
    /// Dwarvish phonotactics - harsh, guttural
    /// </summary>
    public static PhonotacticRules Dwarvish()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CVC", "CCVC", "CVCC" },
            ForbiddenSequences = new List<string>
            {
                "θ", "ð", "w", "y" // No soft sounds
            },
            AllowedOnsets = new List<string>
            {
                "kr", "gr", "dr", "br", "kh", "gh"
            },
            AllowedCodas = new List<string>
            {
                "k", "g", "r", "n", "m", "kh", "gh"
            },
            MaxConsonantCluster = 3,
            MaxVowelCluster = 1,
            MinSyllables = 1,
            MaxSyllables = 3,
            EnforceSonoritySequencing = false
        };
    }

    /// <summary>
    /// Orcish phonotactics - simple, brutal
    /// </summary>
    public static PhonotacticRules Orcish()
    {
        return new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CVC", "CV" },
            ForbiddenSequences = new List<string>
            {
                "θ", "ð", "w", "y", "l" // No soft/liquid sounds
            },
            AllowedOnsets = new List<string>
            {
                "gr", "kr", "gh"
            },
            AllowedCodas = new List<string>
            {
                "k", "g", "gh"
            },
            MaxConsonantCluster = 2,
            MaxVowelCluster = 1,
            MinSyllables = 1,
            MaxSyllables = 2,
            EnforceSonoritySequencing = false
        };
    }

    /// <summary>
    /// Get all available templates
    /// </summary>
    public static Dictionary<string, Func<PhonotacticRules>> GetAllTemplates()
    {
        return new Dictionary<string, Func<PhonotacticRules>>
        {
            { "germanic", Germanic },
            { "romance", Romance },
            { "slavic", Slavic },
            { "elvish", Elvish },
            { "dwarvish", Dwarvish },
            { "orcish", Orcish }
        };
    }

    /// <summary>
    /// Get a template by name
    /// </summary>
    public static PhonotacticRules? GetTemplate(string name)
    {
        var templates = GetAllTemplates();
        return templates.TryGetValue(name.ToLowerInvariant(), out var factory)
            ? factory()
            : null;
    }
}
