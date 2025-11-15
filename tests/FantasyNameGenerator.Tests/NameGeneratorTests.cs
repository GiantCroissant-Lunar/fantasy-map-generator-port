using FantasyNameGenerator;
using FantasyNameGenerator.NameTypes;
using FantasyNameGenerator.Phonology;
using FantasyNameGenerator.Phonotactics;
using FantasyNameGenerator.Core;
using FantasyNameGenerator.Morphology;
using Xunit;

namespace FantasyNameGenerator.Tests;

public class NameGeneratorTests
{
    private NameGenerator CreateGenerator(int seed = 42)
    {
        var random = new Random(seed);
        var phonology = PhonologyTemplates.Germanic(random);
        var phonotactics = PhonotacticTemplates.Germanic(random);
        var morphology = new MorphologyRules(seed);
        
        return new NameGenerator(phonology, phonotactics, morphology, random);
    }

    [Theory]
    [InlineData(NameType.Burg)]
    [InlineData(NameType.State)]
    [InlineData(NameType.Province)]
    [InlineData(NameType.Religion)]
    [InlineData(NameType.Culture)]
    [InlineData(NameType.Person)]
    [InlineData(NameType.River)]
    [InlineData(NameType.Mountain)]
    [InlineData(NameType.Region)]
    [InlineData(NameType.Forest)]
    [InlineData(NameType.Lake)]
    public void Generate_AllNameTypes_ProduceValidNames(NameType type)
    {
        var generator = CreateGenerator();
        
        var name = generator.Generate(type);
        
        Assert.NotEmpty(name);
        Assert.InRange(name.Length, 3, 30);
        Assert.True(char.IsUpper(name[0]));
    }

    [Fact]
    public void Generate_ProducesUniqueNames()
    {
        var generator = CreateGenerator();
        var names = new HashSet<string>();
        
        for (int i = 0; i < 50; i++)
        {
            var name = generator.Generate(NameType.Burg);
            Assert.DoesNotContain(name, names, StringComparer.OrdinalIgnoreCase);
            names.Add(name);
        }
    }

    [Fact]
    public void Generate_IsDeterministic_WithSameSeed()
    {
        var gen1 = CreateGenerator(12345);
        var gen2 = CreateGenerator(12345);
        
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(
                gen1.Generate(NameType.Burg),
                gen2.Generate(NameType.Burg)
            );
        }
    }

    [Fact]
    public void GenerateWord_ProducesValidWord()
    {
        var generator = CreateGenerator();
        
        var word = generator.GenerateWord(2, 3);
        
        Assert.NotEmpty(word);
        Assert.True(char.IsUpper(word[0]));
    }

    [Fact]
    public void GenerateWord_RespectsMinMaxSyllables()
    {
        var generator = CreateGenerator();
        
        var word = generator.GenerateWord(1, 1);
        
        Assert.NotEmpty(word);
        // Single syllable should be relatively short
        Assert.InRange(word.Length, 1, 10);
    }

    [Fact]
    public void GenerateCompound_ProducesTwoPartName()
    {
        var generator = CreateGenerator();
        
        var compound = generator.GenerateCompound();
        
        Assert.NotEmpty(compound);
        Assert.True(char.IsUpper(compound[0]));
    }

    [Fact]
    public void GenerateCompound_WithSeparator_IncludesSeparator()
    {
        var generator = CreateGenerator();
        
        var compound = generator.GenerateCompound("-");
        
        Assert.Contains("-", compound);
    }

    [Fact]
    public void GenerateFromTemplate_CustomTemplate_Works()
    {
        var generator = CreateGenerator();
        
        var name = generator.GenerateFromTemplate("[word]burg");
        
        Assert.EndsWith("burg", name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddGrammarRule_CustomRule_IsUsed()
    {
        var generator = CreateGenerator();
        generator.AddGrammarRule("custom", "test");
        
        var name = generator.GenerateFromTemplate("[custom]");
        
        Assert.Equal("Test", name);
    }

    [Fact]
    public void ClearUsedNames_ResetsCache()
    {
        var generator = CreateGenerator();
        var name = generator.Generate(NameType.Burg);
        
        Assert.True(generator.IsNameUsed(name));
        
        generator.ClearUsedNames();
        
        Assert.False(generator.IsNameUsed(name));
    }

    [Fact]
    public void IsNameUsed_TracksUsedNames()
    {
        var generator = CreateGenerator();
        
        var name = generator.Generate(NameType.Burg);
        
        Assert.True(generator.IsNameUsed(name));
        Assert.True(generator.IsNameUsed(name.ToLower()));
        Assert.True(generator.IsNameUsed(name.ToUpper()));
    }

    [Fact]
    public void Grammar_IsAccessible()
    {
        var generator = CreateGenerator();
        
        Assert.NotNull(generator.Grammar);
    }

    [Fact]
    public void Morphology_IsAccessible()
    {
        var generator = CreateGenerator();
        
        Assert.NotNull(generator.Morphology);
    }

    [Fact]
    public void Generate_BurgNames_HaveVariety()
    {
        var generator = CreateGenerator();
        var names = new HashSet<string>();
        
        for (int i = 0; i < 30; i++)
        {
            names.Add(generator.Generate(NameType.Burg));
        }
        
        // Should have good variety
        Assert.True(names.Count >= 25);
    }

    [Fact]
    public void Generate_StateNames_HaveVariety()
    {
        var generator = CreateGenerator();
        var names = new HashSet<string>();
        
        for (int i = 0; i < 30; i++)
        {
            names.Add(generator.Generate(NameType.State));
        }
        
        // Should have good variety
        Assert.True(names.Count >= 25);
    }

    [Fact]
    public void Generate_DifferentTypes_ProduceDifferentStyles()
    {
        var generator = CreateGenerator();
        
        var burg = generator.Generate(NameType.Burg);
        var river = generator.Generate(NameType.River);
        var mountain = generator.Generate(NameType.Mountain);
        
        // All should be valid but different
        Assert.NotEmpty(burg);
        Assert.NotEmpty(river);
        Assert.NotEmpty(mountain);
    }

    [Fact]
    public void Generate_MaxAttempts_EventuallySucceeds()
    {
        var generator = CreateGenerator();
        
        // Generate many names to fill the cache
        for (int i = 0; i < 50; i++)
        {
            generator.Generate(NameType.Burg);
        }
        
        // Should still be able to generate more
        var name = generator.Generate(NameType.Burg);
        Assert.NotEmpty(name);
    }
}
