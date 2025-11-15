using FantasyNameGenerator.Configuration;
using FantasyNameGenerator.Phonology;
using Xunit;

namespace FantasyNameGenerator.Tests.Configuration;

public class LanguageTemplateLoaderTests
{
    [Theory]
    [InlineData("germanic")]
    [InlineData("romance")]
    [InlineData("slavic")]
    [InlineData("elvish")]
    [InlineData("dwarvish")]
    [InlineData("orcish")]
    public void LoadBuiltIn_LoadsAllDefaultTemplates(string templateName)
    {
        // Act
        var phonology = LanguageTemplateLoader.LoadBuiltIn(templateName);

        // Assert
        Assert.NotNull(phonology);
        Assert.Equal(templateName.ToLowerInvariant(), phonology.Name.ToLowerInvariant());
        Assert.NotEmpty(phonology.Inventory.Consonants);
        Assert.NotEmpty(phonology.Inventory.Vowels);
    }

    [Fact]
    public void LoadBuiltIn_ReturnsNull_ForNonExistentTemplate()
    {
        // Act
        var phonology = LanguageTemplateLoader.LoadBuiltIn("nonexistent");

        // Assert
        Assert.Null(phonology);
    }

    [Fact]
    public void GetBuiltInTemplateNames_ReturnsAllTemplates()
    {
        // Act
        var names = LanguageTemplateLoader.GetBuiltInTemplateNames();

        // Assert
        Assert.NotEmpty(names);
        Assert.Contains("germanic", names);
        Assert.Contains("romance", names);
        Assert.Contains("slavic", names);
        Assert.Contains("elvish", names);
        Assert.Contains("dwarvish", names);
        Assert.Contains("orcish", names);
    }

    [Fact]
    public void LoadFromJson_ConvertsCorrectly()
    {
        // Arrange
        var json = @"{
            ""name"": ""TestLanguage"",
            ""inventory"": {
                ""consonants"": ""ptkbdg"",
                ""vowels"": ""aeiou"",
                ""liquids"": ""lr"",
                ""nasals"": ""mn""
            },
            ""orthography"": {
                ""p"": ""ph"",
                ""k"": ""c""
            }
        }";

        // Act
        var phonology = LanguageTemplateLoader.LoadFromJson(json);

        // Assert
        Assert.NotNull(phonology);
        Assert.Equal("TestLanguage", phonology.Name);
        Assert.Equal("ptkbdg", phonology.Inventory.Consonants);
        Assert.Equal("aeiou", phonology.Inventory.Vowels);
        Assert.Equal("lr", phonology.Inventory.Liquids);
        Assert.Equal("mn", phonology.Inventory.Nasals);
        Assert.Equal(2, phonology.Orthography.Count);
        Assert.Equal("ph", phonology.Orthography['p']);
        Assert.Equal("c", phonology.Orthography['k']);
    }

    [Fact]
    public void ConvertToJson_RoundTripsCorrectly()
    {
        // Arrange
        PhonologyTemplates.UseJsonTemplates = false;
        var original = PhonologyTemplates.Germanic();

        // Act
        var json = LanguageTemplateLoader.ConvertToJson(original);
        var jsonModel = LanguageTemplateLoader.ConvertFromJson(json);

        // Assert
        Assert.Equal(original.Name, jsonModel.Name);
        Assert.Equal(original.Inventory.Consonants, jsonModel.Inventory.Consonants);
        Assert.Equal(original.Inventory.Vowels, jsonModel.Inventory.Vowels);
        Assert.Equal(original.Orthography.Count, jsonModel.Orthography.Count);
    }

    [Fact]
    public void LoadFromJson_HandlesWeights()
    {
        // Arrange
        var json = @"{
            ""name"": ""TestLanguage"",
            ""inventory"": {
                ""consonants"": ""ptk"",
                ""vowels"": ""aei""
            },
            ""weights"": {
                ""p"": 0.5,
                ""t"": 0.8,
                ""k"": 0.3
            }
        }";

        // Act
        var phonology = LanguageTemplateLoader.LoadFromJson(json);

        // Assert
        Assert.Equal(0.5, phonology.GetWeight('p'));
        Assert.Equal(0.8, phonology.GetWeight('t'));
        Assert.Equal(0.3, phonology.GetWeight('k'));
    }

    [Fact]
    public void LoadFromJson_HandlesAllophones()
    {
        // Arrange
        var json = @"{
            ""name"": ""TestLanguage"",
            ""inventory"": {
                ""consonants"": ""td"",
                ""vowels"": ""aei""
            },
            ""allophones"": [
                {
                    ""phoneme"": ""t"",
                    ""allophone"": ""d"",
                    ""context"": ""V_V""
                }
            ]
        }";

        // Act
        var phonology = LanguageTemplateLoader.LoadFromJson(json);

        // Assert
        Assert.Single(phonology.Allophones);
        Assert.Equal('t', phonology.Allophones[0].Phoneme);
        Assert.Equal('d', phonology.Allophones[0].Allophone);
        Assert.Equal("V_V", phonology.Allophones[0].Context);
    }

    [Fact]
    public void LoadFromJson_MatchesHardcodedTemplates()
    {
        // Load from JSON
        PhonologyTemplates.UseJsonTemplates = true;
        var templateNames = new[] { "germanic", "romance", "slavic", "elvish", "dwarvish", "orcish" };
        
        foreach (var name in templateNames)
        {
            var jsonPhonology = LanguageTemplateLoader.LoadBuiltIn(name);
            Assert.NotNull(jsonPhonology);

            // Load hardcoded
            PhonologyTemplates.UseJsonTemplates = false;
            var hardcodedPhonology = PhonologyTemplates.GetTemplate(name);
            Assert.NotNull(hardcodedPhonology);

            // Compare
            Assert.Equal(hardcodedPhonology.Name.ToLowerInvariant(), jsonPhonology.Name.ToLowerInvariant());
            Assert.Equal(hardcodedPhonology.Inventory.Consonants, jsonPhonology.Inventory.Consonants);
            Assert.Equal(hardcodedPhonology.Inventory.Vowels, jsonPhonology.Inventory.Vowels);
        }
        
        PhonologyTemplates.UseJsonTemplates = true;
    }

    [Fact]
    public void LoadFromJson_HandlesPhonotactics()
    {
        // Arrange
        var json = @"{
            ""name"": ""TestLanguage"",
            ""inventory"": {
                ""consonants"": ""ptk"",
                ""vowels"": ""aei""
            },
            ""phonotactics"": {
                ""structures"": [""CV"", ""CVC""],
                ""forbiddenSequences"": [""pp"", ""tt""],
                ""maxConsonantCluster"": 2,
                ""minSyllables"": 1,
                ""maxSyllables"": 3,
                ""enforceSonoritySequencing"": true
            }
        }";

        // Act
        var templateJson = System.Text.Json.JsonSerializer.Deserialize<LanguageTemplateJson>(json);
        
        // Assert - verify phonotactics are in JSON model
        Assert.NotNull(templateJson);
        Assert.NotNull(templateJson.Phonotactics);
        Assert.Equal(2, templateJson.Phonotactics.Structures.Count);
        Assert.Equal(2, templateJson.Phonotactics.ForbiddenSequences.Count);
    }

    [Fact]
    public void ConvertPhonotacticsFromJson_ConvertsCorrectly()
    {
        // Arrange
        var json = new PhonotacticsJson
        {
            Structures = new List<string> { "CV", "CVC", "CCVC" },
            ForbiddenSequences = new List<string> { "pp", "tt" },
            AllowedOnsets = new List<string> { "pl", "tr" },
            MaxConsonantCluster = 3,
            MinSyllables = 1,
            MaxSyllables = 4,
            EnforceSonoritySequencing = true
        };

        // Act
        var rules = LanguageTemplateLoader.ConvertPhonotacticsFromJson(json);

        // Assert
        Assert.NotNull(rules);
        Assert.Equal(3, rules.AllowedStructures.Count);
        Assert.Contains("CV", rules.AllowedStructures);
        Assert.Equal(2, rules.ForbiddenSequences.Count);
        Assert.Equal(3, rules.MaxConsonantCluster);
        Assert.Equal(1, rules.MinSyllables);
        Assert.Equal(4, rules.MaxSyllables);
        Assert.True(rules.EnforceSonoritySequencing);
    }
}
