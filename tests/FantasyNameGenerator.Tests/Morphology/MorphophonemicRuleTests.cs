using FantasyNameGenerator.Morphology;

namespace FantasyNameGenerator.Tests.Morphology;

public class MorphophonemicRuleTests
{
    [Fact]
    public void Apply_WithMatchingPattern_AppliesReplacement()
    {
        // Arrange
        var rule = new MorphophonemicRule
        {
            Pattern = "s$",      // Ends with 's'
            Replacement = "es"   // Replace with 'es'
        };

        // Act
        var result = rule.Apply("bus");

        // Assert
        Assert.Equal("bues", result);
    }

    [Fact]
    public void Apply_WithNonMatchingPattern_ReturnsUnchanged()
    {
        // Arrange
        var rule = new MorphophonemicRule
        {
            Pattern = "x$",
            Replacement = "es"
        };

        // Act
        var result = rule.Apply("cat");

        // Assert
        Assert.Equal("cat", result);
    }

    [Fact]
    public void Apply_WithEmptyPattern_ReturnsUnchanged()
    {
        // Arrange
        var rule = new MorphophonemicRule
        {
            Pattern = "",
            Replacement = "es"
        };

        // Act
        var result = rule.Apply("word");

        // Assert
        Assert.Equal("word", result);
    }

    [Fact]
    public void Apply_WithInvalidRegex_ReturnsUnchanged()
    {
        // Arrange
        var rule = new MorphophonemicRule
        {
            Pattern = "[invalid(",
            Replacement = "x"
        };

        // Act
        var result = rule.Apply("word");

        // Assert
        Assert.Equal("word", result);
    }

    [Fact]
    public void Apply_WithEmptyWord_ReturnsEmpty()
    {
        // Arrange
        var rule = new MorphophonemicRule
        {
            Pattern = "s$",
            Replacement = "es"
        };

        // Act
        var result = rule.Apply("");

        // Assert
        Assert.Empty(result);
    }
}
