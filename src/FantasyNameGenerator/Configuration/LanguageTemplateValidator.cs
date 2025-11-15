namespace FantasyNameGenerator.Configuration;

/// <summary>
/// Validates language template JSON files.
/// Ensures templates have required fields and valid phonology.
/// </summary>
public static class LanguageTemplateValidator
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Validate a language template JSON.
    /// </summary>
    public static ValidationResult Validate(LanguageTemplateJson template)
    {
        var result = new ValidationResult { IsValid = true };

        // Required fields
        if (string.IsNullOrWhiteSpace(template.Name))
        {
            result.Errors.Add("Template must have a name");
            result.IsValid = false;
        }

        // Inventory validation
        if (string.IsNullOrWhiteSpace(template.Inventory.Consonants))
        {
            result.Errors.Add("Inventory must have consonants");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(template.Inventory.Vowels))
        {
            result.Errors.Add("Inventory must have vowels");
            result.IsValid = false;
        }

        // Check for minimum phonemes
        if (template.Inventory.Consonants?.Length < 3)
        {
            result.Warnings.Add("Very few consonants (< 3). Consider adding more for variety.");
        }

        if (template.Inventory.Vowels?.Length < 3)
        {
            result.Warnings.Add("Very few vowels (< 3). Consider adding more for variety.");
        }

        // Validate weights
        if (template.Weights != null)
        {
            foreach (var (phoneme, weight) in template.Weights)
            {
                if (weight < 0 || weight > 1)
                {
                    result.Errors.Add($"Weight for '{phoneme}' must be between 0.0 and 1.0");
                    result.IsValid = false;
                }

                if (phoneme.Length != 1)
                {
                    result.Errors.Add($"Weight key '{phoneme}' must be a single character");
                    result.IsValid = false;
                }
            }
        }

        // Validate allophones
        if (template.Allophones != null)
        {
            foreach (var rule in template.Allophones)
            {
                if (string.IsNullOrWhiteSpace(rule.Phoneme))
                {
                    result.Errors.Add("Allophone rule missing phoneme");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(rule.Allophone))
                {
                    result.Errors.Add("Allophone rule missing allophone");
                    result.IsValid = false;
                }

                if (rule.Phoneme?.Length > 1)
                {
                    result.Errors.Add($"Allophone phoneme '{rule.Phoneme}' must be a single character");
                    result.IsValid = false;
                }

                if (rule.Allophone?.Length > 1)
                {
                    result.Errors.Add($"Allophone allophone '{rule.Allophone}' must be a single character");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(rule.Context))
                {
                    result.Warnings.Add($"Allophone rule for '{rule.Phoneme}' has no context");
                }
            }
        }

        // Validate orthography
        if (template.Orthography != null)
        {
            foreach (var (phoneme, spelling) in template.Orthography)
            {
                if (phoneme.Length != 1)
                {
                    result.Errors.Add($"Orthography key '{phoneme}' must be a single character");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(spelling))
                {
                    result.Errors.Add($"Orthography for '{phoneme}' has empty spelling");
                    result.IsValid = false;
                }
            }
        }

        // Check for duplicate phonemes across categories
        var allPhonemes = new HashSet<char>();
        var categories = new[]
        {
            ("consonants", template.Inventory.Consonants),
            ("vowels", template.Inventory.Vowels),
            ("liquids", template.Inventory.Liquids),
            ("nasals", template.Inventory.Nasals),
            ("fricatives", template.Inventory.Fricatives),
            ("stops", template.Inventory.Stops),
            ("sibilants", template.Inventory.Sibilants),
            ("finals", template.Inventory.Finals)
        };

        foreach (var (category, phonemes) in categories)
        {
            if (string.IsNullOrEmpty(phonemes))
                continue;

            foreach (char c in phonemes)
            {
                if (!allPhonemes.Add(c))
                {
                    result.Warnings.Add($"Phoneme '{c}' appears in multiple categories");
                }
            }
        }

        // Validate phonotactics if present
        if (template.Phonotactics != null)
        {
            var phono = template.Phonotactics;

            if (phono.MaxConsonantCluster.HasValue && phono.MaxConsonantCluster < 1)
            {
                result.Errors.Add("MaxConsonantCluster must be at least 1");
                result.IsValid = false;
            }

            if (phono.MaxVowelCluster.HasValue && phono.MaxVowelCluster < 1)
            {
                result.Errors.Add("MaxVowelCluster must be at least 1");
                result.IsValid = false;
            }

            if (phono.MinSyllables.HasValue && phono.MinSyllables < 1)
            {
                result.Errors.Add("MinSyllables must be at least 1");
                result.IsValid = false;
            }

            if (phono.MaxSyllables.HasValue && phono.MinSyllables.HasValue 
                && phono.MaxSyllables < phono.MinSyllables)
            {
                result.Errors.Add("MaxSyllables must be >= MinSyllables");
                result.IsValid = false;
            }

            if (phono.Structures == null || phono.Structures.Count == 0)
            {
                result.Warnings.Add("No syllable structures defined - using defaults");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate a JSON file.
    /// </summary>
    public static ValidationResult ValidateFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var template = System.Text.Json.JsonSerializer.Deserialize<LanguageTemplateJson>(json);
            
            if (template == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "Failed to deserialize template" }
                };
            }

            return Validate(template);
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = { $"Error reading file: {ex.Message}" }
            };
        }
    }
}
