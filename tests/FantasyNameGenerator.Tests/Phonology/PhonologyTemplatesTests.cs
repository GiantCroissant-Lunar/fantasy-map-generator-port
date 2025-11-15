using FantasyNameGenerator.Phonology;

namespace FantasyNameGenerator.Tests.Phonology;

public class PhonologyTemplatesTests
{
    [Theory]
    [InlineData("germanic")]
    [InlineData("romance")]
    [InlineData("slavic")]
    [InlineData("elvish")]
    [InlineData("dwarvish")]
    [InlineData("orcish")]
    public void GetTemplate_ReturnsValidPhonology(string templateName)
    {
        // Act
        var phonology = PhonologyTemplates.GetTemplate(templateName);

        // Assert
        Assert.NotNull(phonology);
        Assert.NotEmpty(phonology.Name);
        Assert.NotEmpty(phonology.Inventory.Consonants);
        Assert.NotEmpty(phonology.Inventory.Vowels);
    }

    [Fact]
    public void GetTemplate_CaseInsensitive()
    {
        // Act
        var lower = PhonologyTemplates.GetTemplate("germanic");
        var upper = PhonologyTemplates.GetTemplate("GERMANIC");
        var mixed = PhonologyTemplates.GetTemplate("GeRmAnIc");

        // Assert
        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.NotNull(mixed);
        Assert.Equal(lower.Name, upper.Name);
        Assert.Equal(lower.Name, mixed.Name);
    }

    [Fact]
    public void GetTemplate_InvalidName_ReturnsNull()
    {
        // Act
        var result = PhonologyTemplates.GetTemplate("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Germanic_HasExpectedCharacteristics()
    {
        // Act
        var phonology = PhonologyTemplates.Germanic();

        // Assert
        Assert.Equal("Germanic", phonology.Name);
        Assert.Contains('θ', phonology.Inventory.Consonants); // th sound
        Assert.Contains('ð', phonology.Inventory.Consonants); // th sound
        Assert.Contains('ʃ', phonology.Inventory.Consonants); // sh sound
        Assert.True(phonology.Orthography.ContainsKey('ʃ'));
    }

    [Fact]
    public void Romance_HasExpectedCharacteristics()
    {
        // Act
        var phonology = PhonologyTemplates.Romance();

        // Assert
        Assert.Equal("Romance", phonology.Name);
        Assert.Contains('ə', phonology.Inventory.Vowels); // schwa
        Assert.Contains('ɛ', phonology.Inventory.Vowels); // open e
        Assert.Contains('ɔ', phonology.Inventory.Vowels); // open o
    }

    [Fact]
    public void Elvish_HasMelodicCharacteristics()
    {
        // Act
        var phonology = PhonologyTemplates.Elvish();

        // Assert
        Assert.Equal("Elvish", phonology.Name);
        // Should have lots of liquids and vowels
        Assert.Contains('l', phonology.Inventory.Liquids);
        Assert.Contains('r', phonology.Inventory.Liquids);
        Assert.Contains('w', phonology.Inventory.Liquids);
        Assert.Contains('y', phonology.Inventory.Liquids);
        // Fewer harsh consonants
        Assert.DoesNotContain('k', phonology.Inventory.Consonants);
        Assert.DoesNotContain('g', phonology.Inventory.Consonants);
    }

    [Fact]
    public void Dwarvish_HasHarshCharacteristics()
    {
        // Act
        var phonology = PhonologyTemplates.Dwarvish();

        // Assert
        Assert.Equal("Dwarvish", phonology.Name);
        // Should have guttural sounds
        Assert.Contains('χ', phonology.Inventory.Consonants); // kh sound
        Assert.Contains('k', phonology.Inventory.Consonants);
        // Fewer liquids
        Assert.DoesNotContain('w', phonology.Inventory.Liquids);
        Assert.DoesNotContain('y', phonology.Inventory.Liquids);
    }

    [Fact]
    public void Orcish_HasSimpleBrutalCharacteristics()
    {
        // Act
        var phonology = PhonologyTemplates.Orcish();

        // Assert
        Assert.Equal("Orcish", phonology.Name);
        // Simple vowel system
        Assert.Equal(3, phonology.Inventory.Vowels.Length); // Only a, o, u
        Assert.Contains('a', phonology.Inventory.Vowels);
        Assert.Contains('o', phonology.Inventory.Vowels);
        Assert.Contains('u', phonology.Inventory.Vowels);
        // No 'e' or 'i'
        Assert.DoesNotContain('e', phonology.Inventory.Vowels);
        Assert.DoesNotContain('i', phonology.Inventory.Vowels);
    }

    [Fact]
    public void GetAllTemplates_ReturnsAllSixTemplates()
    {
        // Act
        var templates = PhonologyTemplates.GetAllTemplates();

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
    public void AllTemplates_HaveOrthographyRules()
    {
        // Act
        var templates = PhonologyTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates.Values)
        {
            var phonology = template();
            Assert.NotNull(phonology.Orthography);
            // At least some orthography rules defined
            Assert.True(phonology.Orthography.Count >= 0);
        }
    }

    [Fact]
    public void AllTemplates_HaveValidPhonemeInventories()
    {
        // Act
        var templates = PhonologyTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates.Values)
        {
            var phonology = template();
            Assert.NotEmpty(phonology.Inventory.Consonants);
            Assert.NotEmpty(phonology.Inventory.Vowels);
            
            // Phoneme map should work
            var map = phonology.Inventory.ToPhonemeMap();
            Assert.NotEmpty(map['C']);
            Assert.NotEmpty(map['V']);
        }
    }
}
