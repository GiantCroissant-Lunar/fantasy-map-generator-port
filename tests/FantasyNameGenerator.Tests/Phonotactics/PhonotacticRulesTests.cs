using FantasyNameGenerator.Phonotactics;

namespace FantasyNameGenerator.Tests.Phonotactics;

public class PhonotacticRulesTests
{
    [Fact]
    public void IsValidSyllable_WithValidSyllable_ReturnsTrue()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            MaxConsonantCluster = 2,
            MaxVowelCluster = 2,
            ForbiddenSequences = new List<string>()
        };

        // Act
        var result = rules.IsValidSyllable("pat", "aeiou", "ptkbdgmnlrs");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidSyllable_WithForbiddenSequence_ReturnsFalse()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            ForbiddenSequences = new List<string> { "θθ" }
        };

        // Act
        var result = rules.IsValidSyllable("θθa", "a", "θ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidSyllable_WithTooManyConsonants_ReturnsFalse()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            MaxConsonantCluster = 2
        };

        // Act
        var result = rules.IsValidSyllable("stra", "a", "str");

        // Assert
        Assert.False(result); // "str" is 3 consonants
    }

    [Fact]
    public void IsValidSyllable_WithTooManyVowels_ReturnsFalse()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            MaxVowelCluster = 2
        };

        // Act
        var result = rules.IsValidSyllable("paei", "aei", "p");

        // Assert
        Assert.False(result); // "aei" is 3 vowels
    }

    [Fact]
    public void IsValidSyllable_WithEmptySyllable_ReturnsFalse()
    {
        // Arrange
        var rules = new PhonotacticRules();

        // Act
        var result = rules.IsValidSyllable("", "aeiou", "ptkbdg");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidWord_WithValidWord_ReturnsTrue()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            MaxConsonantCluster = 2,
            MaxVowelCluster = 2,
            ForbiddenSequences = new List<string>()
        };

        // Act
        var result = rules.IsValidWord("pataka", "aeiou", "ptkbdgmnlrs");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidWord_WithForbiddenSequence_ReturnsFalse()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            ForbiddenSequences = new List<string> { "nm" }
        };

        // Act
        var result = rules.IsValidWord("panma", "a", "pnm");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetRandomStructure_ReturnsFromAllowedStructures()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CV", "CVC", "CCVC" }
        };
        var random = new Random(12345);

        // Act
        var structure = rules.GetRandomStructure(random);

        // Assert
        Assert.Contains(structure, rules.AllowedStructures);
    }

    [Fact]
    public void GetRandomStructure_WithNoStructures_ReturnsDefault()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            AllowedStructures = new List<string>()
        };
        var random = new Random(12345);

        // Act
        var structure = rules.GetRandomStructure(random);

        // Assert
        Assert.Equal("CV", structure);
    }

    [Fact]
    public void GetRandomStructure_WithSameSeed_ReturnsSameStructure()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CV", "CVC", "CCVC", "V" }
        };

        // Act
        var structure1 = rules.GetRandomStructure(new Random(12345));
        var structure2 = rules.GetRandomStructure(new Random(12345));

        // Assert
        Assert.Equal(structure1, structure2);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new PhonotacticRules
        {
            AllowedStructures = new List<string> { "CV", "CVC" },
            ForbiddenSequences = new List<string> { "θθ" },
            MaxConsonantCluster = 2,
            MaxVowelCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3
        };

        // Act
        var clone = original.Clone();
        clone.AllowedStructures.Add("CCVC");
        clone.ForbiddenSequences.Add("nm");
        clone.MaxConsonantCluster = 3;

        // Assert
        Assert.Equal(2, original.AllowedStructures.Count);
        Assert.Single(original.ForbiddenSequences);
        Assert.Equal(2, original.MaxConsonantCluster);
    }

    [Theory]
    [InlineData("pat", true)]
    [InlineData("ta", true)]
    [InlineData("stra", false)] // 3 consonants
    [InlineData("aei", false)]  // 3 vowels
    public void IsValidSyllable_WithVariousInputs_ReturnsExpected(string syllable, bool expected)
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            MaxConsonantCluster = 2,
            MaxVowelCluster = 2
        };

        // Act
        var result = rules.IsValidSyllable(syllable, "aei", "ptkstr");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidSyllable_WithInvalidRegexPattern_DoesNotThrow()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            ForbiddenSequences = new List<string> { "[invalid(" } // Invalid regex
        };

        // Act & Assert - Should not throw
        var result = rules.IsValidSyllable("pat", "a", "pt");
        Assert.True(result); // Invalid regex is skipped
    }

    [Fact]
    public void IsValidWord_WithInvalidRegexPattern_DoesNotThrow()
    {
        // Arrange
        var rules = new PhonotacticRules
        {
            ForbiddenSequences = new List<string> { "[invalid(" }
        };

        // Act & Assert
        var result = rules.IsValidWord("pataka", "a", "ptk");
        Assert.True(result);
    }
}
