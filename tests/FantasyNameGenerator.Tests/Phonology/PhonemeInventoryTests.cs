using FantasyNameGenerator.Phonology;

namespace FantasyNameGenerator.Tests.Phonology;

public class PhonemeInventoryTests
{
    [Fact]
    public void GetAllPhonemes_ReturnsUniquePhonemes()
    {
        // Arrange
        var inventory = new PhonemeInventory
        {
            Consonants = "ptk",
            Vowels = "aei",
            Liquids = "lr"
        };

        // Act
        var allPhonemes = inventory.GetAllPhonemes();

        // Assert
        Assert.Contains('p', allPhonemes);
        Assert.Contains('t', allPhonemes);
        Assert.Contains('k', allPhonemes);
        Assert.Contains('a', allPhonemes);
        Assert.Contains('e', allPhonemes);
        Assert.Contains('i', allPhonemes);
        Assert.Contains('l', allPhonemes);
        Assert.Contains('r', allPhonemes);
    }

    [Fact]
    public void GetAllPhonemes_RemovesDuplicates()
    {
        // Arrange
        var inventory = new PhonemeInventory
        {
            Consonants = "ptkl",
            Liquids = "lr" // 'l' appears in both
        };

        // Act
        var allPhonemes = inventory.GetAllPhonemes();

        // Assert
        Assert.Equal(5, allPhonemes.Length); // p, t, k, l, r (no duplicate 'l')
    }

    [Fact]
    public void ToPhonemeMap_CreatesCorrectMapping()
    {
        // Arrange
        var inventory = new PhonemeInventory
        {
            Consonants = "ptk",
            Vowels = "aei",
            Liquids = "lr"
        };

        // Act
        var map = inventory.ToPhonemeMap();

        // Assert
        Assert.Equal("ptk", map['C']);
        Assert.Equal("aei", map['V']);
        Assert.Equal("lr", map['L']);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new PhonemeInventory
        {
            Consonants = "ptk",
            Vowels = "aei"
        };

        // Act
        var clone = original.Clone();
        clone.Consonants = "bdg"; // Modify clone

        // Assert
        Assert.Equal("ptk", original.Consonants); // Original unchanged
        Assert.Equal("bdg", clone.Consonants);
    }
}
