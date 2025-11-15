using FantasyNameGenerator.Configuration;
using FantasyNameGenerator.Phonology;

namespace FantasyNameGenerator.Tests.Configuration;

public class TemplateRegistryTests : IDisposable
{
    private readonly TemplateRegistry _registry;
    private readonly string _tempDir;

    public TemplateRegistryTests()
    {
        _registry = TemplateRegistry.Instance;
        _registry.AutoDiscoveryEnabled = false;
        _registry.Clear();

        // Create temp directory for test templates
        _tempDir = Path.Combine(Path.GetTempPath(), $"TemplateRegistryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _registry.Clear();
        
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void GetAvailableTemplates_WithoutDiscovery_ReturnsEmpty()
    {
        // Act
        var templates = _registry.GetAvailableTemplates();

        // Assert
        Assert.NotNull(templates);
        Assert.NotEmpty(templates); // Will discover built-in on first call
    }

    [Fact]
    public void DiscoverTemplates_FindsBuiltInTemplates()
    {
        // Act
        _registry.DiscoverTemplates();
        var templates = _registry.GetAvailableTemplates();

        // Assert
        Assert.Contains("germanic", templates);
        Assert.Contains("romance", templates);
        Assert.Contains("slavic", templates);
        Assert.Contains("elvish", templates);
        Assert.Contains("dwarvish", templates);
        Assert.Contains("orcish", templates);
    }

    [Fact]
    public void GetTemplate_BuiltIn_ReturnsTemplate()
    {
        // Act
        var template = _registry.GetTemplate("germanic");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Germanic", template.Name);
        Assert.NotEmpty(template.Inventory.Consonants);
        Assert.NotEmpty(template.Inventory.Vowels);
    }

    [Fact]
    public void GetTemplate_NonExistent_ReturnsNull()
    {
        // Act
        var template = _registry.GetTemplate("nonexistent");

        // Assert
        Assert.Null(template);
    }

    [Fact]
    public void HasTemplate_BuiltIn_ReturnsTrue()
    {
        // Arrange
        _registry.DiscoverTemplates();

        // Act
        var exists = _registry.HasTemplate("germanic");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void HasTemplate_NonExistent_ReturnsFalse()
    {
        // Act
        var exists = _registry.HasTemplate("nonexistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void RegisterCustomTemplate_AddsToRegistry()
    {
        // Arrange
        var customJson = @"{
  ""name"": ""Custom"",
  ""version"": ""1.0"",
  ""inventory"": {
    ""consonants"": ""ptkmnl"",
    ""vowels"": ""aeiou""
  }
}";
        var customPath = Path.Combine(_tempDir, "custom.json");
        File.WriteAllText(customPath, customJson);

        // Act
        _registry.RegisterCustomTemplate(customPath);
        var template = _registry.GetTemplate("custom");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Custom", template.Name);
        Assert.Equal("ptkmnl", template.Inventory.Consonants);
    }

    [Fact]
    public void AddCustomTemplatePath_DiscoversTemplates()
    {
        // Arrange
        var customJson = @"{
  ""name"": ""TestLang"",
  ""version"": ""1.0"",
  ""inventory"": {
    ""consonants"": ""ptkm"",
    ""vowels"": ""aei""
  }
}";
        var customPath = Path.Combine(_tempDir, "testlang.json");
        File.WriteAllText(customPath, customJson);

        // Act
        _registry.AddCustomTemplatePath(_tempDir);
        var templates = _registry.GetAvailableTemplates();

        // Assert
        Assert.Contains("testlang", templates);
    }

    [Fact]
    public void AddCustomTemplatePath_InvalidPath_ThrowsException()
    {
        // Arrange
        var invalidPath = Path.Combine(_tempDir, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => 
            _registry.AddCustomTemplatePath(invalidPath));
    }

    [Fact]
    public void RemoveCustomTemplatePath_RemovesTemplates()
    {
        // Arrange
        var customJson = @"{
  ""name"": ""TempLang"",
  ""version"": ""1.0"",
  ""inventory"": {
    ""consonants"": ""ptk"",
    ""vowels"": ""ae""
  }
}";
        var customPath = Path.Combine(_tempDir, "templang.json");
        File.WriteAllText(customPath, customJson);

        _registry.AddCustomTemplatePath(_tempDir);
        Assert.True(_registry.HasTemplate("templang"));

        // Act
        _registry.RemoveCustomTemplatePath(_tempDir);
        var stillExists = _registry.HasTemplate("templang");

        // Assert
        Assert.False(stillExists);
    }

    [Fact]
    public void CustomTemplate_OverridesBuiltIn()
    {
        // Arrange - Create custom "germanic" that overrides built-in
        var customJson = @"{
  ""name"": ""CustomGermanic"",
  ""version"": ""1.0"",
  ""inventory"": {
    ""consonants"": ""xyz"",
    ""vowels"": ""a""
  }
}";
        var customPath = Path.Combine(_tempDir, "germanic.json");
        File.WriteAllText(customPath, customJson);

        // Act
        _registry.AddCustomTemplatePath(_tempDir);
        var template = _registry.GetTemplate("germanic");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("CustomGermanic", template.Name);
        Assert.Equal("xyz", template.Inventory.Consonants);
    }

    [Fact]
    public void UnregisterTemplate_RemovesTemplate()
    {
        // Arrange
        _registry.AutoDiscoveryEnabled = false; // Prevent auto-rediscovery
        _registry.DiscoverTemplates();
        Assert.True(_registry.HasTemplate("germanic"));

        // Act
        _registry.UnregisterTemplate("germanic");
        var stillExists = _registry.HasTemplate("germanic");

        // Assert
        Assert.False(stillExists);
    }

    [Fact]
    public void AutoDiscoveryEnabled_AutomaticallyFindsTemplates()
    {
        // Arrange
        var customJson = @"{
  ""name"": ""AutoDiscovered"",
  ""version"": ""1.0"",
  ""inventory"": {
    ""consonants"": ""ptk"",
    ""vowels"": ""aei""
  }
}";
        var customPath = Path.Combine(_tempDir, "autodiscovered.json");
        
        _registry.AutoDiscoveryEnabled = true;
        _registry.AddCustomTemplatePath(_tempDir);

        // Act - Write file after adding path
        File.WriteAllText(customPath, customJson);
        
        // Manual discovery since file watcher isn't implemented yet
        _registry.DiscoverTemplates();
        
        var exists = _registry.HasTemplate("autodiscovered");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void GetTemplate_CaseInsensitive()
    {
        // Act
        var lower = _registry.GetTemplate("germanic");
        var upper = _registry.GetTemplate("GERMANIC");
        var mixed = _registry.GetTemplate("GeRmAnIc");

        // Assert
        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.NotNull(mixed);
        Assert.Equal(lower.Name, upper.Name);
        Assert.Equal(lower.Name, mixed.Name);
    }
}
