using FantasyNameGenerator.Phonology;

namespace FantasyNameGenerator.Tests.Phonology;

public class CulturePhonologyTests
{
    [Fact]
    public void GetWeight_ReturnsSpecifiedWeight()
    {
        // Arrange
        var phonology = new CulturePhonology();
        phonology.Weights['p'] = 0.8;
        phonology.Weights['t'] = 0.5;

        // Act & Assert
        Assert.Equal(0.8, phonology.GetWeight('p'));
        Assert.Equal(0.5, phonology.GetWeight('t'));
    }

    [Fact]
    public void GetWeight_ReturnsDefaultForUnspecified()
    {
        // Arrange
        var phonology = new CulturePhonology();

        // Act & Assert
        Assert.Equal(1.0, phonology.GetWeight('x'));
    }

    [Fact]
    public void ApplyOrthography_ConvertsIPAToWrittenForm()
    {
        // Arrange
        var phonology = new CulturePhonology
        {
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sh" },
                { 'θ', "th" }
            }
        };

        // Act
        var result = phonology.ApplyOrthography("ʃaθa");

        // Assert
        Assert.Equal("shatha", result);
    }

    [Fact]
    public void ApplyOrthography_LeavesUnmappedCharactersUnchanged()
    {
        // Arrange
        var phonology = new CulturePhonology
        {
            Orthography = new Dictionary<char, string>
            {
                { 'ʃ', "sh" }
            }
        };

        // Act
        var result = phonology.ApplyOrthography("ʃapa");

        // Assert
        Assert.Equal("shapa", result);
    }

    [Fact]
    public void ShufflePhonemes_ChangesPhonemeOrder()
    {
        // Arrange
        var phonology = new CulturePhonology
        {
            Inventory = new PhonemeInventory
            {
                Consonants = "abcdefghijklmnop"
            }
        };
        var original = phonology.Inventory.Consonants;
        var random = new Random(12345);

        // Act
        phonology.ShufflePhonemes(random);

        // Assert
        Assert.NotEqual(original, phonology.Inventory.Consonants);
        Assert.Equal(original.Length, phonology.Inventory.Consonants.Length);
        // All characters still present
        foreach (char c in original)
        {
            Assert.Contains(c, phonology.Inventory.Consonants);
        }
    }

    [Fact]
    public void ShufflePhonemes_WithSameSeed_ProducesSameResult()
    {
        // Arrange
        var phonology1 = new CulturePhonology
        {
            Inventory = new PhonemeInventory { Consonants = "abcdefghijklmnop" }
        };
        var phonology2 = new CulturePhonology
        {
            Inventory = new PhonemeInventory { Consonants = "abcdefghijklmnop" }
        };

        // Act
        phonology1.ShufflePhonemes(new Random(12345));
        phonology2.ShufflePhonemes(new Random(12345));

        // Assert
        Assert.Equal(phonology1.Inventory.Consonants, phonology2.Inventory.Consonants);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new CulturePhonology
        {
            Name = "Test",
            Inventory = new PhonemeInventory { Consonants = "ptk" },
            Weights = new Dictionary<char, double> { { 'p', 0.8 } },
            Orthography = new Dictionary<char, string> { { 'ʃ', "sh" } }
        };

        // Act
        var clone = original.Clone();
        clone.Name = "Modified";
        clone.Inventory.Consonants = "bdg";
        clone.Weights['p'] = 0.5;
        clone.Orthography['ʃ'] = "ch";

        // Assert
        Assert.Equal("Test", original.Name);
        Assert.Equal("ptk", original.Inventory.Consonants);
        Assert.Equal(0.8, original.Weights['p']);
        Assert.Equal("sh", original.Orthography['ʃ']);
    }

    [Fact]
    public void ApplyAllophones_AppliesContextualRules()
    {
        // Arrange
        var phonology = new CulturePhonology
        {
            Inventory = new PhonemeInventory { Vowels = "aeiou" },
            Allophones = new List<AllophoneRule>
            {
                new AllophoneRule
                {
                    Phoneme = 't',
                    Allophone = 'ɾ',
                    Context = "V_V" // t becomes ɾ between vowels
                }
            }
        };

        // Act
        var result = phonology.ApplyAllophones("ata");

        // Assert
        Assert.Equal("aɾa", result);
    }
}
