namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

public class MilitaryGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public MilitaryGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    public List<MilitaryUnit> Generate()
    {
        Console.WriteLine("Generating military units...");

        var units = new List<MilitaryUnit>();

        foreach (var state in _map.States.Where(s => s.Id > 0))
        {
            // Place garrisons
            units.AddRange(PlaceGarrisons(state));

            // Place field armies
            units.AddRange(PlaceFieldArmies(state));

            // Place navies
            units.AddRange(PlaceNavies(state));
        }

        Console.WriteLine($"Generated {units.Count} military units");
        return units;
    }

    private int CalculateMilitaryStrength(State state)
    {
        // Base strength on population
        double totalPopulation = state.RuralPopulation + state.UrbanPopulation;

        // Military participation rate
        double participationRate = _settings.MilitaryParticipationRate;

        // Adjust by state type
        if (state.Type == StateType.Nomadic)
            participationRate *= 1.5;
        else if (state.Type == StateType.Naval)
            participationRate *= 1.2;

        // Calculate total strength
        int strength = (int)(totalPopulation * participationRate);

        // Minimum strength
        return Math.Max(strength, 100);
    }

    private List<MilitaryUnit> PlaceGarrisons(State state)
    {
        var units = new List<MilitaryUnit>();

        // Get all burgs in state
        var burgs = _map.Burgs
            .Where(b => b != null && b.StateId == state.Id)
            .OrderByDescending(b => b.Population)
            .ToList();

        if (!burgs.Any()) return units;

        // Calculate garrison strength
        int totalStrength = CalculateMilitaryStrength(state);
        int garrisonStrength = (int)(totalStrength * _settings.GarrisonPercentage);

        // Distribute among burgs
        double totalBurgPop = burgs.Sum(b => b.Population);
        foreach (var burg in burgs)
        {
            // Garrison strength based on burg importance
            double burgShare = burg.Population / totalBurgPop;
            int strength = (int)(garrisonStrength * burgShare);

            if (strength < 10) continue;

            // Create garrison unit
            units.Add(new MilitaryUnit
            {
                Id = _map.MilitaryUnits.Count + units.Count + 1,
                Name = $"{burg.Name} Garrison",
                StateId = state.Id,
                CellId = burg.CellId,
                Position = burg.Position,
                Type = burg.IsPort ? UnitType.Navy : UnitType.Infantry,
                Strength = strength,
                Status = UnitStatus.Garrison,
                GarrisonBurgId = burg.Id
            });
        }

        return units;
    }

    private List<MilitaryUnit> PlaceFieldArmies(State state)
    {
        var units = new List<MilitaryUnit>();

        // Calculate field army strength
        int totalStrength = CalculateMilitaryStrength(state);
        int fieldStrength = (int)(totalStrength * _settings.FieldArmyPercentage);

        if (fieldStrength < 50) return units;

        // Number of armies based on state size
        int armyCount = Math.Max(1, state.BurgCount / 3);
        int strengthPerArmy = fieldStrength / armyCount;

        // Get border cells
        var borderCells = GetBorderCells(state);

        for (int i = 0; i < armyCount; i++)
        {
            // Find position (prefer borders)
            Cell? position = null;

            if (borderCells.Any() && P(0.7))
            {
                position = borderCells[_random.Next(borderCells.Count)];
            }
            else
            {
                // Near capital
                var capital = _map.Burgs.FirstOrDefault(b => b.Id == state.CapitalBurgId);
                if (capital != null)
                    position = _map.Cells[capital.CellId];
            }

            if (position == null) continue;

            // Determine unit type based on state
            var unitType = state.Type switch
            {
                StateType.Nomadic => UnitType.Cavalry,
                StateType.Highland => UnitType.Infantry,
                _ => i % 2 == 0 ? UnitType.Infantry : UnitType.Cavalry
            };

            units.Add(new MilitaryUnit
            {
                Id = _map.MilitaryUnits.Count + units.Count + 1,
                Name = $"{GetOrdinal(i + 1)} Army of {state.Name}",
                StateId = state.Id,
                CellId = position.Id,
                Position = position.Center,
                Type = unitType,
                Strength = strengthPerArmy,
                Status = UnitStatus.Field
            });
        }

        return units;
    }

    private List<MilitaryUnit> PlaceNavies(State state)
    {
        var units = new List<MilitaryUnit>();

        // Only for states with ports
        var ports = _map.Burgs
            .Where(b => b != null && b.StateId == state.Id && b.IsPort)
            .ToList();

        if (!ports.Any()) return units;

        // Calculate naval strength
        int totalStrength = CalculateMilitaryStrength(state);
        double navalPercentage = state.Type == StateType.Naval ? 0.4 : _settings.NavalPercentage;
        int navalStrength = (int)(totalStrength * navalPercentage);

        if (navalStrength < 20) return units;

        // Distribute among ports
        double totalPortPop = ports.Sum(p => p.Population);
        foreach (var port in ports)
        {
            double portShare = port.Population / totalPortPop;
            int strength = (int)(navalStrength * portShare);

            if (strength < 20) continue;

            units.Add(new MilitaryUnit
            {
                Id = _map.MilitaryUnits.Count + units.Count + 1,
                Name = $"{port.Name} Fleet",
                StateId = state.Id,
                CellId = port.CellId,
                Position = port.Position,
                Type = UnitType.Navy,
                Strength = strength,
                Status = UnitStatus.Garrison,
                GarrisonBurgId = port.Id
            });
        }

        return units;
    }

    private List<Cell> GetBorderCells(State state)
    {
        return _map.Cells
            .Where(c => c.StateId == state.Id)
            .Where(c => c.Neighbors.Any(n => _map.Cells[n].StateId != state.Id && _map.Cells[n].StateId > 0))
            .ToList();
    }

    private bool P(double probability)
    {
        return _random.NextDouble() < probability;
    }

    private string GetOrdinal(int number)
    {
        return number switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{number}th"
        };
    }
}
