using FantasyNameGenerator.Phonotactics;

namespace FantasyNameGenerator.Tests.Phonotactics;

public class PhonotacticTemplatesTests
{
    [Theory]
    [InlineData("germanic")]
    [InlineData("romance")]
    [InlineData("slavic")]
    [InlineData("elvish")]
    [InlineData("dwarvish")]
    [InlineData("orcish")]
    public void GetTemplate_ReturnsValidRules(string templateName)
    {
        // Act
        var rules = PhonotacticTemplates.GetTemplate(templateName);

        // Assert
        Assert.NotNull(rules);
        Assert.NotEmpty(rules.AllowedStructures);
        Assert.True(rules.MaxConsonantCluster > 0);
        Assert.True(rules.MaxVowelCluster > 0);
        Assert.True(rules.MinSyllables > 0);
        Assert.True(rules.MaxSyllables >= rules.MinSyllables);
    }

    [Fact]
    public void GetTemplate_CaseInsensitive()
    {
        // Act
        var lower = PhonotacticTemplates.GetTemplate("germanic");
        var upper = PhonotacticTemplates.GetTemplate("GERMANIC");
        var mixed = PhonotacticTemplates.GetTemplate("GeRmAnIc");

        // Assert
        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.NotNull(mixed);
    }

    [Fact]
    public void GetTemplate_InvalidName_ReturnsNull()
    {
        // Act
        var result = PhonotacticTemplates.GetTemplate("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Germanic_AllowsComplexClusters()
    {
        // Act
        var rules = PhonotacticTemplates.Germanic();

        // Assert
        Assert.True(rules.MaxConsonantCluster >= 3);
        Assert.Contains("CCVC", rules.AllowedStructures);
        Assert.NotEmpty(rules.AllowedOnsets);
        Assert.Contains("st", rules.AllowedOnsets);
    }

    [Fact]
    public void Romance_HasSimpleStructure()
    {
        // Act
        var rules = PhonotacticTemplates.Romance();

        // Assert
        Assert.Contains("CV", rules.AllowedStructures);
        Assert.True(rules.MaxConsonantCluster <= 2);
        Assert.True(rules.MinSyllables >= 2); // Romance prefers longer words
    }

    [Fact]
    public void Slavic_AllowsVeryComplexClusters()
    {
        // Act
        var rules = PhonotacticTemplates.Slavic();

        // Assert
        Assert.True(rules.MaxConsonantCluster >= 4);
        Assert.Contains("CCCVC", rules.AllowedStructures);
        Assert.False(rules.EnforceSonoritySequencing); // Slavic violates sonority
    }

    [Fact]
    public void Elvish_HasSimpleFlowingStructure()
    {
        // Act
        var rules = PhonotacticTemplates.Elvish();

        // Assert
        Assert.True(rules.MaxConsonantCluster <= 1); // Very simple
        Assert.Contains("CV", rules.AllowedStructures);
        Assert.True(rules.EnforceSonoritySequencing);
        // Should forbid harsh sounds
        Assert.Contains(rules.ForbiddenSequences, seq => seq.Contains("θθ") || seq.Contains("ww"));
    }

    [Fact]
    public void Dwarvish_HasHarshCharacteristics()
    {
        // Act
        var rules = PhonotacticTemplates.Dwarvish();

        // Assert
        Assert.Contains("CCVC", rules.AllowedStructures);
        Assert.True(rules.MaxConsonantCluster >= 3);
        // Should forbid soft sounds
        Assert.Contains(rules.ForbiddenSequences, seq => seq.Contains("θ") || seq.Contains("w"));
    }

    [Fact]
    public void Orcish_HasSimpleBrutalStructure()
    {
        // Act
        var rules = PhonotacticTemplates.Orcish();

        // Assert
        Assert.Contains("CVC", rules.AllowedStructures);
        Assert.True(rules.MaxSyllables <= 2); // Short words
        Assert.True(rules.MaxVowelCluster <= 1); // No diphthongs
        // Should forbid soft/liquid sounds
        Assert.Contains(rules.ForbiddenSequences, seq => seq.Contains("l") || seq.Contains("w"));
    }

    [Fact]
    public void GetAllTemplates_ReturnsAllSixTemplates()
    {
        // Act
        var templates = PhonotacticTemplates.GetAllTemplates();

        // Assert
        Assert.Equal(6, templates.Count);
        Assert.Contains("germanic", templates.Keys);
        Assert.Contains("romance", templates.Keys);
        Assert.Contains("slavic", templates.Keys);
        Assert.Contains("elvish", templates.Keys);
        Assert.Contains("dwarvish", templates.Keys);
        Assert.Contains("orcish", templates.Keys);
    }

    [Fact]
    public void AllTemplates_HaveConsistentProperties()
    {
        // Act
        var templates = PhonotacticTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates.Values)
        {
            var rules = template();
            
            // All templates should have valid ranges
            Assert.True(rules.MaxConsonantCluster > 0);
            Assert.True(rules.MaxVowelCluster > 0);
            Assert.True(rules.MinSyllables > 0);
            Assert.True(rules.MaxSyllables >= rules.MinSyllables);
            
            // All should have at least one allowed structure
            Assert.NotEmpty(rules.AllowedStructures);
        }
    }

    [Fact]
    public void AllTemplates_CanBeCloned()
    {
        // Act
        var templates = PhonotacticTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates.Values)
        {
            var original = template();
            var clone = original.Clone();
            
            Assert.NotNull(clone);
            Assert.NotSame(original, clone);
            Assert.Equal(original.MaxConsonantCluster, clone.MaxConsonantCluster);
        }
    }

    [Fact]
    public void Germanic_HasRealisticOnsets()
    {
        // Act
        var rules = PhonotacticTemplates.Germanic();

        // Assert
        // Common Germanic onsets
        Assert.Contains("pl", rules.AllowedOnsets);
        Assert.Contains("tr", rules.AllowedOnsets);
        Assert.Contains("st", rules.AllowedOnsets);
        Assert.Contains("ʃr", rules.AllowedOnsets); // "shr"
    }

    [Fact]
    public void Romance_HasRealisticCodas()
    {
        // Act
        var rules = PhonotacticTemplates.Romance();

        // Assert
        // Romance typically has simple codas
        Assert.Contains("n", rules.AllowedCodas);
        Assert.Contains("r", rules.AllowedCodas);
        Assert.Contains("s", rules.AllowedCodas);
    }
}
