using FantasyNameGenerator.Core;
using FantasyNameGenerator.Grammar;
using FantasyNameGenerator.Morphology;
using FantasyNameGenerator.Phonology;
using FantasyNameGenerator.Phonotactics;
using Xunit;

namespace FantasyNameGenerator.Tests.Grammar;

public class GrammarEngineTests
{
    private GrammarEngine CreateEngine(int seed = 42)
    {
        var random = new Random(seed);
        var phonology = PhonologyTemplates.Germanic(random);
        var phonotactics = PhonotacticTemplates.Germanic(random);
        var morphology = new MorphologyRules(seed);
        
        return new GrammarEngine(morphology, random);
    }

    [Fact]
    public void Generate_SimpleTemplate_ReturnsName()
    {
        var engine = CreateEngine();
        engine.AddRule("test", "hello");
        
        var result = engine.Generate("[test]");
        
        Assert.Equal("Hello", result); // Capitalized
    }

    [Fact]
    public void Generate_DynamicRule_CallsGenerator()
    {
        var engine = CreateEngine();
        var callCount = 0;
        engine.AddRule("dynamic", () => { callCount++; return "test"; });
        
        var result = engine.Generate("[dynamic]");
        
        Assert.Equal("Test", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Generate_MixedRules_ExpandsBoth()
    {
        var engine = CreateEngine();
        engine.AddRule("static", "world");
        engine.AddRule("dynamic", () => "hello");
        
        var result = engine.Generate("[dynamic] [static]");
        
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void Generate_Capitalizes_FirstLetter()
    {
        var engine = CreateEngine();
        engine.AddRule("word", "test");
        
        var result = engine.Generate("[word]");
        
        Assert.True(char.IsUpper(result[0]));
    }

    [Fact]
    public void Generate_CleansUp_MultipleSpaces()
    {
        var engine = CreateEngine();
        engine.AddRule("word", "test");
        
        var result = engine.Generate("[word]  [word]");
        
        Assert.DoesNotContain("  ", result);
    }

    [Fact]
    public void AddRules_AddsMultipleTemplates()
    {
        var engine = CreateEngine();
        engine.AddRules("test", "one", "two", "three");
        
        var results = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            results.Add(engine.Generate("[test]"));
        }
        
        Assert.True(results.Count > 1);
    }

    [Fact]
    public void Generate_DefaultRules_WorkCorrectly()
    {
        var engine = CreateEngine();
        
        // Test [word] rule
        var word = engine.Generate("[word]");
        Assert.NotEmpty(word);
        Assert.True(char.IsUpper(word[0]));
        
        // Test [compound] rule
        var compound = engine.Generate("[compound]");
        Assert.NotEmpty(compound);
        
        // Test [name] rule
        var name = engine.Generate("[name]");
        Assert.NotEmpty(name);
    }

    [Fact]
    public void Generate_IsDeterministic_WithSameSeed()
    {
        var engine1 = CreateEngine(12345);
        var engine2 = CreateEngine(12345);
        
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(
                engine1.Generate("[word]"),
                engine2.Generate("[word]")
            );
        }
    }

    [Fact]
    public void RulePack_IsAccessible()
    {
        var engine = CreateEngine();
        
        Assert.NotNull(engine.RulePack);
    }

    [Fact]
    public void Generate_ComplexTemplate_ExpandsCorrectly()
    {
        var engine = CreateEngine();
        engine.AddRule("title", "Kingdom");
        engine.AddRule("place", "Angland");
        
        var result = engine.Generate("[title] of [place]");
        
        Assert.Contains("Kingdom", result);
        Assert.Contains("Angland", result);
    }

    [Fact]
    public void Generate_MultipleDynamicRules_AllExpand()
    {
        var engine = CreateEngine();
        var count1 = 0;
        var count2 = 0;
        
        engine.AddRule("d1", () => { count1++; return "a"; });
        engine.AddRule("d2", () => { count2++; return "b"; });
        
        var result = engine.Generate("[d1][d2]");
        
        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
    }
}
