using FantasyNameGenerator.Core;

namespace FantasyNameGenerator.Tests.Core;

public class SyllableGeneratorTests
{
    [Fact]
    public void GenerateSyllable_WithSimplePattern_GeneratesValidSyllable()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptkmnls" },
            { 'V', "aeiou" }
        };

        // Act
        var syllable = generator.GenerateSyllable("CV", phonemeMap);

        // Assert
        Assert.NotEmpty(syllable);
        Assert.Equal(2, syllable.Length);
        Assert.Contains(syllable[0], "ptkmnls");
        Assert.Contains(syllable[1], "aeiou");
    }

    [Fact]
    public void GenerateSyllable_WithCVCPattern_GeneratesThreePhonemes()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptkmnls" },
            { 'V', "aeiou" }
        };

        // Act
        var syllable = generator.GenerateSyllable("CVC", phonemeMap);

        // Assert
        Assert.Equal(3, syllable.Length);
        Assert.Contains(syllable[0], "ptkmnls");
        Assert.Contains(syllable[1], "aeiou");
        Assert.Contains(syllable[2], "ptkmnls");
    }

    [Fact]
    public void GenerateSyllable_WithOptionalPhoneme_GeneratesVariableLengthSyllables()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptkmnls" },
            { 'V', "aeiou" }
        };

        // Act - Generate multiple syllables to test optional behavior
        var syllables = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            var gen = new SyllableGenerator(seed: i);
            syllables.Add(gen.GenerateSyllable("CV?", phonemeMap));
        }

        // Assert - Should have both 2-char and 1-char syllables
        var lengths = syllables.Select(s => s.Length).Distinct().ToList();
        Assert.True(lengths.Count > 1, "Optional phoneme should produce variable lengths");
    }

    [Fact]
    public void GenerateSyllable_WithMultiplePhonemeTypes_UsesCorrectSets()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "aei" },
            { 'L', "lr" },
            { 'S', "sʃ" }
        };

        // Act
        var syllable = generator.GenerateSyllable("CLVS", phonemeMap);

        // Assert
        Assert.Equal(4, syllable.Length);
        Assert.Contains(syllable[0], "ptk");
        Assert.Contains(syllable[1], "lr");
        Assert.Contains(syllable[2], "aei");
        Assert.Contains(syllable[3], "sʃ");
    }

    [Fact]
    public void GenerateSyllable_WithEmptyPhonemeSet_SkipsPhoneme()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "" }, // Empty vowel set
            { 'L', "lr" }
        };

        // Act
        var syllable = generator.GenerateSyllable("CVL", phonemeMap);

        // Assert
        Assert.Equal(2, syllable.Length); // Only C and L
    }

    [Fact]
    public void GenerateSyllable_WithMissingPhonemeType_SkipsPhoneme()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "aei" }
            // No 'L' defined
        };

        // Act
        var syllable = generator.GenerateSyllable("CVL", phonemeMap);

        // Assert
        Assert.Equal(2, syllable.Length); // Only C and V
    }

    [Fact]
    public void GenerateSyllable_WithSameSeed_GeneratesSameSyllable()
    {
        // Arrange
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptkmnls" },
            { 'V', "aeiou" }
        };

        // Act
        var gen1 = new SyllableGenerator(seed: 12345);
        var syllable1 = gen1.GenerateSyllable("CVC", phonemeMap);

        var gen2 = new SyllableGenerator(seed: 12345);
        var syllable2 = gen2.GenerateSyllable("CVC", phonemeMap);

        // Assert
        Assert.Equal(syllable1, syllable2);
    }

    [Fact]
    public void GenerateSyllable_WithDifferentSeeds_GeneratesDifferentSyllables()
    {
        // Arrange
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptkmnls" },
            { 'V', "aeiou" }
        };

        // Act
        var syllables = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            var gen = new SyllableGenerator(seed: i);
            syllables.Add(gen.GenerateSyllable("CVC", phonemeMap));
        }

        // Assert
        Assert.True(syllables.Count > 1, "Different seeds should produce different syllables");
    }

    [Fact]
    public void GenerateSyllable_WithNullPattern_ThrowsArgumentException()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            generator.GenerateSyllable(null!, phonemeMap));
    }

    [Fact]
    public void GenerateSyllable_WithNullPhonemeMap_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            generator.GenerateSyllable("CV", null!));
    }

    [Fact]
    public void GenerateSyllableWeighted_WithHighExponent_BiasToBeginningSyllables()
    {
        // Arrange
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "abcdefghijklmnop" }, // 16 consonants
            { 'V', "12345" }
        };

        // Act - Generate many syllables with high exponent
        var firstChars = new List<char>();
        for (int i = 0; i < 100; i++)
        {
            var gen = new SyllableGenerator(seed: i);
            var syllable = gen.GenerateSyllableWeighted("C", phonemeMap, exponent: 3.0);
            firstChars.Add(syllable[0]);
        }

        // Assert - Should have more 'a', 'b', 'c' than 'n', 'o', 'p'
        var earlyCount = firstChars.Count(c => "abc".Contains(c));
        var lateCount = firstChars.Count(c => "nop".Contains(c));
        Assert.True(earlyCount > lateCount, 
            $"High exponent should bias toward beginning. Early: {earlyCount}, Late: {lateCount}");
    }

    [Fact]
    public void IsValidPattern_WithValidPattern_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(SyllableGenerator.IsValidPattern("CV"));
        Assert.True(SyllableGenerator.IsValidPattern("CVC"));
        Assert.True(SyllableGenerator.IsValidPattern("CV?"));
        Assert.True(SyllableGenerator.IsValidPattern("CLVS"));
        Assert.True(SyllableGenerator.IsValidPattern("V"));
    }

    [Fact]
    public void IsValidPattern_WithInvalidPattern_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(SyllableGenerator.IsValidPattern("CVX")); // X is invalid
        Assert.False(SyllableGenerator.IsValidPattern("123")); // Numbers invalid
        Assert.False(SyllableGenerator.IsValidPattern("")); // Empty
        Assert.False(SyllableGenerator.IsValidPattern(null!)); // Null
    }

    [Fact]
    public void ExpandPattern_WithNoOptionals_ReturnsSinglePattern()
    {
        // Act
        var expanded = SyllableGenerator.ExpandPattern("CVC");

        // Assert
        Assert.Single(expanded);
        Assert.Contains("CVC", expanded);
    }

    [Fact]
    public void ExpandPattern_WithOneOptional_ReturnsTwoPatterns()
    {
        // Act
        var expanded = SyllableGenerator.ExpandPattern("CV?");

        // Assert
        Assert.Equal(2, expanded.Count);
        Assert.Contains("CV", expanded);
        Assert.Contains("C", expanded);
    }

    [Fact]
    public void ExpandPattern_WithMultipleOptionals_ReturnsAllCombinations()
    {
        // Act
        var expanded = SyllableGenerator.ExpandPattern("C?V?");

        // Assert
        Assert.True(expanded.Count >= 2);
        // Should include various combinations
    }

    [Theory]
    [InlineData("CV", "ptkmnls", "aeiou")]
    [InlineData("CVC", "ptk", "aei")]
    [InlineData("V", "x", "aeiou")]
    public void GenerateSyllable_WithVariousPatterns_GeneratesValidOutput(
        string pattern, string consonants, string vowels)
    {
        // Arrange
        var generator = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', consonants },
            { 'V', vowels }
        };

        // Act
        var syllable = generator.GenerateSyllable(pattern, phonemeMap);

        // Assert
        Assert.NotEmpty(syllable);
        Assert.True(syllable.Length <= pattern.Replace("?", "").Length);
    }
}
