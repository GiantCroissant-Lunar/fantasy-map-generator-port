namespace FantasyNameGenerator.Phonology;

/// <summary>
/// Defines the phonological system for a culture's language.
/// Inspired by libconlang - constructed language theory.
/// </summary>
public class CulturePhonology
{
    /// <summary>
    /// Name of this phonological system
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phoneme inventory - the sounds available
    /// </summary>
    public PhonemeInventory Inventory { get; set; } = new();

    /// <summary>
    /// Phoneme frequency weights (0.0 to 1.0)
    /// Higher weight = more common
    /// </summary>
    public Dictionary<char, double> Weights { get; set; } = new();

    /// <summary>
    /// Allophonic rules - contextual sound changes
    /// </summary>
    public List<AllophoneRule> Allophones { get; set; } = new();

    /// <summary>
    /// Orthography rules - how phonemes are spelled
    /// Maps IPA symbol -> written form
    /// </summary>
    public Dictionary<char, string> Orthography { get; set; } = new();

    /// <summary>
    /// Get the weight for a phoneme (default 1.0 if not specified)
    /// </summary>
    public double GetWeight(char phoneme)
    {
        return Weights.TryGetValue(phoneme, out double weight) ? weight : 1.0;
    }

    /// <summary>
    /// Apply allophonic rules to a word
    /// </summary>
    public string ApplyAllophones(string word)
    {
        string result = word;
        foreach (var rule in Allophones)
        {
            result = rule.Apply(result, Inventory.Vowels);
        }
        return result;
    }

    /// <summary>
    /// Apply orthography rules to convert IPA to written form
    /// </summary>
    public string ApplyOrthography(string ipaWord)
    {
        var result = new System.Text.StringBuilder();
        
        foreach (char c in ipaWord)
        {
            if (Orthography.TryGetValue(c, out string? spelling))
            {
                result.Append(spelling);
            }
            else
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Shuffle phoneme order (for language mutation)
    /// </summary>
    public void ShufflePhonemes(Random random)
    {
        Inventory.Consonants = Shuffle(Inventory.Consonants, random);
        Inventory.Vowels = Shuffle(Inventory.Vowels, random);
        Inventory.Liquids = Shuffle(Inventory.Liquids, random);
        Inventory.Nasals = Shuffle(Inventory.Nasals, random);
        Inventory.Fricatives = Shuffle(Inventory.Fricatives, random);
        Inventory.Stops = Shuffle(Inventory.Stops, random);
        Inventory.Sibilants = Shuffle(Inventory.Sibilants, random);
        Inventory.Finals = Shuffle(Inventory.Finals, random);
    }

    private static string Shuffle(string str, Random random)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var chars = str.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    /// <summary>
    /// Clone this phonology
    /// </summary>
    public CulturePhonology Clone()
    {
        return new CulturePhonology
        {
            Name = Name,
            Inventory = Inventory.Clone(),
            Weights = new Dictionary<char, double>(Weights),
            Allophones = new List<AllophoneRule>(Allophones),
            Orthography = new Dictionary<char, string>(Orthography)
        };
    }
}
