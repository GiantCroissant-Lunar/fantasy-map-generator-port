using FantasyNameGenerator.Core;
using FantasyNameGenerator.Morphology;

namespace FantasyNameGenerator.Tests.Morphology;

public class MorphologyRulesTests
{
    [Fact]
    public void GetOrCreateMorpheme_FirstCall_CreatesNewMorpheme()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345);
        var syllableGen = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "aei" }
        };

        // Act
        var morpheme = rules.GetOrCreateMorpheme("water", syllableGen, phonemeMap, "CV");

        // Assert
        Assert.NotEmpty(morpheme);
        Assert.Single(rules.Morphemes["water"]);
    }

    [Fact]
    public void GetOrCreateMorpheme_MultipleCalls_MayReuseOrCreate()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345);
        var syllableGen = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "aei" }
        };

        // Act - Generate multiple morphemes
        var morphemes = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            morphemes.Add(rules.GetOrCreateMorpheme("water", syllableGen, phonemeMap, "CV"));
        }

        // Assert - Should have created some morphemes
        Assert.True(rules.Morphemes["water"].Count > 0);
    }

    [Fact]
    public void GetOrCreateMorpheme_DifferentKeys_CreatesSeparateMorphemes()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345);
        var syllableGen = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "aei" }
        };

        // Act
        var water = rules.GetOrCreateMorpheme("water", syllableGen, phonemeMap, "CV");
        var fire = rules.GetOrCreateMorpheme("fire", syllableGen, phonemeMap, "CV");

        // Assert
        Assert.Contains("water", rules.Morphemes.Keys);
        Assert.Contains("fire", rules.Morphemes.Keys);
    }

    [Fact]
    public void ApplyAffixes_WithPrefix_AddsPrefix()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Prefixes = new List<Affix>
            {
                new Affix { Form = "new", Frequency = 1.0 }
            }
        };

        // Act
        var result = rules.ApplyAffixes("town");

        // Assert
        Assert.StartsWith("new", result);
    }

    [Fact]
    public void ApplyAffixes_WithSuffix_AddsSuffix()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Suffixes = new List<Affix>
            {
                new Affix { Form = "ton", Frequency = 1.0 }
            }
        };

        // Act
        var result = rules.ApplyAffixes("wash");

        // Assert
        Assert.EndsWith("ton", result);
    }

    [Fact]
    public void ApplyAffixes_WithLowFrequency_MayNotApply()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Suffixes = new List<Affix>
            {
                new Affix { Form = "ton", Frequency = 0.0 } // Never applies
            }
        };

        // Act
        var result = rules.ApplyAffixes("wash");

        // Assert
        Assert.Equal("wash", result);
    }

    [Fact]
    public void CreateCompound_WithTwoWords_JoinsThem()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules { Joiner = " " }
        };
        var words = new List<string> { "black", "bird" };

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Equal("black bird", result);
    }

    [Fact]
    public void CreateCompound_WithHyphenJoiner_UsesHyphen()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules { Joiner = "-" }
        };
        var words = new List<string> { "black", "bird" };

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Equal("black-bird", result);
    }

    [Fact]
    public void CreateCompound_WithHeadFinal_ReversesOrder()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules 
            { 
                Joiner = " ",
                HeadFirst = false 
            }
        };
        var words = new List<string> { "black", "bird" };

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Equal("bird black", result);
    }

    [Fact]
    public void CreateCompound_WithMaxLength_LimitsWords()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules 
            { 
                Joiner = " ",
                MaxCompoundLength = 2
            }
        };
        var words = new List<string> { "one", "two", "three", "four" };

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Equal("one two", result);
    }

    [Fact]
    public void CreateCompound_WithMorphophonemicRules_AppliesRules()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules { Joiner = "" },
            MorphophonemicRules = new List<MorphophonemicRule>
            {
                new MorphophonemicRule 
                { 
                    Pattern = "ss", 
                    Replacement = "s" 
                }
            }
        };
        var words = new List<string> { "glas", "shard" };

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Equal("glashard", result); // "ss" → "s"
    }

    [Fact]
    public void CreateCompound_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345);
        var words = new List<string>();

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CreateCompound_WithSingleWord_ReturnsThatWord()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345);
        var words = new List<string> { "alone" };

        // Act
        var result = rules.CreateCompound(words);

        // Assert
        Assert.Equal("alone", result);
    }

    [Fact]
    public void ShouldCompound_WithHighProbability_ReturnsTrue()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules { CompoundProbability = 1.0 }
        };

        // Act
        var result = rules.ShouldCompound();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldCompound_WithZeroProbability_ReturnsFalse()
    {
        // Arrange
        var rules = new MorphologyRules(seed: 12345)
        {
            Compounding = new CompoundingRules { CompoundProbability = 0.0 }
        };

        // Act
        var result = rules.ShouldCompound();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new MorphologyRules(seed: 12345);
        original.Morphemes["test"] = new List<string> { "abc" };
        original.Prefixes.Add(new Affix { Form = "pre" });
        original.Suffixes.Add(new Affix { Form = "suf" });

        // Act
        var clone = original.Clone(newSeed: 54321);
        clone.Morphemes["test"].Add("xyz");
        clone.Prefixes.Add(new Affix { Form = "new" });

        // Assert
        Assert.Single(original.Morphemes["test"]);
        Assert.Single(original.Prefixes);
        Assert.Equal(2, clone.Morphemes["test"].Count);
        Assert.Equal(2, clone.Prefixes.Count);
    }

    [Fact]
    public void GetOrCreateMorpheme_WithSameSeed_ProducesSameResults()
    {
        // Arrange
        var rules1 = new MorphologyRules(seed: 12345);
        var rules2 = new MorphologyRules(seed: 12345);
        var syllableGen1 = new SyllableGenerator(seed: 12345);
        var syllableGen2 = new SyllableGenerator(seed: 12345);
        var phonemeMap = new Dictionary<char, string>
        {
            { 'C', "ptk" },
            { 'V', "aei" }
        };

        // Act
        var morpheme1 = rules1.GetOrCreateMorpheme("water", syllableGen1, phonemeMap, "CV");
        var morpheme2 = rules2.GetOrCreateMorpheme("water", syllableGen2, phonemeMap, "CV");

        // Assert
        Assert.Equal(morpheme1, morpheme2);
    }
}
