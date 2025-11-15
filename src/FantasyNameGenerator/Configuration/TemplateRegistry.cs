using System.Collections.Concurrent;
using FantasyNameGenerator.Phonology;

namespace FantasyNameGenerator.Configuration;

/// <summary>
/// Central registry for language templates.
/// Manages both built-in and custom templates with auto-discovery.
/// </summary>
public class TemplateRegistry
{
    private static readonly TemplateRegistry _instance = new();
    private readonly ConcurrentDictionary<string, TemplateSource> _templates = new();
    private readonly List<string> _customTemplatePaths = new();
    private bool _autoDiscoveryEnabled = false;

    private TemplateRegistry() { }

    public static TemplateRegistry Instance => _instance;

    /// <summary>
    /// Enable or disable auto-discovery of custom templates.
    /// </summary>
    public bool AutoDiscoveryEnabled
    {
        get => _autoDiscoveryEnabled;
        set
        {
            _autoDiscoveryEnabled = value;
            if (value)
                DiscoverTemplates();
        }
    }

    /// <summary>
    /// Add a directory to scan for custom templates.
    /// Immediately discovers templates in the directory.
    /// </summary>
    public void AddCustomTemplatePath(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Template directory not found: {path}");

        if (!_customTemplatePaths.Contains(path))
        {
            _customTemplatePaths.Add(path);
            
            // Always discover templates when adding a path
            DiscoverTemplatesInDirectory(path);
        }
    }

    /// <summary>
    /// Remove a custom template path.
    /// </summary>
    public void RemoveCustomTemplatePath(string path)
    {
        _customTemplatePaths.Remove(path);
        
        // Remove templates from this path
        var toRemove = _templates
            .Where(kvp => kvp.Value.Type == TemplateSourceType.Custom && 
                         kvp.Value.Path?.StartsWith(path) == true)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _templates.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Discover all templates from configured paths.
    /// </summary>
    public void DiscoverTemplates()
    {
        // Discover built-in templates
        DiscoverBuiltInTemplates();

        // Discover custom templates from all configured paths
        foreach (var path in _customTemplatePaths)
        {
            DiscoverTemplatesInDirectory(path);
        }
    }

    /// <summary>
    /// Get all available template names.
    /// </summary>
    public string[] GetAvailableTemplates()
    {
        if (_autoDiscoveryEnabled || _templates.IsEmpty)
            DiscoverTemplates();

        return _templates.Keys.OrderBy(k => k).ToArray();
    }

    /// <summary>
    /// Check if a template exists.
    /// </summary>
    public bool HasTemplate(string name)
    {
        var normalizedName = name.ToLowerInvariant();
        
        if (_templates.ContainsKey(normalizedName))
            return true;

        // If registry is empty or auto-discovery is enabled, discover templates
        if (_templates.IsEmpty || _autoDiscoveryEnabled)
        {
            DiscoverTemplates();
            return _templates.ContainsKey(normalizedName);
        }

        return false;
    }

    /// <summary>
    /// Get a template by name.
    /// </summary>
    public CulturePhonology? GetTemplate(string name)
    {
        var normalizedName = name.ToLowerInvariant();

        if (!_templates.TryGetValue(normalizedName, out var source))
        {
            // If registry is empty or auto-discovery is enabled, discover templates
            if (_templates.IsEmpty || _autoDiscoveryEnabled)
            {
                DiscoverTemplates();
                if (!_templates.TryGetValue(normalizedName, out source))
                    return null;
            }
            else
            {
                return null;
            }
        }

        return LoadTemplate(source);
    }

    /// <summary>
    /// Register a custom template from file.
    /// </summary>
    public void RegisterCustomTemplate(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Template file not found: {filePath}");

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var normalizedName = fileName.ToLowerInvariant();

        var source = new TemplateSource
        {
            Name = normalizedName,
            Type = TemplateSourceType.Custom,
            Path = filePath
        };

        _templates[normalizedName] = source;
    }

    /// <summary>
    /// Unregister a template.
    /// </summary>
    public void UnregisterTemplate(string name)
    {
        var normalizedName = name.ToLowerInvariant();
        _templates.TryRemove(normalizedName, out _);
    }

    /// <summary>
    /// Clear all registered templates.
    /// </summary>
    public void Clear()
    {
        _templates.Clear();
    }

    private void DiscoverBuiltInTemplates()
    {
        var builtInNames = LanguageTemplateLoader.GetBuiltInTemplateNames();

        foreach (var name in builtInNames)
        {
            var normalizedName = name.ToLowerInvariant();
            
            // Don't override custom templates with built-in
            if (!_templates.ContainsKey(normalizedName) || 
                _templates[normalizedName].Type == TemplateSourceType.BuiltIn)
            {
                _templates[normalizedName] = new TemplateSource
                {
                    Name = normalizedName,
                    Type = TemplateSourceType.BuiltIn
                };
            }
        }
    }

    private void DiscoverTemplatesInDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        var jsonFiles = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var filePath in jsonFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var normalizedName = fileName.ToLowerInvariant();

            // Custom templates override built-in
            _templates[normalizedName] = new TemplateSource
            {
                Name = normalizedName,
                Type = TemplateSourceType.Custom,
                Path = filePath
            };
        }
    }

    private CulturePhonology? LoadTemplate(TemplateSource source)
    {
        try
        {
            return source.Type switch
            {
                TemplateSourceType.BuiltIn => LanguageTemplateLoader.LoadBuiltIn(source.Name),
                TemplateSourceType.Custom when source.Path != null => 
                    LanguageTemplateLoader.LoadFromFile(source.Path),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents a template source location.
/// </summary>
internal class TemplateSource
{
    public required string Name { get; init; }
    public required TemplateSourceType Type { get; init; }
    public string? Path { get; init; }
}

/// <summary>
/// Type of template source.
/// </summary>
internal enum TemplateSourceType
{
    BuiltIn,
    Custom
}
