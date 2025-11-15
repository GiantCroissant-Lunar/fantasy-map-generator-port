# Spec 014: Advanced Name Generation System

## Status
- **State:** In Progress
- **Priority:** ⭐⭐⭐⭐ Important
- **Estimated Effort:** 2 weeks
- **Dependencies:** None (standalone library)
- **Blocks:** None (enhances existing features)
- **Project:** `FantasyNameGenerator` (separate library)

## Overview

Implement an advanced linguistic name generation system as a **standalone reusable library** that creates culturally-appropriate names for places, people, and features. The system uses phoneme-based language generation with morphemes, syllable structures, and orthographic rules to create realistic-sounding names.

**Key Design Decisions:**
- ✅ **Unique Language per Culture** - Each culture gets its own language variant (mutated from templates)
- ✅ **Full Orthography/Spelling Rules** - Readable names with consistent spelling conventions
- ✅ **Fantasy Language Support** - Built-in Elvish, Dwarvish, Orcish alongside real-world inspired languages
- ✅ **JSON-Based Extensibility** - Easy to add new language templates via JSON files
- ✅ **Standalone Library** - Separate `FantasyNameGenerator` project for reusability

## Architecture Layers

The system follows a layered architecture inspired by conlang (constructed language) theory and RimWorld's RulePacks:

```
┌─────────────────────────────────────────────────────────┐
│ NameTypes (burg, state, river, religion, culture, army)│  ← High-level API
├─────────────────────────────────────────────────────────┤
│ GrammarEngine (RimWorld RulePacks concept)              │  ← Grammar rules
├─────────────────────────────────────────────────────────┤
│ MorphologyRules (morphemes, word formation)             │  ← Word building
├─────────────────────────────────────────────────────────┤
│ PhonotacticRules (conlang-namegen concepts)             │  ← Sound patterns
├─────────────────────────────────────────────────────────┤
│ CulturePhonology (libconlang inspired)                  │  ← Phoneme inventory
├─────────────────────────────────────────────────────────┤
│ SyllableGenerator (base layer)                          │  ← Core generation
└─────────────────────────────────────────────────────────┘
```

**Layer Responsibilities:**

1. **SyllableGenerator** - Core syllable construction from phonemes
2. **CulturePhonology** - Phoneme inventory and sound system per culture (libconlang concepts)
3. **PhonotacticRules** - Legal sound combinations and constraints (conlang-namegen)
4. **MorphologyRules** - Morpheme tracking and word formation rules
5. **GrammarEngine** - Grammatical constructions (compounds, genitives, etc.) inspired by RimWorld RulePacks
6. **NameTypes** - High-level API for specific name types (burg, state, river, religion, culture, army)

## Goals

1. **Language Templates** - Define base languages (Germanic, Romance, Elvish, Dwarvish, etc.)
2. **Language Mutation** - Create unique variants per culture from templates
3. **Phoneme Systems** - Define consonants, vowels, and special sounds
4. **Syllable Structures** - Control how syllables are formed
5. **Morpheme Tracking** - Semantic meaning in word parts
6. **Orthographic Rules** - Spelling conventions for readability
7. **Name Generation** - Generate names for burgs, states, features, people
8. **JSON Configuration** - Load language templates from JSON files

## Reference Implementation

**Source:** `ref-projects/FantasyMapGenerator/Language/LanguageGenerator.cs`

## Layer 1: SyllableGenerator

**Purpose:** Core syllable construction from phoneme patterns

```csharp
namespace FantasyNameGenerator.Core;

public class SyllableGenerator
{
    private readonly Random _random;
    
    public string GenerateSyllable(string pattern, Dictionary<char, string> phonemeMap)
    {
        var syllable = new StringBuilder();
        
        foreach (char symbol in pattern)
        {
            if (symbol == '?') continue; // Optional marker
            
            if (phonemeMap.TryGetValue(symbol, out string? phonemes))
            {
                int index = _random.Next(phonemes.Length);
                syllable.Append(phonemes[index]);
            }
        }
        
        return syllable.ToString();
    }
}
```

## Layer 2: CulturePhonology (libconlang)

**Purpose:** Define phoneme inventory and sound system per culture

```csharp
namespace FantasyNameGenerator.Phonology;

public class CulturePhonology
{
    public string Name { get; set; } = string.Empty;
    
    // Phoneme inventory (IPA symbols)
    public PhonemeInventory Inventory { get; set; } = new();
    
    // Phoneme frequency weights
    public Dictionary<char, double> Weights { get; set; } = new();
    
    // Allophonic rules (contextual sound changes)
    public List<AllophoneRule> Allophones { get; set; } = new();
}

public class PhonemeInventory
{
    public string Consonants { get; set; } = string.Empty;
    public string Vowels { get; set; } = string.Empty;
    public string Liquids { get; set; } = string.Empty;
    public string Nasals { get; set; } = string.Empty;
    public string Fricatives { get; set; } = string.Empty;
    public string Stops { get; set; } = string.Empty;
}

public class AllophoneRule
{
    public char Phoneme { get; set; }
    public char Allophone { get; set; }
    public string Context { get; set; } = string.Empty; // Regex pattern
}
```

## Layer 3: PhonotacticRules (conlang-namegen)

**Purpose:** Define legal sound combinations and phonotactic constraints

```csharp
namespace FantasyNameGenerator.Phonotactics;

public class PhonotacticRules
{
    // Syllable structure patterns (e.g., "CVC", "CV", "CCVC")
    public List<string> AllowedStructures { get; set; } = new();
    
    // Forbidden phoneme sequences (regex patterns)
    public List<string> ForbiddenSequences { get; set; } = new();
    
    // Onset constraints (syllable-initial)
    public List<string> AllowedOnsets { get; set; } = new();
    
    // Coda constraints (syllable-final)
    public List<string> AllowedCodas { get; set; } = new();
    
    // Cluster constraints
    public int MaxConsonantCluster { get; set; } = 2;
    public int MaxVowelCluster { get; set; } = 2;
    
    // Sonority sequencing
    public bool EnforceSonoritySequencing { get; set; } = true;
    
    public bool IsValidSyllable(string syllable)
    {
        // Check against forbidden sequences
        foreach (var forbidden in ForbiddenSequences)
        {
            if (Regex.IsMatch(syllable, forbidden, RegexOptions.IgnoreCase))
                return false;
        }
        
        // Check cluster constraints
        // Check sonority sequencing
        // etc.
        
        return true;
    }
}
```

## Layer 4: MorphologyRules

**Purpose:** Morpheme tracking and word formation rules

```csharp
namespace FantasyNameGenerator.Morphology;

public class MorphologyRules
{
    // Morpheme database (semantic -> phonetic forms)
    public Dictionary<string, List<string>> Morphemes { get; set; } = new();
    
    // Derivational affixes
    public List<Affix> Prefixes { get; set; } = new();
    public List<Affix> Suffixes { get; set; } = new();
    public List<Affix> Infixes { get; set; } = new();
    
    // Compounding rules
    public CompoundingRules Compounding { get; set; } = new();
    
    // Morphophonemic rules (sound changes at morpheme boundaries)
    public List<MorphophonemicRule> MorphophonemicRules { get; set; } = new();
    
    public string GetOrCreateMorpheme(string semanticKey, SyllableGenerator generator)
    {
        if (!Morphemes.ContainsKey(semanticKey))
            Morphemes[semanticKey] = new List<string>();
        
        var list = Morphemes[semanticKey];
        
        // Return existing or generate new
        if (list.Count > 0 && _random.NextDouble() < 0.7)
            return list[_random.Next(list.Count)];
        
        var newMorpheme = generator.GenerateSyllable(/* ... */);
        list.Add(newMorpheme);
        return newMorpheme;
    }
}

public class Affix
{
    public string Form { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public double Frequency { get; set; } = 0.1;
}

public class CompoundingRules
{
    public string Joiner { get; set; } = " ";
    public bool HeadFirst { get; set; } = true; // Head-initial vs head-final
    public double CompoundProbability { get; set; } = 0.3;
}

public class MorphophonemicRule
{
    public string Pattern { get; set; } = string.Empty; // Regex
    public string Replacement { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty; // Where it applies
}
```

## Layer 5: GrammarEngine (RimWorld RulePacks)

**Purpose:** Grammatical constructions and name templates

```csharp
namespace FantasyNameGenerator.Grammar;

public class GrammarEngine
{
    private readonly MorphologyRules _morphology;
    
    // Grammar rules (inspired by RimWorld RulePacks)
    public Dictionary<string, List<GrammarRule>> RulePacks { get; set; } = new();
    
    public string ApplyGrammarRule(string ruleKey, Dictionary<string, string> context)
    {
        if (!RulePacks.TryGetValue(ruleKey, out var rules))
            return string.Empty;
        
        var rule = rules[_random.Next(rules.Count)];
        return rule.Apply(context, this);
    }
}

public class GrammarRule
{
    public string Pattern { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    
    // Pattern examples:
    // "[word]" - simple word
    // "[word] [word]" - compound
    // "[word] [genitive] [word]" - genitive construction
    // "[definite] [word]" - with article
    // "[prefix][word]" - with affix
    
    public string Apply(Dictionary<string, string> context, GrammarEngine engine)
    {
        var result = Pattern;
        
        // Replace placeholders with actual words/morphemes
        var matches = Regex.Matches(Pattern, @"\[(\w+)\]");
        foreach (Match match in matches)
        {
            string key = match.Groups[1].Value;
            if (context.TryGetValue(key, out string? value))
            {
                result = result.Replace(match.Value, value);
            }
        }
        
        return result;
    }
}

// Example RulePacks
public static class DefaultRulePacks
{
    public static Dictionary<string, List<GrammarRule>> GetBurgRules()
    {
        return new()
        {
            ["simple"] = new()
            {
                new GrammarRule { Pattern = "[word]", Weight = 0.5 },
                new GrammarRule { Pattern = "[word][suffix]", Weight = 0.3 },
            },
            ["compound"] = new()
            {
                new GrammarRule { Pattern = "[word1][joiner][word2]", Weight = 0.4 },
                new GrammarRule { Pattern = "[word1][genitive][word2]", Weight = 0.3 },
            },
            ["descriptive"] = new()
            {
                new GrammarRule { Pattern = "[adjective][joiner][word]", Weight = 0.5 },
                new GrammarRule { Pattern = "[definite][joiner][word]", Weight = 0.1 },
            }
        };
    }
}
```

## Layer 6: NameTypes (High-Level API)

**Purpose:** Specific name generation for different entity types

```csharp
namespace FantasyNameGenerator.API;

public class NameGenerator
{
    private readonly GrammarEngine _grammar;
    private readonly MorphologyRules _morphology;
    private readonly PhonotacticRules _phonotactics;
    private readonly CulturePhonology _phonology;
    
    // High-level API for each name type
    public string GenerateBurgName(NameContext context)
    {
        var semanticKeys = new[] { "settlement", "town", "city", "fort" };
        return GenerateName("burg", semanticKeys, context);
    }
    
    public string GenerateStateName(NameContext context)
    {
        var semanticKeys = new[] { "kingdom", "empire", "realm", "land" };
        return GenerateName("state", semanticKeys, context);
    }
    
    public string GenerateRiverName(NameContext context)
    {
        var semanticKeys = new[] { "river", "water", "flow", "stream" };
        return GenerateName("river", semanticKeys, context);
    }
    
    public string GenerateReligionName(NameContext context)
    {
        var semanticKeys = new[] { "god", "faith", "divine", "holy" };
        return GenerateName("religion", semanticKeys, context);
    }
    
    public string GenerateCultureName(NameContext context)
    {
        var semanticKeys = new[] { "people", "folk", "tribe", "clan" };
        return GenerateName("culture", semanticKeys, context);
    }
    
    public string GenerateArmyName(NameContext context)
    {
        var semanticKeys = new[] { "army", "legion", "host", "guard" };
        return GenerateName("army", semanticKeys, context);
    }
    
    private string GenerateName(string nameType, string[] semanticKeys, NameContext context)
    {
        // Select appropriate grammar rule pack
        var ruleKey = SelectRuleKey(nameType, context);
        
        // Build context with morphemes
        var grammarContext = new Dictionary<string, string>();
        foreach (var key in semanticKeys)
        {
            grammarContext[key] = _morphology.GetOrCreateMorpheme(key, /* ... */);
        }
        
        // Apply grammar rules
        var name = _grammar.ApplyGrammarRule(ruleKey, grammarContext);
        
        // Apply orthography
        name = ApplyOrthography(name);
        
        // Capitalize
        name = Capitalize(name);
        
        return name;
    }
}

public class NameContext
{
    public int Seed { get; set; }
    public string? CultureId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

## Data Models

```csharp
namespace FantasyNameGenerator.Models;

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

### Phase 1: Foundation Layers (Days 1-4)

#### Step 1: Layer 1 - SyllableGenerator (Day 1)
- [ ] Create `SyllableGenerator.cs`
- [ ] Implement basic syllable construction
- [ ] Add pattern parsing (C, V, L, S, F, ?)
- [ ] Unit tests for syllable generation

#### Step 2: Layer 2 - CulturePhonology (Day 2)
- [ ] Create `CulturePhonology.cs` and `PhonemeInventory.cs`
- [ ] Implement phoneme inventory system
- [ ] Add phoneme frequency weights
- [ ] Create allophone rules
- [ ] Unit tests for phonology

#### Step 3: Layer 3 - PhonotacticRules (Day 3)
- [ ] Create `PhonotacticRules.cs`
- [ ] Implement syllable structure validation
- [ ] Add forbidden sequence checking
- [ ] Implement onset/coda constraints
- [ ] Add sonority sequencing
- [ ] Unit tests for phonotactics

#### Step 4: Layer 4 - MorphologyRules (Day 4)
- [ ] Create `MorphologyRules.cs`
- [ ] Implement morpheme database
- [ ] Add affix system (prefix/suffix/infix)
- [ ] Implement compounding rules
- [ ] Add morphophonemic rules
- [ ] Unit tests for morphology

### Phase 2: Grammar & API (Days 5-7)

#### Step 5: Layer 5 - GrammarEngine (Day 5-6)
- [ ] Create `GrammarEngine.cs` and `GrammarRule.cs`
- [ ] Implement RulePack system
- [ ] Add pattern matching and replacement
- [ ] Create default RulePacks for each name type
- [ ] Unit tests for grammar engine

#### Step 6: Layer 6 - NameTypes API (Day 7)
- [ ] Create `NameGenerator.cs` (high-level API)
- [ ] Implement `GenerateBurgName()`
- [ ] Implement `GenerateStateName()`
- [ ] Implement `GenerateRiverName()`
- [ ] Implement `GenerateReligionName()`
- [ ] Implement `GenerateCultureName()`
- [ ] Implement `GenerateArmyName()`
- [ ] Unit tests for each name type

### Phase 3: Language Templates (Days 8-10)

#### Step 7: JSON Language System (Day 8)
- [ ] Create JSON schema for language templates
- [ ] Implement JSON loader
- [ ] Add language mutation system
- [ ] Unit tests for JSON loading

#### Step 8: Built-in Language Templates (Day 9-10)
- [ ] Create `germanic.json`
- [ ] Create `romance.json`
- [ ] Create `slavic.json`
- [ ] Create `elvish.json` (fantasy)
- [ ] Create `dwarvish.json` (fantasy)
- [ ] Create `orcish.json` (fantasy)
- [ ] Integration tests for all templates

### Phase 4: Integration & Polish (Days 11-14)

#### Step 9: Orthography System (Day 11)
- [ ] Implement orthography rules
- [ ] Add spelling conventions
- [ ] Create readable output
- [ ] Unit tests for orthography

#### Step 10: Quality Assurance (Day 12)
- [ ] Implement duplicate detection
- [ ] Add name length constraints
- [ ] Add pronounceability scoring
- [ ] Performance optimization

#### Step 11: Integration with Map Generator (Day 13)
- [ ] Add project reference to `FantasyMapGenerator.Core`
- [ ] Integrate with `CulturesGenerator`
- [ ] Integrate with `BurgsGenerator`
- [ ] Integrate with `StatesGenerator`
- [ ] Integration tests

#### Step 12: Documentation (Day 14)
- [ ] Update README with examples
- [ ] Document each layer
- [ ] Add usage guide
- [ ] Create language template guide

## Language Templates

Language templates are defined in JSON files under `Data/Languages/`:

```json
{
  "name": "elvish",
  "description": "Flowing, melodic language inspired by Tolkien",
  "phonemes": {
    "C": "lmnrwyfθð",
    "V": "aeiouəɛ",
    "L": "lrwy",
    "S": "sʃ",
    "F": "nml"
  },
  "structures": ["CV", "CVC", "V"],
  "restricts": ["θθ", "ðð", "ww"],
  "consonantOrthography": {
    "θ": "th",
    "ð": "dh",
    "ʃ": "sh"
  },
  "vowelOrthography": {
    "ə": "e",
    "ɛ": "e"
  },
  "minSyllables": 2,
  "maxSyllables": 4,
  "joiner": " ",
  "exponent": 2
}
```

**Built-in Templates:**
- `germanic.json` - Hard consonants, compound words
- `romance.json` - Flowing vowels, Latin-inspired
- `slavic.json` - Consonant clusters, palatalization
- `elvish.json` - Melodic, flowing (fantasy)
- `dwarvish.json` - Harsh, guttural (fantasy)
- `orcish.json` - Simple, brutal (fantasy)

## Language Mutation

Each culture gets a **unique language variant** mutated from a template:

```csharp
public Language MutateLanguage(Language template, int seed)
{
    var rng = new Random(seed);
    var mutated = template.Clone();
    
    // Shuffle phoneme order (changes frequency)
    mutated.Phonemes["C"] = Shuffle(mutated.Phonemes["C"], rng);
    mutated.Phonemes["V"] = Shuffle(mutated.Phonemes["V"], rng);
    
    // Randomly adjust syllable counts
    mutated.MinSyllables += rng.Next(-1, 2);
    mutated.MaxSyllables += rng.Next(-1, 2);
    
    // Randomly change structure preference
    if (rng.NextDouble() < 0.3)
    {
        mutated.Structure = mutated.Structures[rng.Next(mutated.Structures.Length)];
    }
    
    return mutated;
}
```

## Configuration

```csharp
public class NameGeneratorSettings
{
    /// <summary>
    /// Minimum name length
    /// </summary>
    public int MinNameLength { get; set; } = 3;
    
    /// <summary>
    /// Maximum name length
    /// </summary>
    public int MaxNameLength { get; set; } = 20;
    
    /// <summary>
    /// Path to language template JSON files
    /// </summary>
    public string LanguageDataPath { get; set; } = "Data/Languages";
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
