namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

/// <summary>
/// Generates political states and their territories
/// </summary>
public class StatesGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public StatesGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    /// <summary>
    /// Generate all states
    /// </summary>
    public List<State> Generate(List<Burg> burgs)
    {
        Console.WriteLine("Generating states...");

        // Get capitals
        var capitals = burgs.Where(b => b != null && b.IsCapital).ToList();
        
        // Create states from capitals
        var states = CreateStates(capitals);
        Console.WriteLine($"Created {states.Count - 1} states");

        // Expand state territories
        ExpandStates(states);
        Console.WriteLine("Expanded state territories");

        // Normalize borders
        NormalizeStates(states);
        Console.WriteLine("Normalized borders");

        // Calculate statistics
        CollectStatistics(states, burgs);
        Console.WriteLine("Collected statistics");

        // Generate diplomacy
        GenerateDiplomacy(states);
        Console.WriteLine("Generated diplomacy");

        // Define state forms
        DefineStateForms(states);
        Console.WriteLine("Defined state forms");

        return states;
    }

    /// <summary>
    /// Create states from capitals
    /// </summary>
    private List<State> CreateStates(List<Burg> capitals)
    {
        var states = new List<State>
        {
            new State { Id = 0, Name = "Neutrals", Color = "#808080" }
        };

        var colors = GenerateStateColors(capitals.Count);

        for (int i = 0; i < capitals.Count; i++)
        {
            var capital = capitals[i];
            var cell = _map.Cells[capital.CellId];

            // Calculate expansionism
            double expansionism = _random.NextDouble() * _settings.SizeVariety + 1.0;

            // Generate state name (simplified for now)
            string stateName = capital.Name;

            var state = new State
            {
                Id = i + 1,
                Name = stateName,
                Color = colors[i],
                CapitalBurgId = capital.Id,
                CenterCellId = capital.CellId,
                CultureId = capital.CultureId,
                Type = DetermineStateType(cell),
                Expansionism = expansionism
            };

            states.Add(state);

            // Assign capital cell to state
            cell.StateId = i + 1;
            capital.StateId = i + 1;
        }

        return states;
    }

    /// <summary>
    /// Determine state type based on cell characteristics
    /// </summary>
    private StateType DetermineStateType(Cell cell)
    {
        // Check for port
        if (cell.Harbor > 0)
            return StateType.Naval;

        // Check for lake
        if (cell.HavenCell.HasValue)
            return StateType.Lake;

        // Check for highland
        if (cell.Height > 60)
            return StateType.Highland;

        // Check for river
        if (cell.RiverId > 0 && cell.Flux >= 100)
            return StateType.River;

        // Check for nomadic (desert/steppe)
        if (cell.Population <= 5 && (cell.BiomeId == 1 || cell.BiomeId == 2 || cell.BiomeId == 4))
            return StateType.Nomadic;

        // Check for hunting (forest)
        if (cell.BiomeId >= 5 && cell.BiomeId <= 9)
            return StateType.Hunting;

        return StateType.Generic;
    }

    /// <summary>
    /// Generate distinct colors for states
    /// </summary>
    private List<string> GenerateStateColors(int count)
    {
        var colors = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            // Generate colors with good saturation and brightness
            float hue = (i * 360f / count + _random.Next(0, 30)) % 360f;
            float saturation = 0.6f + (float)_random.NextDouble() * 0.3f;
            float lightness = 0.5f + (float)_random.NextDouble() * 0.2f;
            
            colors.Add(HSLToHex(hue, saturation, lightness));
        }
        
        return colors;
    }

    /// <summary>
    /// Convert HSL to hex color string
    /// </summary>
    private string HSLToHex(float h, float s, float l)
    {
        float c = (1 - Math.Abs(2 * l - 1)) * s;
        float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        float m = l - c / 2;

        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte rByte = (byte)((r + m) * 255);
        byte gByte = (byte)((g + m) * 255);
        byte bByte = (byte)((b + m) * 255);

        return $"#{rByte:X2}{gByte:X2}{bByte:X2}";
    }

    /// <summary>
    /// Expand state territories using Dijkstra algorithm
    /// </summary>
    private void ExpandStates(List<State> states)
    {
        var queue = new PriorityQueue<ExpansionNode, double>();
        var costs = new Dictionary<int, double>();

        double maxCost = _map.Cells.Count * 0.6 * _settings.GrowthRate;

        // Initialize: add capital cells to queue
        foreach (var state in states.Where(s => s.Id > 0))
        {
            var cell = _map.Cells[state.CenterCellId];

            queue.Enqueue(
                new ExpansionNode(state.CenterCellId, state.Id, 0, cell.BiomeId),
                0);
            costs[state.CenterCellId] = 0;
        }

        // Expand using Dijkstra
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var cell = _map.Cells[node.CellId];
            var state = states[node.StateId];

            // Expand to neighbors
            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];

                // Skip if locked state
                if (neighbor.StateId > 0 && states[neighbor.StateId].IsLocked)
                    continue;

                // Skip if capital of another state
                if (neighbor.Burg > 0)
                {
                    var burg = _map.Burgs[neighbor.Burg];
                    if (burg != null && burg.IsCapital)
                        continue;
                }

                // Calculate expansion cost
                double cost = CalculateExpansionCost(
                    cell, neighbor, state, node.NativeBiome);

                double totalCost = node.Cost + cost;

                // Stop if too expensive
                if (totalCost > maxCost)
                    continue;

                // Update if better path found
                if (!costs.ContainsKey(neighborId) || totalCost < costs[neighborId])
                {
                    if (neighbor.Height >= 20) // Land only
                        neighbor.StateId = node.StateId;

                    costs[neighborId] = totalCost;
                    queue.Enqueue(
                        new ExpansionNode(neighborId, node.StateId, totalCost, node.NativeBiome),
                        totalCost);
                }
            }
        }

        // Update burg states
        foreach (var burg in _map.Burgs.Where(b => b != null && !b.IsCapital))
        {
            burg.StateId = _map.Cells[burg.CellId].StateId;
        }
    }

    /// <summary>
    /// Calculate cost of expanding to a cell
    /// </summary>
    private double CalculateExpansionCost(
        Cell from, Cell to, State state, int nativeBiome)
    {
        // Culture cost
        double cultureCost = to.CultureId == state.CultureId ? -9 : 100;

        // Population cost (prefer populated areas)
        double populationCost = to.Height < 20 ? 0
            : to.Population > 0 ? Math.Max(20 - to.Population, 0)
            : 5000;

        // Biome cost
        double biomeCost = GetBiomeCost(nativeBiome, to.BiomeId, state.Type);

        // Height cost
        double heightCost = GetHeightCost(to, state.Type);

        // River cost
        double riverCost = GetRiverCost(to, state.Type);

        // Type cost (coastline vs inland)
        double typeCost = GetTypeCost(to, state.Type);

        double totalCost = cultureCost + populationCost + biomeCost +
                          heightCost + riverCost + typeCost;

        return Math.Max(totalCost, 0) / state.Expansionism;
    }

    private double GetBiomeCost(int nativeBiome, int biome, StateType type)
    {
        if (nativeBiome == biome) return 10; // Native biome bonus

        if (biome < 0 || biome >= _map.Biomes.Count)
            return 100;

        var biomeCost = 50; // Default cost

        if (type == StateType.Hunting)
            return biomeCost * 2; // Hunters prefer native biome

        if (type == StateType.Nomadic && biome >= 5 && biome <= 9)
            return biomeCost * 3; // Nomads avoid forests

        return biomeCost;
    }

    private double GetHeightCost(Cell cell, StateType type)
    {
        if (type == StateType.Lake && cell.HavenCell.HasValue)
            return 10; // Lake cultures cross lakes easily

        if (type == StateType.Naval && cell.Height < 20)
            return 300; // Naval states cross seas

        if (type == StateType.Nomadic && cell.Height < 20)
            return 10000; // Nomads can't cross water

        if (cell.Height < 20)
            return 1000; // General water crossing penalty

        if (type == StateType.Highland && cell.Height < 62)
            return 1100; // Highlanders prefer mountains

        if (type == StateType.Highland)
            return 0; // No penalty for highlands

        if (cell.Height >= 67)
            return 2200; // Mountain crossing

        if (cell.Height >= 44)
            return 300; // Hill crossing

        return 0;
    }

    private double GetRiverCost(Cell cell, StateType type)
    {
        if (type == StateType.River)
            return cell.RiverId > 0 ? 0 : 100;

        if (cell.RiverId == 0)
            return 0;

        return Math.Clamp(cell.Flux / 10.0, 20, 100);
    }

    private double GetTypeCost(Cell cell, StateType type)
    {
        if (cell.CoastDistance == 1) // Coastline
        {
            if (type == StateType.Naval || type == StateType.Lake)
                return 0;
            if (type == StateType.Nomadic)
                return 60;
            return 20;
        }

        if (cell.CoastDistance == 2) // Near coast
        {
            if (type == StateType.Naval || type == StateType.Nomadic)
                return 30;
            return 0;
        }

        if (cell.CoastDistance > 2) // Inland
        {
            if (type == StateType.Naval || type == StateType.Lake)
                return 100;
            return 0;
        }

        return 0;
    }

    private record ExpansionNode(int CellId, int StateId, double Cost, int NativeBiome);

    /// <summary>
    /// Normalize state borders by cleaning up irregular cells
    /// </summary>
    private void NormalizeStates(List<State> states)
    {
        foreach (var cell in _map.Cells.Where(c => c.Height >= 20))
        {
            if (cell.Burg > 0) continue; // Don't touch burg cells
            if (cell.StateId > 0 && states[cell.StateId].IsLocked) continue;

            // Check if capital is nearby
            bool nearCapital = cell.Neighbors.Any(nId =>
            {
                var neighbor = _map.Cells[nId];
                if (neighbor.Burg > 0)
                {
                    var burg = _map.Burgs[neighbor.Burg];
                    return burg != null && burg.IsCapital;
                }
                return false;
            });

            if (nearCapital) continue;

            // Count neighbors by state
            var neighborStates = cell.Neighbors
                .Where(nId => _map.Cells[nId].Height >= 20)
                .Select(nId => _map.Cells[nId].StateId)
                .Where(sId => sId >= 0 && sId < states.Count && !states[sId].IsLocked)
                .ToList();

            if (neighborStates.Count < 2) continue;

            var adversaries = neighborStates.Where(sId => sId != cell.StateId).ToList();
            var buddies = neighborStates.Where(sId => sId == cell.StateId).ToList();

            // Reassign if surrounded by different state
            if (adversaries.Count >= 2 && buddies.Count <= 2 &&
                adversaries.Count > buddies.Count)
            {
                cell.StateId = adversaries[0];
            }
        }
    }

    /// <summary>
    /// Collect statistics for all states
    /// </summary>
    private void CollectStatistics(List<State> states, List<Burg> burgs)
    {
        // Reset statistics
        foreach (var state in states.Where(s => s.Id > 0))
        {
            state.CellCount = 0;
            state.Area = 0;
            state.BurgCount = 0;
            state.RuralPopulation = 0;
            state.UrbanPopulation = 0;
            state.Neighbors.Clear();
        }

        // Count cells and calculate area
        foreach (var cell in _map.Cells.Where(c => c.Height >= 20))
        {
            if (cell.StateId <= 0 || cell.StateId >= states.Count) continue;

            var state = states[cell.StateId];
            state.CellCount++;
            state.Area += 1.0; // Simplified area calculation
            state.RuralPopulation += cell.Population;

            // Find neighbors
            foreach (var neighborId in cell.Neighbors)
            {
                var neighbor = _map.Cells[neighborId];
                if (neighbor.StateId != cell.StateId && neighbor.StateId > 0 &&
                    !state.Neighbors.Contains(neighbor.StateId))
                {
                    state.Neighbors.Add(neighbor.StateId);
                }
            }
        }

        // Count burgs and urban population
        foreach (var burg in burgs.Where(b => b != null))
        {
            if (burg.StateId <= 0 || burg.StateId >= states.Count) continue;

            var state = states[burg.StateId];
            state.BurgCount++;
            state.UrbanPopulation += burg.Population;
        }
    }

    /// <summary>
    /// Generate diplomatic relations between states
    /// </summary>
    private void GenerateDiplomacy(List<State> states)
    {
        var validStates = states.Where(s => s.Id > 0).ToList();
        if (validStates.Count < 2) return;

        double avgArea = validStates.Average(s => s.Area);

        // Initialize diplomacy
        foreach (var state in validStates)
        {
            state.Diplomacy = Enumerable.Range(0, states.Count)
                .ToDictionary(i => i, i => DiplomaticStatus.Unknown);
            state.Diplomacy[state.Id] = DiplomaticStatus.Neutral; // Self
        }

        // Set relations
        foreach (var state in validStates)
        {
            // Neighbors
            foreach (var neighborId in state.Neighbors)
            {
                if (state.Diplomacy[neighborId] != DiplomaticStatus.Unknown)
                    continue;

                var status = WeightedRandom(new Dictionary<DiplomaticStatus, int>
                {
                    { DiplomaticStatus.Ally, 1 },
                    { DiplomaticStatus.Friendly, 2 },
                    { DiplomaticStatus.Neutral, 1 },
                    { DiplomaticStatus.Suspicion, 10 },
                    { DiplomaticStatus.Rival, 9 }
                });

                // Check for vassalage
                var neighbor = states[neighborId];
                if (P(0.8) && state.Area > avgArea && neighbor.Area < avgArea &&
                    state.Area / neighbor.Area > 2)
                {
                    state.Diplomacy[neighborId] = DiplomaticStatus.Suzerain;
                    neighbor.Diplomacy[state.Id] = DiplomaticStatus.Vassal;
                }
                else
                {
                    state.Diplomacy[neighborId] = status;
                    neighbor.Diplomacy[state.Id] = status;
                }
            }
        }

        // Declare wars
        DeclareWars(states);
    }

    /// <summary>
    /// Declare wars between rival states
    /// </summary>
    private void DeclareWars(List<State> states)
    {
        foreach (var attacker in states.Where(s => s.Id > 0))
        {
            // Must have rivals and not be at war already
            if (!attacker.Diplomacy.Values.Contains(DiplomaticStatus.Rival))
                continue;
            if (attacker.Diplomacy.Values.Contains(DiplomaticStatus.Enemy))
                continue;
            if (attacker.Diplomacy.Values.Contains(DiplomaticStatus.Vassal))
                continue;

            // Find independent rival
            var rivals = attacker.Diplomacy
                .Where(kvp => kvp.Value == DiplomaticStatus.Rival)
                .Where(kvp => !states[kvp.Key].Diplomacy.Values.Contains(DiplomaticStatus.Vassal))
                .Select(kvp => kvp.Key)
                .ToList();

            if (!rivals.Any()) continue;

            int defenderId = rivals[_random.Next(rivals.Count)];
            var defender = states[defenderId];

            // Check power balance
            double attackerPower = attacker.Area * attacker.Expansionism;
            double defenderPower = defender.Area * defender.Expansionism;

            if (attackerPower < defenderPower * Gauss(1.6, 0.8, 0, 10, 2))
                continue; // Defender too strong

            // Declare war
            attacker.Diplomacy[defenderId] = DiplomaticStatus.Enemy;
            defender.Diplomacy[attacker.Id] = DiplomaticStatus.Enemy;

            // Create campaign
            string warName = $"{attacker.Name}-{defender.Name} War";
            int startYear = _settings.CurrentYear - _random.Next(2, 10);
            int endYear = startYear + _random.Next(1, 5);

            var campaign = new Campaign
            {
                Name = warName,
                StartYear = startYear,
                EndYear = endYear,
                AttackerId = attacker.Id,
                DefenderId = defenderId
            };

            attacker.Campaigns.Add(campaign);
            defender.Campaigns.Add(campaign);
        }
    }

    /// <summary>
    /// Define state forms (government types)
    /// </summary>
    private void DefineStateForms(List<State> states)
    {
        foreach (var state in states.Where(s => s.Id > 0))
        {
            // Simplified form assignment based on size
            if (state.Area > 200)
                state.Form = StateForm.Empire;
            else if (state.Area > 100)
                state.Form = StateForm.Kingdom;
            else if (state.Area > 50)
                state.Form = StateForm.Principality;
            else
                state.Form = StateForm.Duchy;

            // Generate full name
            state.FullName = $"{state.Form} of {state.Name}";
        }
    }

    /// <summary>
    /// Weighted random selection
    /// </summary>
    private T WeightedRandom<T>(Dictionary<T, int> weights) where T : notnull
    {
        int total = weights.Values.Sum();
        int roll = _random.Next(total);

        int cumulative = 0;
        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative)
                return kvp.Key;
        }

        return weights.Keys.First();
    }

    /// <summary>
    /// Gaussian distribution helper
    /// </summary>
    private double Gauss(double mean, double deviation, double min, double max, double skew)
    {
        double u = 0, v = 0;
        while (u == 0) u = _random.NextDouble();
        while (v == 0) v = _random.NextDouble();

        double num = Math.Sqrt(-2.0 * Math.Log(u)) * Math.Cos(2.0 * Math.PI * v);
        num = num * deviation + mean;

        if (skew != 0)
        {
            num = Math.Pow(num, skew);
        }

        return Math.Max(min, Math.Min(max, num));
    }

    /// <summary>
    /// Probability helper
    /// </summary>
    private bool P(double probability) => _random.NextDouble() < probability;
}
