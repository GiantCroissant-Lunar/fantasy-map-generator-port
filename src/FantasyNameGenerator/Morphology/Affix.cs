namespace FantasyNameGenerator.Morphology;

/// <summary>
/// Represents an affix (prefix, suffix, or infix) that can be added to words.
/// </summary>
public class Affix
{
    /// <summary>
    /// The phonetic form of the affix
    /// </summary>
    public string Form { get; set; } = string.Empty;

    /// <summary>
    /// The semantic meaning of the affix
    /// Example: "-ton" = town, "new-" = new
    /// </summary>
    public string Meaning { get; set; } = string.Empty;

    /// <summary>
    /// How frequently this affix should be used (0.0 to 1.0)
    /// </summary>
    public double Frequency { get; set; } = 0.1;

    /// <summary>
    /// Position where this affix can be applied
    /// </summary>
    public AffixPosition Position { get; set; } = AffixPosition.Suffix;
}

/// <summary>
/// Position where an affix can be applied
/// </summary>
public enum AffixPosition
{
    /// <summary>
    /// Before the root (e.g., "un-happy")
    /// </summary>
    Prefix,

    /// <summary>
    /// After the root (e.g., "happi-ness")
    /// </summary>
    Suffix,

    /// <summary>
    /// Inside the root (rare in English, common in Arabic)
    /// </summary>
    Infix
}
