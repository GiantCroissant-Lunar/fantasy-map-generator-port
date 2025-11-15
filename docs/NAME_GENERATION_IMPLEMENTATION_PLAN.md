# Name Generation System - Implementation Plan

## Executive Summary

Based on research into linguistic name generation systems and analysis of the spec, this document outlines a pragmatic implementation strategy for Spec 014.

## Strategy: Hybrid Phonotactic + Morphology System

### Why This Approach?

1. **Realistic** - Produces linguistically consistent names
2. **Flexible** - Supports multiple cultures with distinct "feels"
3. **Performant** - Can be optimized for real-time generation
4. **Maintainable** - Clean separation of concerns
5. **Extensible** - Easy to add new cultures/languages

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                  NameGenerator                          │
│  (High-level API for generating names)                 │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
┌───────▼────────┐      ┌────────▼─────────┐
│ LanguageEngine │      │  GrammarEngine   │
│ (Phonotactics) │      │  (Templates)     │
└───────┬────────┘      └────────┬─────────┘
        │                        │
┌───────▼────────┐      ┌────────▼─────────┐
│ SyllableGen    │      │  RuleExpander    │
│ MorphemeGen    │      │  (Tracery-like)  │
└───────┬────────┘      └──────────────────┘
        │
┌───────▼────────┐
│ CultureLanguage│
│ (Phoneme Sets) │
└────────────────┘
```

## Phase 1: Core Foundation (Days 1-3)

### 1.1 Models

**Files to create:**
- `src/FantasyMapGenerator.Core/Naming/Language.cs`
- `src/FantasyMapGenerator.Core/Naming/CultureLanguage.cs`
- `src/FantasyMapGenerator.Core/Naming/NameType.cs`

**Key classes:**

```csharp
public class Language
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Phoneme inventory
    public PhonemeSet Phonemes { get; set; }
    
    // Syllable patterns
    public List<SyllablePattern> Patterns { get; set; }
    
    // Morphemes (semantic units)
    public MorphemeSet Morphemes { get; set; }
    
    // Orthography (spelling rules)
    public OrthographyRules Orthography { get; set; }
    
    // Constraints
    public int MinSyllables { get; set; }
    public int MaxSyllables { get; set; }
    public List<string> ForbiddenPatterns { get; set; }
    
    // Grammar
    public char Joiner { get; set; } = ' ';
    public string? Genitive { get; set; }
    public string? Definite { get; set; }
}

public class PhonemeSet
{
    public string Consonants { get; set; } = "";
    public string Vowels { get; set; } = "";
    public string Liquids { get; set; } = "";
    public string Sibilants { get; set; } = "";
    public string Finals { get; set; } = "";
    
    // Weighted selection
    public Dictionary<char, double> Weights { get; set; } = new();
}

public class SyllablePattern
{
    public string Pattern { get; set; } = ""; // e.g., "CVC", "CV?"
    public double Weight { get; set; } = 1.0;
}

public enum NameType
{
    Burg,
    State,
    Province,
    Religion,
    Culture,
    Person,
    River,
    Mountain,
    Region
}
```

### 1.2 Phoneme Data

**File:** `src/FantasyMapGenerator.Core/Naming/Data/PhonemeLibrary.cs`

Create phoneme inventories for common language families:

```csharp
public static class PhonemeLibrary
{
    public static PhonemeSet Germanic => new()
    {
        Consonants = "ptkbdgmnlrsfvhw",
        Vowels = "aeiouæ",
        Liquids = "lr",
        Sibilants = "sʃ",
        Finals = "mnŋ"
    };
    
    public static PhonemeSet Romance => new()
    {
        Consonants = "ptkbdgmnlrsfv",
        Vowels = "aeiou",
        Liquids = "lr",
        Sibilants = "s",
        Finals = "mn"
    };
    
    public static PhonemeSet Slavic => new()
    {
        Consonants = "ptkbdgmnlrsfvzʃʒ",
        Vowels = "aeiouə",
        Liquids = "lr",
        Sibilants = "sʃʒ",
        Finals = "mnŋ"
    };
    
    public static PhonemeSet Arabic => new()
    {
        Consonants = "btkdqmnlrsfʃħʕ",
        Vowels = "aiuː",
        Liquids = "lr",
        Sibilants = "sʃ",
        Finals = "mn"
    };
    
    public static PhonemeSet Japanese => new()
    {
        Consonants = "tkmnrshwy",
        Vowels = "aiueo",
        Liquids = "r",
        Sibilants = "s",
        Finals = "n"
    };
    
    // Add more: Celtic, Nordic, Polynesian, etc.
}
```

## Phase 2: Syllable Generation (Days 4-5)

### 2.1 Pattern Parser

**File:** `src/FantasyMapGenerator.Core/Naming/SyllablePatternParser.cs`

Parse patterns like:
- `CV` - Consonant + Vowel
- `CVC` - Consonant + Vowel + Consonant
- `CV?` - Consonant + Vowel + Optional Consonant
- `C?VC` - Optional Consonant + Vowel + Consonant

```csharp
public class SyllablePatternParser
{
    public List<PatternElement> Parse(string pattern)
    {
        var elements = new List<PatternElement>();
        
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            bool optional = (i + 1 < pattern.Length && pattern[i + 1] == '?');
            
            if (optional) i++; // Skip the '?'
            
            elements.Add(new PatternElement
            {
                Type = c switch
                {
                    'C' => PhonemeType.Consonant,
                    'V' => PhonemeType.Vowel,
                    'L' => PhonemeType.Liquid,
                    'S' => PhonemeType.Sibilant,
                    'F' => PhonemeType.Final,
                    _ => throw new ArgumentException($"Unknown pattern: {c}")
                },
                Optional = optional
            });
        }
        
        return elements;
    }
}
```

### 2.2 Syllable Generator

**File:** `src/FantasyMapGenerator.Core/Naming/SyllableGenerator.cs`

```csharp
public class SyllableGenerator
{
    private readonly Language _language;
    private readonly IRandomSource _random;
    
    public string Generate()
    {
        // Pick pattern
        var pattern = PickWeightedPattern();
        var elements = SyllablePatternParser.Parse(pattern.Pattern);
        
        // Generate syllable
        var syllable = new StringBuilder();
        foreach (var element in elements)
        {
            if (element.Optional && _random.NextDouble() < 0.5)
                continue;
            
            char phoneme = PickPhoneme(element.Type);
            syllable.Append(phoneme);
        }
        
        string result = syllable.ToString();
        
        // Check forbidden patterns
        if (IsForbidden(result))
            return Generate(); // Retry
        
        // Apply orthography
        return ApplyOrthography(result);
    }
    
    private char PickPhoneme(PhonemeType type)
    {
        string set = type switch
        {
            PhonemeType.Consonant => _language.Phonemes.Consonants,
            PhonemeType.Vowel => _language.Phonemes.Vowels,
            PhonemeType.Liquid => _language.Phonemes.Liquids,
            PhonemeType.Sibilant => _language.Phonemes.Sibilants,
            PhonemeType.Final => _language.Phonemes.Finals,
            _ => throw new ArgumentException()
        };
        
        // Weighted selection
        return PickWeighted(set);
    }
}
```

## Phase 3: Morpheme System (Days 6-7)

### 3.1 Morpheme Generator

**File:** `src/FantasyMapGenerator.Core/Naming/MorphemeGenerator.cs`

```csharp
public class MorphemeGenerator
{
    private readonly Language _language;
    private readonly SyllableGenerator _syllables;
    private readonly Dictionary<string, List<string>> _cache = new();
    
    public string GetMorpheme(string semanticKey = "")
    {
        if (!_cache.ContainsKey(semanticKey))
            _cache[semanticKey] = new List<string>();
        
        var list = _cache[semanticKey];
        
        // Reuse existing morpheme (70% chance)
        if (list.Any() && _random.NextDouble() < 0.7)
            return list[_random.Next(list.Count)];
        
        // Generate new morpheme
        string morpheme = _syllables.Generate();
        
        // Ensure uniqueness across all semantic keys
        if (IsAlreadyUsed(morpheme))
            return GetMorpheme(semanticKey); // Retry
        
        list.Add(morpheme);
        return morpheme;
    }
    
    private bool IsAlreadyUsed(string morpheme)
    {
        return _cache.Values.Any(list => list.Contains(morpheme));
    }
}
```

### 3.2 Semantic Morphemes

**File:** `src/FantasyMapGenerator.Core/Naming/Data/SemanticKeys.cs`

```csharp
public static class SemanticKeys
{
    // Geography
    public const string Mountain = "mountain";
    public const string River = "river";
    public const string Forest = "forest";
    public const string Sea = "sea";
    public const string Lake = "lake";
    
    // Settlements
    public const string City = "city";
    public const string Town = "town";
    public const string Fort = "fort";
    public const string Port = "port";
    
    // Directions
    public const string North = "north";
    public const string South = "south";
    public const string East = "east";
    public const string West = "west";
    
    // Qualities
    public const string Great = "great";
    public const string New = "new";
    public const string Old = "old";
    public const string Holy = "holy";
}
```

## Phase 4: Name Generation (Days 8-9)

### 4.1 Name Generator

**File:** `src/FantasyMapGenerator.Core/Naming/NameGenerator.cs`

```csharp
public class NameGenerator
{
    private readonly Language _language;
    private readonly MorphemeGenerator _morphemes;
    private readonly IRandomSource _random;
    private readonly HashSet<string> _usedNames = new();
    
    public string GenerateName(NameType type, string? semanticHint = null)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            string name = type switch
            {
                NameType.Burg => GenerateBurgName(semanticHint),
                NameType.State => GenerateStateName(semanticHint),
                NameType.River => GenerateRiverName(),
                NameType.Mountain => GenerateMountainName(),
                _ => GenerateSimpleName(semanticHint)
            };
            
            // Validate
            if (name.Length < 3 || name.Length > 20)
                continue;
            
            if (_usedNames.Contains(name))
                continue;
            
            _usedNames.Add(name);
            return name;
        }
        
        throw new Exception("Failed to generate unique name");
    }
    
    private string GenerateBurgName(string? hint)
    {
        if (_random.NextDouble() < 0.6)
        {
            // Simple name
            return Capitalize(GetWord(hint ?? SemanticKeys.City));
        }
        else
        {
            // Compound name
            string w1 = Capitalize(GetWord(hint ?? SemanticKeys.City));
            string w2 = Capitalize(GetWord());
            return $"{w1}{_language.Joiner}{w2}";
        }
    }
    
    private string GenerateStateName(string? hint)
    {
        double roll = _random.NextDouble();
        
        if (roll < 0.3)
        {
            // Simple: "Angland"
            return Capitalize(GetWord(hint));
        }
        else if (roll < 0.6)
        {
            // Compound: "Northmark"
            string w1 = Capitalize(GetWord());
            string w2 = Capitalize(GetWord(hint));
            return $"{w1}{w2}";
        }
        else
        {
            // Genitive: "Kingdom of Angland"
            string title = PickTitle();
            string name = Capitalize(GetWord(hint));
            return $"{title}{_language.Joiner}{_language.Genitive}{_language.Joiner}{name}";
        }
    }
    
    private string GetWord(string? semanticKey = null)
    {
        int syllables = _random.Next(_language.MinSyllables, _language.MaxSyllables + 1);
        var word = new StringBuilder();
        
        for (int i = 0; i < syllables; i++)
        {
            string key = (i == 0 && semanticKey != null) ? semanticKey : "";
            word.Append(_morphemes.GetMorpheme(key));
        }
        
        return word.ToString();
    }
}
```

## Phase 5: Integration (Days 10-11)

### 5.1 Culture Integration

Update `Culture.cs`:

```csharp
public class Culture
{
    // ... existing properties ...
    
    public Language? Language { get; set; }
    public NameGenerator? NameGenerator { get; set; }
}
```

### 5.2 Generator Integration

Update generators to use name system:

```csharp
// In BurgsGenerator
var burg = new Burg
{
    Name = culture.NameGenerator?.GenerateName(NameType.Burg) 
           ?? $"Burg{id}",
    // ...
};

// In StatesGenerator
var state = new State
{
    Name = culture.NameGenerator?.GenerateName(NameType.State)
           ?? $"State{id}",
    // ...
};
```

## Phase 6: Testing (Days 12-13)

### 6.1 Unit Tests

**File:** `tests/FantasyMapGenerator.Core.Tests/Naming/NameGeneratorTests.cs`

```csharp
[Fact]
public void GenerateName_ProducesValidNames()
{
    var language = CreateTestLanguage();
    var generator = new NameGenerator(language, random, settings);
    
    var name = generator.GenerateName(NameType.Burg);
    
    Assert.NotEmpty(name);
    Assert.InRange(name.Length, 3, 20);
    Assert.True(char.IsUpper(name[0]));
}

[Fact]
public void GenerateName_ProducesUniqueNames()
{
    var generator = new NameGenerator(language, random, settings);
    var names = new HashSet<string>();
    
    for (int i = 0; i < 100; i++)
    {
        var name = generator.GenerateName(NameType.Burg);
        Assert.DoesNotContain(name, names);
        names.Add(name);
    }
}

[Fact]
public void GenerateName_IsDeterministic()
{
    var gen1 = new NameGenerator(language, new PcgRandomSource(12345), settings);
    var gen2 = new NameGenerator(language, new PcgRandomSource(12345), settings);
    
    for (int i = 0; i < 50; i++)
    {
        Assert.Equal(
            gen1.GenerateName(NameType.Burg),
            gen2.GenerateName(NameType.Burg)
        );
    }
}
```

## Recommended Libraries

Based on research, here are the best options:

### Option 1: Build from Scratch (RECOMMENDED)
- **Pros:** Full control, no dependencies, optimized for FMG
- **Cons:** More work upfront
- **Effort:** ~2 weeks
- **Best for:** Long-term maintainability

### Option 2: Adapt Syllabore
- **Repo:** https://github.com/kesac/Syllabore
- **License:** MIT
- **Pros:** C#, syllable-based, actively maintained
- **Cons:** May need significant adaptation
- **Effort:** ~1 week

### Option 3: Hybrid Approach (RECOMMENDED)
- Use Syllabore for syllable generation
- Build custom morpheme + grammar layers
- **Effort:** ~1.5 weeks
- **Best balance:** Speed + flexibility

## Decision: Recommended Approach

**Build a minimal custom system** inspired by:
1. **Syllabore** - for syllable generation patterns
2. **Azgaar's approach** - for morpheme tracking
3. **Tracery-style grammars** - for name templates

This gives you:
- ✅ Full control over the system
- ✅ No complex dependencies
- ✅ Optimized for FMG's needs
- ✅ Easy to extend per culture
- ✅ Deterministic and testable

## File Structure

```
src/FantasyMapGenerator.Core/
└── Naming/
    ├── Models/
    │   ├── Language.cs
    │   ├── PhonemeSet.cs
    │   ├── SyllablePattern.cs
    │   └── NameType.cs
    ├── Data/
    │   ├── PhonemeLibrary.cs
    │   └── SemanticKeys.cs
    ├── SyllablePatternParser.cs
    ├── SyllableGenerator.cs
    ├── MorphemeGenerator.cs
    ├── NameGenerator.cs
    └── LanguageFactory.cs

tests/FantasyMapGenerator.Core.Tests/
└── Naming/
    ├── SyllableGeneratorTests.cs
    ├── MorphemeGeneratorTests.cs
    ├── NameGeneratorTests.cs
    └── LanguageFactoryTests.cs
```

## Next Steps

1. **Review this plan** - Does this approach make sense?
2. **Adjust scope** - Any features to add/remove?
3. **Start implementation** - Begin with Phase 1 (Models)

## Design Decisions ✅

### 1. Language per Culture: YES
- Each culture gets its own unique language
- Languages are procedurally generated with distinct phoneme sets
- Ensures cultural identity and immersion

### 2. Spelling Rules: YES
- Full orthography system for readable names
- Maps phonemes to letter combinations (e.g., "ʃ" → "sh", "θ" → "th")
- Makes names pronounceable and natural-looking

### 3. User Customization: JSON-based Language Definitions
- **Built-in languages:** Germanic, Romance, Slavic, Arabic, Japanese, Celtic, Nordic, Polynesian
- **Fantasy languages:** Elvish, Dwarvish, Orcish (Tolkien-inspired)
- **Extensible:** Users can add custom languages via JSON files

**JSON Structure:**
```json
{
  "name": "Elvish",
  "phonemes": {
    "consonants": "ptklmnrsflvw",
    "vowels": "aeiouëïöü",
    "liquids": "lr",
    "sibilants": "s",
    "finals": "nml"
  },
  "patterns": [
    { "pattern": "CV", "weight": 0.4 },
    { "pattern": "CVC", "weight": 0.3 },
    { "pattern": "V", "weight": 0.3 }
  ],
  "orthography": {
    "ë": "ë",
    "ï": "ï",
    "th": "þ"
  },
  "morphemes": {
    "forest": ["gal", "taur", "las"],
    "mountain": ["amon", "orod"],
    "river": ["sir", "duin"],
    "city": ["ost", "minas"]
  },
  "minSyllables": 2,
  "maxSyllables": 4,
  "joiner": " ",
  "forbiddenPatterns": ["ii", "uu", "ëë"]
}
```

### 4. Language Loading System

**File:** `src/FantasyMapGenerator.Core/Naming/Data/Languages/`

Directory structure:
```
Data/Languages/
├── germanic.json
├── romance.json
├── slavic.json
├── arabic.json
├── japanese.json
├── celtic.json
├── nordic.json
├── polynesian.json
├── elvish.json       ← Fantasy
├── dwarvish.json     ← Fantasy
└── orcish.json       ← Fantasy
```

**Language Loader:**
```csharp
public class LanguageLoader
{
    public static Language LoadFromJson(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Language>(json);
    }
    
    public static Language LoadBuiltIn(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"FantasyMapGenerator.Core.Naming.Data.Languages.{name}.json";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Built-in language '{name}' not found");
        
        return JsonSerializer.Deserialize<Language>(stream);
    }
}
```

### 5. Culture-Language Mapping

**Update Culture model:**
```csharp
public class Culture
{
    // ... existing properties ...
    
    /// <summary>
    /// Language template to use (e.g., "germanic", "elvish")
    /// </summary>
    public string LanguageTemplate { get; set; } = "germanic";
    
    /// <summary>
    /// Generated language instance (unique per culture)
    /// </summary>
    public Language? Language { get; set; }
}
```

**In CulturesGenerator:**
```csharp
private void AssignLanguages()
{
    foreach (var culture in _map.Cultures)
    {
        // Load base language template
        var baseLanguage = LanguageLoader.LoadBuiltIn(culture.LanguageTemplate);
        
        // Mutate it to make it unique for this culture
        culture.Language = LanguageFactory.CreateVariant(
            baseLanguage, 
            _random, 
            mutationRate: 0.3
        );
    }
}
```

## Fantasy Language Support

### Elvish (Tolkien-inspired)
- **Phonemes:** Flowing, melodic (l, r, v, w, soft consonants)
- **Patterns:** CV, CVC, V (open syllables)
- **Orthography:** Diacritics (ë, ï, ö), digraphs (th → þ)
- **Morphemes:** Semantic roots (gal=light, taur=forest, duin=river)
- **Examples:** Lothlórien, Rivendell, Mirkwood

### Dwarvish (Tolkien-inspired)
- **Phonemes:** Harsh, guttural (k, g, kh, r, z, hard consonants)
- **Patterns:** CVC, CVCC (closed syllables, consonant clusters)
- **Orthography:** Digraphs (kh, zh, dh)
- **Morphemes:** Semantic roots (khaz=mine, dum=hall, barak=fortress)
- **Examples:** Khazad-dûm, Erebor, Gundabad

### Orcish (Tolkien-inspired)
- **Phonemes:** Harsh, simple (g, k, z, sh, u, a)
- **Patterns:** CVC, CV (simple structure)
- **Orthography:** Minimal, phonetic
- **Morphemes:** Brutal roots (gul=dark, dush=death, agh=battle)
- **Examples:** Mordor, Isengard, Lugburz

## Implementation Priority

### Phase 1 (Core - Week 1):
1. Models + Phoneme library
2. Syllable generation
3. Basic name generation

### Phase 2 (Advanced - Week 2):
4. Morpheme system
5. Grammar templates
6. JSON language loading
7. Fantasy languages (Elvish, Dwarvish, Orcish)

---

**Next Steps:**
1. I can start implementing Phase 1 right now
2. Or we can refine the fantasy language specifications first
3. Or discuss any other aspects

What would you like to do?
