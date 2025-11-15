using FantasyNameGenerator.Morphology;

namespace FantasyNameGenerator.Tests.Morphology;

public class AffixTests
{
    [Fact]
    public void Affix_CanBeCreated()
    {
        // Arrange & Act
        var affix = new Affix
        {
            Form = "ton",
            Meaning = "town",
            Frequency = 0.3,
            Position = AffixPosition.Suffix
        };

        // Assert
        Assert.Equal("ton", affix.Form);
        Assert.Equal("town", affix.Meaning);
        Assert.Equal(0.3, affix.Frequency);
        Assert.Equal(AffixPosition.Suffix, affix.Position);
    }

    [Theory]
    [InlineData(AffixPosition.Prefix)]
    [InlineData(AffixPosition.Suffix)]
    [InlineData(AffixPosition.Infix)]
    public void AffixPosition_AllValuesValid(AffixPosition position)
    {
        // Arrange & Act
        var affix = new Affix { Position = position };

        // Assert
        Assert.Equal(position, affix.Position);
    }
}
