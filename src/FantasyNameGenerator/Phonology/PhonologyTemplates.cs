using FantasyNameGenerator.Configuration;

namespace FantasyNameGenerator.Phonology;

/// <summary>
/// Predefined phonology templates for different language types.
/// Based on real-world language families and fantasy archetypes.
/// Now supports both hardcoded templates and JSON-based templates with auto-discovery.
/// </summary>
public static class PhonologyTemplates
{
    private static readonly Dictionary<string, CulturePhonology> _cachedTemplates = new();
    private static bool _useJsonTemplates = true;
    private static bool _useRegistry = false;

    /// <summary>
    /// Enable or disable JSON template loading (default: true).
    /// When disabled, falls back to hardcoded templates.
    /// </summary>
    public static bool UseJsonTemplates
    {
        get => _useJsonTemplates;
        set
        {
            _useJsonTemplates = value;
            if (!value)
                _cachedTemplates.Clear();
        }
    }

    /// <summary>
    /// Enable or disable template registry (default: false).
    /// When enabled, uses TemplateRegistry for template discovery.
    /// </summary>
    public static bool UseRegistry
    {
        get => _useRegistry;
        set
        {
            _useRegistry = value;
            if (value)
                _cachedTemplates.Clear();
        }
    }

    /// <summary>
    /// Get the template registry instance.
    /// </summary>
    public static TemplateRegistry Registry => TemplateRegistry.Instance;
    /// <summary>
    /// Germanic-inspired phonology (English, German, Norse)
    /// Hard consonants, consonant clusters, diphthongs
    /// </summary>
    public static CulturePhonology Germanic(Random? random = null)
    {
        var phonology = new CulturePhonology
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

        if (random != null)
            phonology.ShufflePhonemes(random);

        return phonology;
    }

    /// <summary>
    /// Romance-inspired phonology (Latin, Italian, Spanish, French)
    /// Flowing vowels, softer consonants
    /// </summary>
    public static CulturePhonology Romance(Random? random = null)
    {
        var phonology = new CulturePhonology
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

        if (random != null)
            phonology.ShufflePhonemes(random);

        return phonology;
    }

    /// <summary>
    /// Slavic-inspired phonology (Russian, Polish, Czech)
    /// Consonant clusters, palatalization
    /// </summary>
    public static CulturePhonology Slavic(Random? random = null)
    {
        var phonology = new CulturePhonology
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

        if (random != null)
            phonology.ShufflePhonemes(random);

        return phonology;
    }

    /// <summary>
    /// Elvish-inspired phonology (Tolkien-style)
    /// Melodic, flowing, lots of liquids and vowels
    /// </summary>
    public static CulturePhonology Elvish(Random? random = null)
    {
        var phonology = new CulturePhonology
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

        if (random != null)
            phonology.ShufflePhonemes(random);

        return phonology;
    }

    /// <summary>
    /// Dwarvish-inspired phonology
    /// Harsh, guttural, lots of consonant clusters
    /// </summary>
    public static CulturePhonology Dwarvish(Random? random = null)
    {
        var phonology = new CulturePhonology
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

        if (random != null)
            phonology.ShufflePhonemes(random);

        return phonology;
    }

    /// <summary>
    /// Orcish-inspired phonology
    /// Simple, brutal, harsh sounds
    /// </summary>
    public static CulturePhonology Orcish(Random? random = null)
    {
        var phonology = new CulturePhonology
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

        if (random != null)
            phonology.ShufflePhonemes(random);

        return phonology;
    }

    /// <summary>
    /// Get all available templates
    /// </summary>
    public static Dictionary<string, Func<CulturePhonology>> GetAllTemplates()
    {
        if (_useJsonTemplates)
        {
            var templates = new Dictionary<string, Func<CulturePhonology>>();
            var builtInNames = LanguageTemplateLoader.GetBuiltInTemplateNames();
            
            foreach (var name in builtInNames)
            {
                templates[name] = () => GetTemplateFromJson(name);
            }
            
            return templates;
        }

        return new Dictionary<string, Func<CulturePhonology>>
        {
            { "germanic", () => Germanic() },
            { "romance", () => Romance() },
            { "slavic", () => Slavic() },
            { "elvish", () => Elvish() },
            { "dwarvish", () => Dwarvish() },
            { "orcish", () => Orcish() }
        };
    }

    /// <summary>
    /// Get a template by name.
    /// First tries registry (if enabled), then JSON templates, then falls back to hardcoded.
    /// </summary>
    public static CulturePhonology? GetTemplate(string name)
    {
        var normalizedName = name.ToLowerInvariant();

        // Try registry first if enabled
        if (_useRegistry)
        {
            var template = TemplateRegistry.Instance.GetTemplate(normalizedName);
            if (template != null)
                return template.Clone();
        }

        if (_useJsonTemplates)
        {
            var template = GetTemplateFromJson(normalizedName);
            if (template != null)
                return template;
        }

        var templates = GetAllTemplates();
        return templates.TryGetValue(normalizedName, out var factory) 
            ? factory() 
            : null;
    }

    /// <summary>
    /// Load a template from JSON with caching.
    /// </summary>
    private static CulturePhonology? GetTemplateFromJson(string name)
    {
        if (_cachedTemplates.TryGetValue(name, out var cached))
            return cached.Clone();

        try
        {
            var template = LanguageTemplateLoader.LoadBuiltIn(name);
            if (template != null)
            {
                _cachedTemplates[name] = template;
                return template.Clone();
            }
        }
        catch
        {
            // Fall back to hardcoded if JSON fails
        }

        return null;
    }

    /// <summary>
    /// Load a custom template from file system.
    /// </summary>
    public static CulturePhonology? LoadCustomTemplate(string filePath)
    {
        try
        {
            return LanguageTemplateLoader.LoadFromFile(filePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get all available template names (including custom from registry if enabled).
    /// </summary>
    public static string[] GetAvailableTemplateNames()
    {
        if (_useRegistry)
        {
            return TemplateRegistry.Instance.GetAvailableTemplates();
        }

        if (_useJsonTemplates)
        {
            return LanguageTemplateLoader.GetBuiltInTemplateNames();
        }

        return new[] { "germanic", "romance", "slavic", "elvish", "dwarvish", "orcish" };
    }

    /// <summary>
    /// Add a custom template directory for auto-discovery.
    /// Requires UseRegistry = true.
    /// </summary>
    public static void AddCustomTemplateDirectory(string path)
    {
        TemplateRegistry.Instance.AddCustomTemplatePath(path);
    }

    /// <summary>
    /// Register a custom template file.
    /// Requires UseRegistry = true.
    /// </summary>
    public static void RegisterCustomTemplate(string filePath)
    {
        TemplateRegistry.Instance.RegisterCustomTemplate(filePath);
    }
}
