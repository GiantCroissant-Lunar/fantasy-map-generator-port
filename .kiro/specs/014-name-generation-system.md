# Spec 014: Advanced Name Generation System

## Status
- **State:** Not Started
- **Priority:** ⭐⭐⭐⭐ Important
- **Estimated Effort:** 2 weeks
- **Dependencies:** Cultures (008)
- **Blocks:** None (enhances existing features)

## Overview

Implement an advanced linguistic name generation system that creates culturally-appropriate names for places, people, and features. The system uses phoneme-based language generation with morphemes, syllable structures, and orthographic rules to create realistic-sounding names.

## Goals

1. **Language Generation** - Create procedural languages with phonetic rules
2. **Phoneme Systems** - Define consonants, vowels, and special sounds
3. **Syllable Structures** - Control how syllables are formed
4. **Morpheme Tracking** - Semantic meaning in word parts
5. **Orthographic Rules** - Spelling conventions
6. **Name Generation** - Generate names for burgs, states, features, people

## Reference Implementation

**Source:** `ref-projects/FantasyMapGenerator/Language/LanguageGenerator.cs`

## Data Models

```csharp
namespace FantasyMapGenerator.Core.Models;

public class Language
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Phoneme Sets
    public Dictionary<string, string> Phonemes { get; set; } = new()
    {
        { "C", "" }, // Consonants
        { "V", "" }, // Vowels
        { "L", "" }, // Liquids
        { "S", "" }, // Sibilants
        { "F", "" }  // Finals
    };
    
    // Syllable Structure (e.g., "CVC", "CV", "V")
    public string Structure { get; set; } = "CVC";
    
    // Forbidden patterns (regex)
    public string[] Restricts { get; set; } = Array.Empty<string>();
    
    // Orthographic rules (phoneme -> spelling)
    public Dictionary<char, string> ConsonantOrthography { get; set; } = new();
    public Dictionary<char, string> VowelOrthography { get; set; } = new();
    
    // Syllable constraints
    public int MinSyllables { get; set; } = 1;
    public int MaxSyllables { get; set; } = 3;
    
    // Word joiner (space, hyphen, etc.)
    public char Joiner { get; set; } = ' ';
    
    // Morphemes (semantic word parts)
    public Dictionary<string, List<string>> Morphemes { get; set; } = new();
    
    // Generated words
    public Dictionary<string, List<string>> Words { get; set; } = new();
    
    // Generated names
    public List<string> Names { get; set; } = new();
    
    // Grammatical elements
    public string? Genitive { get; set; } // "of"
    public string? Definite { get; set; } // "the"
    
    // Generation parameters
    public int Exponent { get; set; } = 1; // Phoneme selection bias
    public bool NoOrthography { get; set; } = false;
    public bool NoMorphemes { get; set; } = false;
}
```

## Algorithm

### 1. Generate Random Language

```csharp
public class LanguageGenerator
{
    private readonly Random _random;
    
    public Language GenerateRandomLanguage()
    {
        var lang = new Language
        {
            NoOrthography = false,
            NoMorphemes = false
        };
        
        // Select phoneme sets
        lang.Phonemes["C"] = Shuffle(ConsonantSets[Choose(ConsonantSets.Length, 2)]);
        lang.Phonemes["V"] = Shuffle(VowelSets[Choose(VowelSets.Length, 2)]);
        lang.Phonemes["L"] = Shuffle(LiquidSets[Choose(LiquidSets.Length, 2)]);
        lang.Phonemes["S"] = Shuffle(SibilantSets[Choose(SibilantSets.Length, 2)]);
        lang.Phonemes["F"] = Shuffle(FinalSets[Choose(FinalSets.Length, 2)]);
        
        // Select syllable structure
        lang.Structure = SyllableStructures[Choose(SyllableStructures.Length)];
        
        // Select restrictions
        lang.Restricts = RestrictionSets[2]; // Common restrictions
        
        // Select orthography
        lang.ConsonantOrthography = ConsonantOrthographySets[Choose(ConsonantOrthographySets.Length, 2)];
        lang.VowelOrthography = VowelOrthographySets[Choose(VowelOrthographySets.Length, 2)];
        
        // Set syllable constraints
        lang.MinSyllables = _random.Next(1, 3);
        if (lang.Structure.Length < 3) lang.MinSyllables++;
        lang.MaxSyllables = _random.Next(lang.MinSyllables + 1, 7);
        
        // Set joiner
        lang.Joiner = new[] { ' ', ' ', ' ', '-' }[Choose(4)];
        
        return lang;
    }
    
    private int Choose(int length, int exponent = 1)
    {
        return (int)Math.Floor(Math.Pow(_random.NextDouble(), exponent) * length);
    }
    
    private string Shuffle(string str)
    {
        var chars = str.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}
```

### 2. Generate Syllable

```csharp
private string GenerateSyllable(Language lang)
{
    while (true)
    {
        string syllable = "";
        
        // Build syllable according to structure
        for (int i = 0; i < lang.Structure.Length; i++)
        {
            char phoneType = lang.Structure[i];
            
            // Check for optional phoneme (marked with ?)
            if (i + 1 < lang.Structure.Length && lang.Structure[i + 1] == '?')
            {
                i++; // Skip the ?
                if (_random.NextDouble() < 0.5)
                    continue; // Skip this phoneme
            }
            
            // Get phoneme set
            string phoneSet = lang.Phonemes[phoneType.ToString()];
            if (string.IsNullOrEmpty(phoneSet)) continue;
            
            // Select phoneme with bias
            int index = Choose(phoneSet.Length, lang.Exponent);
            syllable += phoneSet[index];
        }
        
        // Check restrictions
        bool forbidden = false;
        foreach (var restrict in lang.Restricts)
        {
            if (Regex.IsMatch(syllable, restrict, RegexOptions.IgnoreCase))
            {
                forbidden = true;
                break;
            }
        }
        
        if (forbidden) continue;
        
        // Apply orthography
        return ApplyOrthography(lang, syllable);
    }
}

private string ApplyOrthography(Language lang, string syllable)
{
    if (lang.NoOrthography) return syllable;
    
    string result = "";
    foreach (char c in syllable)
    {
        if (lang.ConsonantOrthography.TryGetValue(c, out string? spelling))
            result += spelling;
        else if (lang.VowelOrthography.TryGetValue(c, out spelling))
            result += spelling;
        else if (DefaultOrthography.TryGetValue(c, out spelling))
            result += spelling;
        else
            result += c;
    }
    return result;
}
```

### 3. Generate Morpheme

```csharp
private string GetMorpheme(Language lang, string semanticKey = "")
{
    if (lang.NoMorphemes)
        return GenerateSyllable(lang);
    
    // Get or create morpheme list for this key
    if (!lang.Morphemes.ContainsKey(semanticKey))
        lang.Morphemes[semanticKey] = new List<string>();
    
    var list = lang.Morphemes[semanticKey];
    int extras = string.IsNullOrEmpty(semanticKey) ? 10 : 1;
    
    while (true)
    {
        int n = _random.Next(list.Count + extras);
        
        // Return existing morpheme
        if (n < list.Count)
            return list[n];
        
        // Generate new morpheme
        string morph = GenerateSyllable(lang);
        
        // Check if already used
        bool alreadyUsed = lang.Morphemes.Values
            .Any(morphList => morphList.Contains(morph));
        
        if (alreadyUsed) continue;
        
        list.Add(morph);
        return morph;
    }
}
```

### 4. Generate Word

```csharp
private string GenerateWord(Language lang, string semanticKey = "")
{
    int syllableCount = _random.Next(lang.MinSyllables, lang.MaxSyllables + 1);
    string word = "";
    
    // Assign semantic key to random syllable
    var keys = new Dictionary<int, string>();
    if (!string.IsNullOrEmpty(semanticKey))
    {
        keys[_random.Next(syllableCount)] = semanticKey;
    }
    
    // Build word from morphemes
    for (int i = 0; i < syllableCount; i++)
    {
        string key = keys.ContainsKey(i) ? keys[i] : "";
        word += GetMorpheme(lang, key);
    }
    
    return word;
}
```

### 5. Generate Name

```csharp
public string GenerateName(Language lang, string semanticKey = "")
{
    // Initialize grammatical elements if needed
    if (lang.Genitive == null)
        lang.Genitive = GetMorpheme(lang, "of");
    if (lang.Definite == null)
        lang.Definite = GetMorpheme(lang, "the");
    
    while (true)
    {
        string name;
        
        if (_random.NextDouble() < 0.5)
        {
            // Simple name
            name = Capitalize(GetWord(lang, semanticKey));
        }
        else
        {
            // Compound name
            string w1 = Capitalize(GetWord(lang, 
                _random.NextDouble() < 0.6 ? semanticKey : ""));
            string w2 = Capitalize(GetWord(lang, 
                _random.NextDouble() < 0.6 ? semanticKey : ""));
            
            if (w1 == w2) continue;
            
            if (_random.NextDouble() > 0.5)
            {
                // Simple compound
                name = $"{w1}{lang.Joiner}{w2}";
            }
            else
            {
                // Genitive compound ("X of Y")
                name = $"{w1}{lang.Joiner}{lang.Genitive}{lang.Joiner}{w2}";
            }
        }
        
        // Optionally add definite article
        if (_random.NextDouble() < 0.1)
        {
            name = $"{lang.Definite}{lang.Joiner}{name}";
        }
        
        // Check length constraints
        if (name.Length < 3 || name.Length > 20) continue;
        
        // Check if name already used
        if (lang.Names.Any(n => n.Contains(name) || name.Contains(n)))
            continue;
        
        lang.Names.Add(name);
        return name;
    }
}

private string GetWord(Language lang, string semanticKey = "")
{
    if (!lang.Words.ContainsKey(semanticKey))
        lang.Words[semanticKey] = new List<string>();
    
    var list = lang.Words[semanticKey];
    int extras = string.IsNullOrEmpty(semanticKey) ? 3 : 2;
    
    while (true)
    {
        int n = _random.Next(list.Count + extras);
        
        if (n < list.Count)
            return list[n];
        
        string word = GenerateWord(lang, semanticKey);
        
        // Check if already used
        bool alreadyUsed = lang.Words.Values
            .Any(wordList => wordList.Contains(word));
        
        if (alreadyUsed) continue;
        
        list.Add(word);
        return word;
    }
}

private string Capitalize(string word)
{
    if (string.IsNullOrEmpty(word)) return word;
    return char.ToUpper(word[0]) + word[1..];
}
```

### 6. Phoneme Sets

```csharp
public static class PhonemeSets
{
    public static readonly string[] ConsonantSets = new[]
    {
        "ptkmnls",           // Minimal
        "ptkbdgmnlrsʃ",      // Common
        "ptkbdgmnlrsfvʃʒ",   // Extended
        "ptkbdgmnlrsfvʃʒθð", // Full
    };
    
    public static readonly string[] VowelSets = new[]
    {
        "aiu",               // Minimal
        "aeiou",             // Common
        "aeiouəɛɔ",          // Extended
        "aeiouəɛɔæɑɪʊ",     // Full
    };
    
    public static readonly string[] LiquidSets = new[]
    {
        "lr",
        "lrw",
        "lrwy"
    };
    
    public static readonly string[] SibilantSets = new[]
    {
        "s",
        "sʃ",
        "sʃʒ"
    };
    
    public static readonly string[] FinalSets = new[]
    {
        "mn",
        "mnŋ",
        "mnŋs"
    };
    
    public static readonly string[] SyllableStructures = new[]
    {
        "CVC",   // Consonant-Vowel-Consonant
        "CV",    // Consonant-Vowel
        "VC",    // Vowel-Consonant
        "V",     // Vowel only
        "CCV",   // Consonant-Consonant-Vowel
        "CVF",   // Consonant-Vowel-Final
        "CVCC",  // Consonant-Vowel-Consonant-Consonant
        "CVC?",  // Optional final consonant
        "C?VC",  // Optional initial consonant
    };
}
```

## Implementation Steps

### Step 1: Models (Day 1-2)
- [ ] Create `Language.cs` model
- [ ] Create phoneme set constants
- [ ] Create orthography mappings

### Step 2: Core Generation (Day 3-5)
- [ ] Create `LanguageGenerator.cs`
- [ ] Implement `GenerateRandomLanguage()`
- [ ] Implement `GenerateSyllable()`
- [ ] Implement `ApplyOrthography()`

### Step 3: Morphemes & Words (Day 6-7)
- [ ] Implement `GetMorpheme()`
- [ ] Implement `GenerateWord()`
- [ ] Implement word tracking

### Step 4: Name Generation (Day 8-9)
- [ ] Implement `GenerateName()`
- [ ] Implement `GetWord()`
- [ ] Implement compound names
- [ ] Implement genitive constructions

### Step 5: Integration (Day 10-11)
- [ ] Integrate with Cultures
- [ ] Integrate with Burgs
- [ ] Integrate with States
- [ ] Integrate with Features

### Step 6: Testing (Day 12-13)
- [ ] Unit tests for syllable generation
- [ ] Unit tests for name generation
- [ ] Integration tests
- [ ] Quality tests (no duplicates, etc.)

### Step 7: Documentation (Day 14)
- [ ] Update README
- [ ] Add usage examples
- [ ] Document phoneme systems

## Configuration

```csharp
public class MapGenerationSettings
{
    /// <summary>
    /// Enable advanced name generation
    /// </summary>
    public bool UseAdvancedNameGeneration { get; set; } = true;
    
    /// <summary>
    /// Generate unique language per culture
    /// </summary>
    public bool UniqueLanguagePerCulture { get; set; } = false;
    
    /// <summary>
    /// Minimum name length
    /// </summary>
    public int MinNameLength { get; set; } = 3;
    
    /// <summary>
    /// Maximum name length
    /// </summary>
    public int MaxNameLength { get; set; } = 20;
}
```

## Success Criteria

- [ ] Languages generated with consistent phonology
- [ ] Names sound natural and pronounceable
- [ ] No duplicate names
- [ ] Culturally appropriate names
- [ ] All tests passing
- [ ] Performance < 100ms per name

## Dependencies

**Required:**
- Cultures (008) - for language assignment

**Enhances:**
- Burgs (006) - better burg names
- States (007) - better state names
- Features - better feature names

## Notes

- This is the most complex spec
- Language generation is computationally intensive
- Cache generated names for performance
- Phoneme sets should be culturally appropriate
- Orthography makes names more readable

## References

- Original C#: `ref-projects/FantasyMapGenerator/Language/LanguageGenerator.cs`
- Algorithm: Phoneme-based procedural generation
- Inspiration: Conlang (constructed language) techniques
