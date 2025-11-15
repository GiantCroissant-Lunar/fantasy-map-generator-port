# FantasyNameGenerator

A standalone .NET library for generating linguistically-consistent fantasy names for people, places, and cultures.

## Features

- **Unique Languages per Culture**: Each culture gets its own language variant mutated from templates
- **Phonetically-Based Generation**: Uses realistic phoneme sets and syllable patterns
- **Spelling/Orthography Rules**: Generates readable names with consistent spelling conventions
- **Fantasy Language Support**: Built-in support for Elvish, Dwarvish, Orcish, and real-world inspired languages
- **JSON-Based Extensibility**: Easy to add new language templates via JSON configuration
- **Deterministic**: Same seed produces same names for reproducibility

## Usage

```csharp
// Basic usage example (to be implemented)
var generator = new NameGenerator(seed: 12345);
var language = generator.CreateLanguage("elvish");
var personName = generator.GeneratePersonName(language);
var placeName = generator.GeneratePlaceName(language, NameType.Settlement);
```

## Architecture

- **Models**: Core data structures (Language, PhonemeSet, SyllablePattern, etc.)
- **Generators**: Name generation logic
- **Data**: Language templates (JSON files)
- **Services**: High-level API for name generation

## Status

🚧 **In Development** - Phase 1 implementation in progress
