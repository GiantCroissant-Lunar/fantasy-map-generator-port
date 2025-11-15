namespace FantasyNameGenerator.Morphology;

/// <summary>
/// Rules for combining words into compounds.
/// </summary>
public class CompoundingRules
{
    /// <summary>
    /// String used to join compound words (space, hyphen, or empty)
    /// </summary>
    public string Joiner { get; set; } = " ";

    /// <summary>
    /// Whether the head (main word) comes first
    /// True = head-initial (English: "blackbird")
    /// False = head-final (Japanese: "yama-dera" = mountain-temple)
    /// </summary>
    public bool HeadFirst { get; set; } = true;

    /// <summary>
    /// Probability of creating a compound vs simple word (0.0 to 1.0)
    /// </summary>
    public double CompoundProbability { get; set; } = 0.3;

    /// <summary>
    /// Maximum number of words in a compound
    /// </summary>
    public int MaxCompoundLength { get; set; } = 2;

    /// <summary>
    /// Clone these rules
    /// </summary>
    public CompoundingRules Clone()
    {
        return new CompoundingRules
        {
            Joiner = Joiner,
            HeadFirst = HeadFirst,
            CompoundProbability = CompoundProbability,
            MaxCompoundLength = MaxCompoundLength
        };
    }
}
