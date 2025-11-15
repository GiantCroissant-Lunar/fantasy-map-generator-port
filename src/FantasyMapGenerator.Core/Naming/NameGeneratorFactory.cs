using FantasyMapGenerator.Core.Models;
using FantasyNameGenerator;
using FantasyNameGenerator.Morphology;
using FantasyNameGenerator.Phonology;
using FantasyNameGenerator.Phonotactics;

namespace FantasyMapGenerator.Core.Naming;

/// <summary>
/// Factory for creating name generators for cultures.
/// </summary>
public static class NameGeneratorFactory
{
    /// <summary>
    /// Create a name generator for a culture based on its base template.
    /// </summary>
    public static NameGenerator CreateForCulture(Culture culture, System.Random random)
    {
        // Get base phonology template based on culture type
        var phonologyTemplate = GetPhonologyTemplate(culture.Type, random);
        
        // Get phonotactic rules
        var phonotactics = GetPhonotacticTemplate(culture.Type, random);
        
        // Create morphology rules
        var morphology = new MorphologyRules(random.Next());
        
        // Create and return name generator
        return new NameGenerator(phonologyTemplate, phonotactics, morphology, random);
    }

    private static CulturePhonology GetPhonologyTemplate(CultureType cultureType, System.Random random)
    {
        // Map culture types to phonology templates
        var template = cultureType switch
        {
            CultureType.Nomadic => PhonologyTemplates.Orcish(),
            CultureType.Highland => PhonologyTemplates.Dwarvish(),
            CultureType.Hunting => PhonologyTemplates.Elvish(),
            CultureType.Lake => PhonologyTemplates.Slavic(),
            CultureType.Naval => PhonologyTemplates.Romance(),
            CultureType.River => PhonologyTemplates.Romance(),
            _ => PhonologyTemplates.Germanic()
        };
        
        // Shuffle phonemes to create variation
        template.ShufflePhonemes(random);
        return template;
    }

    private static PhonotacticRules GetPhonotacticTemplate(CultureType cultureType, System.Random random)
    {
        var template = cultureType switch
        {
            CultureType.Nomadic => PhonotacticTemplates.Orcish(),
            CultureType.Highland => PhonotacticTemplates.Dwarvish(),
            CultureType.Hunting => PhonotacticTemplates.Elvish(),
            CultureType.Lake => PhonotacticTemplates.Slavic(),
            CultureType.Naval => PhonotacticTemplates.Romance(),
            CultureType.River => PhonotacticTemplates.Romance(),
            _ => PhonotacticTemplates.Germanic()
        };
        
        // Clone to avoid modifying the template
        return template.Clone();
    }
}
