namespace FantasyNameGenerator.Phonology;

/// <summary>
/// Represents a phoneme inventory - the set of sounds used in a language.
/// Based on IPA (International Phonetic Alphabet) symbols.
/// </summary>
public class PhonemeInventory
{
    /// <summary>
    /// Consonant phonemes (e.g., "ptkbdgmnlrsʃʒθð")
    /// </summary>
    public string Consonants { get; set; } = string.Empty;

    /// <summary>
    /// Vowel phonemes (e.g., "aeiouəɛɔ")
    /// </summary>
    public string Vowels { get; set; } = string.Empty;

    /// <summary>
    /// Liquid consonants (e.g., "lrwy")
    /// </summary>
    public string Liquids { get; set; } = string.Empty;

    /// <summary>
    /// Nasal consonants (e.g., "mnŋ")
    /// </summary>
    public string Nasals { get; set; } = string.Empty;

    /// <summary>
    /// Fricative consonants (e.g., "fvszʃʒθð")
    /// </summary>
    public string Fricatives { get; set; } = string.Empty;

    /// <summary>
    /// Stop consonants (e.g., "ptkbdg")
    /// </summary>
    public string Stops { get; set; } = string.Empty;

    /// <summary>
    /// Sibilant consonants (e.g., "sʃʒz")
    /// </summary>
    public string Sibilants { get; set; } = string.Empty;

    /// <summary>
    /// Syllable-final consonants (e.g., "mnŋs")
    /// </summary>
    public string Finals { get; set; } = string.Empty;

    /// <summary>
    /// Get all phonemes as a single string
    /// </summary>
    public string GetAllPhonemes()
    {
        var allPhonemes = new HashSet<char>();
        
        foreach (char c in Consonants) allPhonemes.Add(c);
        foreach (char c in Vowels) allPhonemes.Add(c);
        foreach (char c in Liquids) allPhonemes.Add(c);
        foreach (char c in Nasals) allPhonemes.Add(c);
        foreach (char c in Fricatives) allPhonemes.Add(c);
        foreach (char c in Stops) allPhonemes.Add(c);
        foreach (char c in Sibilants) allPhonemes.Add(c);
        foreach (char c in Finals) allPhonemes.Add(c);
        
        return new string(allPhonemes.ToArray());
    }

    /// <summary>
    /// Create a phoneme map for use with SyllableGenerator
    /// </summary>
    public Dictionary<char, string> ToPhonemeMap()
    {
        return new Dictionary<char, string>
        {
            { 'C', Consonants },
            { 'V', Vowels },
            { 'L', Liquids },
            { 'N', Nasals },
            { 'F', Fricatives },
            { 'S', Sibilants },
            { 'T', Stops },
            { 'E', Finals } // E for End
        };
    }

    /// <summary>
    /// Clone this inventory
    /// </summary>
    public PhonemeInventory Clone()
    {
        return new PhonemeInventory
        {
            Consonants = Consonants,
            Vowels = Vowels,
            Liquids = Liquids,
            Nasals = Nasals,
            Fricatives = Fricatives,
            Stops = Stops,
            Sibilants = Sibilants,
            Finals = Finals
        };
    }
}
