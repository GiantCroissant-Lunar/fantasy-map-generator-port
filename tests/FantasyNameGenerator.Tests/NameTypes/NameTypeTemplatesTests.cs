using FantasyNameGenerator.NameTypes;
using Xunit;

namespace FantasyNameGenerator.Tests.NameTypes;

public class NameTypeTemplatesTests
{
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
    public void GetTemplates_AllTypes_ReturnTemplates(NameType type)
    {
        var templates = NameTypeTemplates.GetTemplates(type);
        
        Assert.NotEmpty(templates);
        Assert.All(templates, t => Assert.NotEmpty(t));
    }

    [Fact]
    public void GetTemplates_Burg_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Burg);
        
        Assert.True(templates.Length >= 5);
        Assert.Contains(templates, t => t.Contains("burg"));
        Assert.Contains(templates, t => t.Contains("ton"));
        Assert.Contains(templates, t => t.Contains("[word]"));
    }

    [Fact]
    public void GetTemplates_State_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.State);
        
        Assert.True(templates.Length >= 5);
        Assert.Contains(templates, t => t.Contains("Kingdom"));
        Assert.Contains(templates, t => t.Contains("Empire"));
        Assert.Contains(templates, t => t.Contains("land"));
    }

    [Fact]
    public void GetTemplates_Province_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Province);
        
        Assert.True(templates.Length >= 3);
        Assert.Contains(templates, t => t.Contains("shire"));
        Assert.Contains(templates, t => t.Contains("Province"));
    }

    [Fact]
    public void GetTemplates_Religion_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Religion);
        
        Assert.True(templates.Length >= 3);
        Assert.Contains(templates, t => t.Contains("Church"));
        Assert.Contains(templates, t => t.Contains("Faith"));
        Assert.Contains(templates, t => t.Contains("ism"));
    }

    [Fact]
    public void GetTemplates_Culture_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Culture);
        
        Assert.True(templates.Length >= 3);
        Assert.Contains(templates, t => t.Contains("ish"));
        Assert.Contains(templates, t => t.Contains("ian"));
    }

    [Fact]
    public void GetTemplates_River_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.River);
        
        Assert.True(templates.Length >= 3);
        Assert.Contains(templates, t => t.Contains("River"));
        Assert.Contains(templates, t => t.Contains("water"));
    }

    [Fact]
    public void GetTemplates_Mountain_HasVariety()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Mountain);
        
        Assert.True(templates.Length >= 3);
        Assert.Contains(templates, t => t.Contains("Mount"));
        Assert.Contains(templates, t => t.Contains("Peak"));
    }

    [Fact]
    public void GetTemplates_AllContainWordTag()
    {
        foreach (NameType type in Enum.GetValues<NameType>())
        {
            var templates = NameTypeTemplates.GetTemplates(type);
            
            // At least one template should contain [word]
            Assert.Contains(templates, t => t.Contains("[word]"));
        }
    }

    [Fact]
    public void GetTemplates_Person_IsSimple()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Person);
        
        // Person names should be simple
        Assert.All(templates, t => 
        {
            Assert.DoesNotContain("Kingdom", t);
            Assert.DoesNotContain("Empire", t);
        });
    }

    [Fact]
    public void GetTemplates_Forest_HasForestThemes()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Forest);
        
        Assert.Contains(templates, t => t.Contains("Forest") || t.Contains("wood") || t.Contains("Woods"));
    }

    [Fact]
    public void GetTemplates_Lake_HasWaterThemes()
    {
        var templates = NameTypeTemplates.GetTemplates(NameType.Lake);
        
        Assert.Contains(templates, t => t.Contains("Lake") || t.Contains("mere") || t.Contains("water"));
    }
}
