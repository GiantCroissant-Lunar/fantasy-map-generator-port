using FantasyNameGenerator.Grammar;
using Xunit;

namespace FantasyNameGenerator.Tests.Grammar;

public class RulePackTests
{
    [Fact]
    public void AddRule_StoresRule()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        
        pack.AddRule("test", "value");
        
        Assert.True(pack.HasTag("test"));
    }

    [Fact]
    public void Expand_SimpleTag_ReplacesWithRule()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRule("word", "hello");
        
        var result = pack.Expand("[word]");
        
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Expand_MultipleOccurrences_ReplacesAll()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRule("word", "test");
        
        var result = pack.Expand("[word] [word]");
        
        Assert.Equal("test test", result);
    }

    [Fact]
    public void Expand_NestedTags_ExpandsRecursively()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRule("outer", "[inner]");
        pack.AddRule("inner", "value");
        
        var result = pack.Expand("[outer]");
        
        Assert.Equal("value", result);
    }

    [Fact]
    public void Expand_UnknownTag_LeavesAsIs()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        
        var result = pack.Expand("[unknown]");
        
        Assert.Equal("[unknown]", result);
    }

    [Fact]
    public void Expand_MultipleRules_PicksRandomly()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRules("word", "one", "two", "three");
        
        var results = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            results.Add(pack.Expand("[word]"));
        }
        
        // Should have picked multiple different values
        Assert.True(results.Count > 1);
        Assert.All(results, r => Assert.Contains(r, new[] { "one", "two", "three" }));
    }

    [Fact]
    public void Expand_ComplexTemplate_ExpandsCorrectly()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRule("adj", "big");
        pack.AddRule("noun", "house");
        
        var result = pack.Expand("The [adj] [noun]");
        
        Assert.Equal("The big house", result);
    }

    [Fact]
    public void Expand_MaxDepth_PreventsInfiniteRecursion()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRule("loop", "[loop]");
        
        var result = pack.Expand("[loop]", maxDepth: 5);
        
        // Should stop after max depth
        Assert.NotNull(result);
    }

    [Fact]
    public void GetRules_ReturnsAllRulesForTag()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        pack.AddRules("test", "a", "b", "c");
        
        var rules = pack.GetRules("test");
        
        Assert.Equal(3, rules.Count);
        Assert.Contains("a", rules);
        Assert.Contains("b", rules);
        Assert.Contains("c", rules);
    }

    [Fact]
    public void GetRules_UnknownTag_ReturnsEmpty()
    {
        var random = new Random(42);
        var pack = new RulePack(random);
        
        var rules = pack.GetRules("unknown");
        
        Assert.Empty(rules);
    }

    [Fact]
    public void Expand_IsDeterministic_WithSameSeed()
    {
        var pack1 = new RulePack(new Random(12345));
        var pack2 = new RulePack(new Random(12345));
        
        pack1.AddRules("word", "one", "two", "three");
        pack2.AddRules("word", "one", "two", "three");
        
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(
                pack1.Expand("[word]"),
                pack2.Expand("[word]")
            );
        }
    }
}
