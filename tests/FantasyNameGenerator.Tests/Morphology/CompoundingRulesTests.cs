using FantasyNameGenerator.Morphology;

namespace FantasyNameGenerator.Tests.Morphology;

public class CompoundingRulesTests
{
    [Fact]
    public void CompoundingRules_HasDefaultValues()
    {
        // Arrange & Act
        var rules = new CompoundingRules();

        // Assert
        Assert.Equal(" ", rules.Joiner);
        Assert.True(rules.HeadFirst);
        Assert.Equal(0.3, rules.CompoundProbability);
        Assert.Equal(2, rules.MaxCompoundLength);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new CompoundingRules
        {
            Joiner = "-",
            HeadFirst = false,
            CompoundProbability = 0.5,
            MaxCompoundLength = 3
        };

        // Act
        var clone = original.Clone();
        clone.Joiner = "_";
        clone.HeadFirst = true;

        // Assert
        Assert.Equal("-", original.Joiner);
        Assert.False(original.HeadFirst);
        Assert.Equal("_", clone.Joiner);
        Assert.True(clone.HeadFirst);
    }
}
