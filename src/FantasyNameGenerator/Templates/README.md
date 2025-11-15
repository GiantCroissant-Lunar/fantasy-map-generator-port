# Language Templates

This directory contains JSON-based language phonology templates for the Fantasy Name Generator.

## Overview

Language templates define the phonological system of a constructed language, including:
- **Phoneme inventory**: Available consonants, vowels, and phoneme classes
- **Weights**: Frequency of specific phonemes (optional)
- **Allophones**: Contextual sound changes (optional)
- **Orthography**: How phonemes are spelled in written form

## Built-in Templates

The following templates are embedded as resources:

1. **germanic.json** - Germanic-inspired (English, German, Norse)
2. **romance.json** - Romance-inspired (Latin, Italian, Spanish, French)
3. **slavic.json** - Slavic-inspired (Russian, Polish, Czech)
4. **elvish.json** - Elvish-inspired (Tolkien-style)
5. **dwarvish.json** - Dwarvish-inspired (harsh, guttural)
6. **orcish.json** - Orcish-inspired (simple, brutal)

## JSON Schema

```json
{
  "name": "LanguageName",
  "description": "Optional description",
  "version": "1.0",
  "inventory": {
    "consonants": "ptkbdgmnlrs",
    "vowels": "aeiou",
    "liquids": "lr",           // Optional
    "nasals": "mn",            // Optional
    "fricatives": "fs",        // Optional
    "stops": "ptkbdg",         // Optional
    "sibilants": "s",          // Optional
    "finals": "mnrs"           // Optional
  },
  "weights": {                 // Optional phoneme frequencies
    "p": 0.8,
    "t": 1.0,
    "k": 0.6
  },
  "allophones": [              // Optional sound changes
    {
      "phoneme": "t",
      "allophone": "d",
      "context": "V_V",        // Between vowels
      "description": "Optional explanation"
    }
  ],
  "orthography": {             // Optional spelling rules
    "ʃ": "sh",
    "θ": "th",
    "ŋ": "ng"
  }
}
```

## Using Templates

### Load a Built-in Template

```csharp
using FantasyNameGenerator.Configuration;

// Load from embedded resources
var phonology = LanguageTemplateLoader.LoadBuiltIn("germanic");
```

### Load from File

```csharp
// Load custom template from file system
var phonology = LanguageTemplateLoader.LoadFromFile("path/to/custom.json");
```

### List Available Templates

```csharp
var names = LanguageTemplateLoader.GetBuiltInTemplateNames();
// Returns: ["germanic", "romance", "slavic", "elvish", "dwarvish", "orcish"]
```

### Using with PhonologyTemplates

```csharp
using FantasyNameGenerator.Phonology;

// Enable JSON loading (default)
PhonologyTemplates.UseJsonTemplates = true;

// Get template - automatically loads from JSON
var phonology = PhonologyTemplates.GetTemplate("germanic");

// Or use the old hardcoded templates
PhonologyTemplates.UseJsonTemplates = false;
var hardcoded = PhonologyTemplates.Germanic();
```

## Creating Custom Templates

1. Create a new JSON file following the schema above
2. Save it with a descriptive name (e.g., `japanese.json`)
3. Load it using `LanguageTemplateLoader.LoadFromFile()`

### Example: Simple Custom Language

```json
{
  "name": "SimpleLang",
  "description": "A simple constructed language",
  "version": "1.0",
  "inventory": {
    "consonants": "ptkmnls",
    "vowels": "aeiou",
    "liquids": "lr",
    "nasals": "mn",
    "finals": "nms"
  },
  "orthography": {
    "p": "p",
    "t": "t",
    "k": "k"
  }
}
```

## Validation

Use the validator to check templates before use:

```csharp
using FantasyNameGenerator.Configuration;

// Validate a JSON file
var result = LanguageTemplateValidator.ValidateFile("path/to/template.json");

if (result.IsValid)
{
    Console.WriteLine("Template is valid!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}

// Check warnings
foreach (var warning in result.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

## Converting Existing Phonology

```csharp
using FantasyNameGenerator.Configuration;
using FantasyNameGenerator.Phonology;

// Convert existing CulturePhonology to JSON
var phonology = PhonologyTemplates.Germanic();
var json = LanguageTemplateLoader.ConvertToJson(phonology);

// Save to file
LanguageTemplateLoader.SaveToFile(phonology, "output.json");

// Or serialize to string
var jsonString = System.Text.Json.JsonSerializer.Serialize(json, 
    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
```

## IPA Symbols

Use International Phonetic Alphabet (IPA) symbols for phonemes:

### Common Consonants
- **Stops**: p t k b d g
- **Fricatives**: f v θ ð s z ʃ ʒ χ
- **Nasals**: m n ŋ
- **Liquids**: l r
- **Approximants**: w j

### Common Vowels
- **Basic**: a e i o u
- **Additional**: ə ɛ ɔ æ ɑ ɪ ʊ ɨ

### Orthography Mapping
Map IPA symbols to readable spellings:
- ʃ → "sh"
- θ → "th"
- ŋ → "ng"
- χ → "kh" or "gh"

## Tips for Creating Templates

1. **Start Simple**: Begin with basic consonants and vowels
2. **Test Early**: Validate your template before generating names
3. **Use Realistic Phonology**: Study real languages for inspiration
4. **Balance Phonemes**: Mix common and rare sounds
5. **Add Orthography**: Make output readable
6. **Consider Phonotactics**: What sound combinations are allowed?

## See Also

- [Phonology Documentation](../Phonology/README.md)
- [Name Generator Guide](../README.md)
- [IPA Chart](https://www.internationalphoneticassociation.org/content/ipa-chart)
