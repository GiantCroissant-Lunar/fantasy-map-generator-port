namespace FantasyNameGenerator;

/// <summary>
/// Configuration options for the name generator.
/// </summary>
public class NameGeneratorOptions
{
    /// <summary>
    /// Gets or sets the custom path to load language templates from.
    /// If not specified, only built-in templates will be used.
    /// </summary>
    public string? CustomLanguagesPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to load custom templates.
    /// Default is true.
    /// </summary>
    public bool LoadCustomTemplates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use JSON templates.
    /// If false, falls back to hardcoded templates.
    /// Default is true.
    /// </summary>
    public bool UseJsonTemplates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to cache loaded templates.
    /// Default is true for performance.
    /// </summary>
    public bool CacheTemplates { get; set; } = true;
}
