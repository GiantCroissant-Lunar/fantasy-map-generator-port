using FantasyNameGenerator.Configuration;
using Xunit;

namespace FantasyNameGenerator.Tests.Configuration;

public class LanguageTemplateValidatorTests
{
    [Fact]
    public void Validate_ValidTemplate_ReturnsSuccess()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "TestLanguage",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "ptkbdg",
                Vowels = "aeiou"
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "ptkbdg",
                Vowels = "aeiou"
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must have a name", result.Errors[0]);
    }

    [Fact]
    public void Validate_MissingConsonants_ReturnsError()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "TestLanguage",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "",
                Vowels = "aeiou"
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must have consonants", result.Errors[0]);
    }

    [Fact]
    public void Validate_MissingVowels_ReturnsError()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "TestLanguage",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "ptkbdg",
                Vowels = ""
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must have vowels", result.Errors[0]);
    }

    [Fact]
    public void Validate_FewPhonemes_ReturnsWarning()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "TestLanguage",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "pt",
                Vowels = "ae"
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void Validate_InvalidWeight_ReturnsError()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "TestLanguage",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "ptk",
                Vowels = "aei"
            },
            Weights = new Dictionary<string, double>
            {
                { "p", 1.5 }
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must be between 0.0 and 1.0", result.Errors[0]);
    }

    [Fact]
    public void Validate_MultiCharacterPhoneme_ReturnsError()
    {
        // Arrange
        var template = new LanguageTemplateJson
        {
            Name = "TestLanguage",
            Inventory = new PhonemeInventoryJson
            {
                Consonants = "ptk",
                Vowels = "aei"
            },
            Weights = new Dictionary<string, double>
            {
                { "ch", 0.5 }
            }
        };

        // Act
        var result = LanguageTemplateValidator.Validate(template);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must be a single character", result.Errors[0]);
    }

    [Fact]
    public void Validate_BuiltInTemplates_AllValid()
    {
        // Arrange
        var templateNames = LanguageTemplateLoader.GetBuiltInTemplateNames();

        foreach (var name in templateNames)
        {
            var phonology = LanguageTemplateLoader.LoadBuiltIn(name);
            Assert.NotNull(phonology);

            var json = LanguageTemplateLoader.ConvertToJson(phonology);

            // Act
            var result = LanguageTemplateValidator.Validate(json);

            // Assert
            Assert.True(result.IsValid, $"Template '{name}' should be valid. Errors: {string.Join(", ", result.Errors)}");
        }
    }
}
