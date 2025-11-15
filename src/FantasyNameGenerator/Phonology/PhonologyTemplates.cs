namespace FantasyNameGenerator.Phonology;

/// <summary>
/// Predefined phonology templates for different language types.
/// Based on real-world language families and fantasy archetypes.
/// </summary>
public static class PhonologyTemplates
{
    /// <summary>
    /// Germanic-inspired phonology (English, German, Norse)
    /// Hard consonants, consonant clusters, diphthongs
    /// </summary>
    public static CulturePhonology Germanic()
    {
        return new CulturePhonology
        {
            Name = "Germanic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdgmnlrsʃfvθð",
                Vowels = "aeiouæɑɪʊ",
                Liquids = "lrw",
                Nasals = "mnŋ",
                Fricatives = "fvszʃθð",
                Stops = "ptkbdg",
                Sibilants = "sʃz",
                Finals = "mnstŋ"
            },
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sh" },
                { 'θ', "th" },
                { 'ð', "th" },
                { 'æ', "ae" },
                { 'ɑ', "a" },
                { 'ɪ', "i" },
                { 'ʊ', "u" },
                { 'ŋ', "ng" }
            }
        };
    }

    /// <summary>
    /// Romance-inspired phonology (Latin, Italian, Spanish, French)
    /// Flowing vowels, softer consonants
    /// </summary>
    public static CulturePhonology Romance()
    {
        return new CulturePhonology
        {
            Name = "Romance",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdgmnlrsʃ",
                Vowels = "aeiouəɛɔ",
                Liquids = "lr",
                Nasals = "mn",
                Fricatives = "fvsʃ",
                Stops = "ptkbdg",
                Sibilants = "sʃ",
                Finals = "mnrs"
            },
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sc" },
                { 'ə', "e" },
                { 'ɛ', "e" },
                { 'ɔ', "o" }
            }
        };
    }

    /// <summary>
    /// Slavic-inspired phonology (Russian, Polish, Czech)
    /// Consonant clusters, palatalization
    /// </summary>
    public static CulturePhonology Slavic()
    {
        return new CulturePhonology
        {
            Name = "Slavic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdgmnlrszʃʒʧʤ",
                Vowels = "aeiouɨ",
                Liquids = "lrj",
                Nasals = "mn",
                Fricatives = "fvszʃʒ",
                Stops = "ptkbdg",
                Sibilants = "szʃʒ",
                Finals = "mnstk"
            },
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sh" },
                { 'ʒ', "zh" },
                { 'ʧ', "ch" },
                { 'ʤ', "j" },
                { 'ɨ', "y" },
                { 'j', "y" }
            }
        };
    }

    /// <summary>
    /// Elvish-inspired phonology (Tolkien-style)
    /// Melodic, flowing, lots of liquids and vowels
    /// </summary>
    public static CulturePhonology Elvish()
    {
        return new CulturePhonology
        {
            Name = "Elvish",
            Inventory = new PhonemeInventory
            {
                Consonants = "lmnrwyfθð",
                Vowels = "aeiouəɛ",
                Liquids = "lrwy",
                Nasals = "mn",
                Fricatives = "fθð",
                Stops = "pt",
                Sibilants = "s",
                Finals = "nmlr"
            },
            Orthography = new Dictionary<char, string>
            {
                { 'θ', "th" },
                { 'ð', "dh" },
                { 'ə', "e" },
                { 'ɛ', "ë" }
            }
        };
    }

    /// <summary>
    /// Dwarvish-inspired phonology
    /// Harsh, guttural, lots of consonant clusters
    /// </summary>
    public static CulturePhonology Dwarvish()
    {
        return new CulturePhonology
        {
            Name = "Dwarvish",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdgmnrsʃχ",
                Vowels = "aeiou",
                Liquids = "r",
                Nasals = "mn",
                Fricatives = "fsʃχ",
                Stops = "ptkbdg",
                Sibilants = "sʃ",
                Finals = "mnrskχ"
            },
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sh" },
                { 'χ', "kh" }
            }
        };
    }

    /// <summary>
    /// Orcish-inspired phonology
    /// Simple, brutal, harsh sounds
    /// </summary>
    public static CulturePhonology Orcish()
    {
        return new CulturePhonology
        {
            Name = "Orcish",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdgmnrsʃχ",
                Vowels = "aou",
                Liquids = "r",
                Nasals = "mn",
                Fricatives = "sʃχ",
                Stops = "ptkbdg",
                Sibilants = "sʃ",
                Finals = "kgχ"
            },
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sh" },
                { 'χ', "gh" }
            }
        };
    }

    /// <summary>
    /// Get all available templates
    /// </summary>
    public static Dictionary<string, Func<CulturePhonology>> GetAllTemplates()
    {
        return new Dictionary<string, Func<CulturePhonology>>
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
    public static CulturePhonology? GetTemplate(string name)
    {
        var templates = GetAllTemplates();
        return templates.TryGetValue(name.ToLowerInvariant(), out var factory) 
            ? factory() 
            : null;
    }
}
