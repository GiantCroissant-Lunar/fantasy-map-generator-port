namespace FantasyMapGenerator.Core.Generators;

using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

public class ReligionsGenerator
{
    private readonly MapData _map;
    private readonly IRandomSource _random;
    private readonly MapGenerationSettings _settings;

    public ReligionsGenerator(MapData map, IRandomSource random, MapGenerationSettings settings)
    {
        _map = map;
        _random = random;
        _settings = settings;
    }

    public List<Religion> Generate()
    {
        Console.WriteLine("Generating religions...");

        var religions = GenerateReligions(_settings.ReligionCount);
        Console.WriteLine($"Created {religions.Count - 1} religions");

        PlaceReligionOrigins(religions);
        Console.WriteLine("Placed religion origins");

        ExpandReligions(religions);
        Console.WriteLine("Expanded religions");

        CollectStatistics(religions);
        Console.WriteLine("Collected statistics");

        return religions;
    }

    private List<Religion> GenerateReligions(int count)
    {
        var religions = new List<Religion>
        {
            new Religion { Id = 0, Name = "No Religion", Color = "#808080" }
        };

        var colors = GenerateReligionColors(count);

        for (int i = 1; i <= count; i++)
        {
            var religion = new Religion
            {
                Id = i,
                Name = $"Religion{i}",
                Color = colors[i - 1],
                Type = DetermineReligionType(),
                Expansion = DetermineExpansionType(),
                Form = DetermineReligionForm()
            };

            religion.Deities = GenerateDeities(religion.Form);
            religions.Add(religion);
        }

        return religions;
    }

    private ReligionType DetermineReligionType()
    {
        return WeightedRandom(new Dictionary<ReligionType, int>
        {
            { ReligionType.Organized, 50 },
            { ReligionType.Folk, 30 },
            { ReligionType.Cult, 15 },
            { ReligionType.Heresy, 5 }
        });
    }

    private ExpansionType DetermineExpansionType()
    {
        return WeightedRandom(new Dictionary<ExpansionType, int>
        {
            { ExpansionType.Global, 20 },
            { ExpansionType.State, 10 },
            { ExpansionType.Culture, 40 },
            { ExpansionType.Homeland, 30 }
        });
    }

    private string DetermineReligionForm()
    {
        var forms = new[] { "Pantheon", "Monotheism", "Dualism", "Animism", "Ancestor Worship" };
        return forms[_random.Next(forms.Length)];
    }

    private List<Deity> GenerateDeities(string form)
    {
        var deities = new List<Deity>();
        var spheres = new[] { "War", "Love", "Death", "Life", "Knowledge",
                              "Nature", "Sky", "Sea", "Fire", "Earth" };

        int count = form switch
        {
            "Monotheism" => 1,
            "Dualism" => 2,
            "Pantheon" => _random.Next(3, 12),
            _ => _random.Next(1, 5)
        };

        for (int i = 0; i < count; i++)
        {
            deities.Add(new Deity
            {
                Name = $"Deity{i + 1}",
                Sphere = spheres[_random.Next(spheres.Length)]
            });
        }

        return deities;
    }

    private void PlaceReligionOrigins(List<Religion> religions)
    {
        var populated = _map.Cells.Where(c => c.Population > 0).ToList();
        var originTree = new QuadTree(_map.Width, _map.Height);

        double spacing = (_map.Width + _map.Height) / 2.0 / religions.Count;

        foreach (var religion in religions.Skip(1))
        {
            int centerCell = FindReligionOrigin(populated, originTree, spacing);

            religion.CenterCellId = centerCell;
            religion.Origins.Add(_map.Cells[centerCell].CultureId);

            _map.Cells[centerCell].ReligionId = religion.Id;
            originTree.Add(_map.Cells[centerCell].Center, religion.Id);
        }
    }

    private int FindReligionOrigin(List<Cell> populated, QuadTree originTree, double spacing)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var cell = populated[_random.Next(populated.Count)];

            if (cell.ReligionId == 0 && originTree.FindNearest(cell.Center, spacing) == null)
            {
                return cell.Id;
            }

            if (attempt % 20 == 19)
                spacing *= 0.9;
        }

        return populated.First(c => c.ReligionId == 0).Id;
    }

    private void ExpandReligions(List<Religion> religions)
    {
        foreach (var religion in religions.Skip(1))
        {
            switch (religion.Expansion)
            {
                case ExpansionType.Global:
                    ExpandGlobal(religion);
                    break;
                case ExpansionType.State:
                    ExpandByState(religion);
                    break;
                case ExpansionType.Culture:
                    ExpandByCulture(religion);
                    break;
                case ExpansionType.Homeland:
                    ExpandHomeland(religion);
                    break;
            }
        }
    }

    private void ExpandGlobal(Religion religion)
    {
        var center = _map.Cells[religion.CenterCellId];
        double maxDistance = Math.Sqrt(_map.Width * _map.Width + _map.Height * _map.Height);

        foreach (var cell in _map.Cells.Where(c => c.Population > 0 && c.ReligionId == 0))
        {
            double distance = Distance(center.Center, cell.Center);
            double probability = 1.0 - (distance / maxDistance);

            if (_random.NextDouble() < probability * 0.5)
            {
                cell.ReligionId = religion.Id;
            }
        }
    }

    private void ExpandByState(Religion religion)
    {
        var originState = _map.Cells[religion.CenterCellId].StateId;

        foreach (var cell in _map.Cells.Where(c => c.StateId == originState && c.ReligionId == 0))
        {
            if (_random.NextDouble() < 0.9)
            {
                cell.ReligionId = religion.Id;
            }
        }
    }

    private void ExpandByCulture(Religion religion)
    {
        var originCulture = _map.Cells[religion.CenterCellId].CultureId;

        foreach (var cell in _map.Cells.Where(c => c.CultureId == originCulture && c.ReligionId == 0))
        {
            if (_random.NextDouble() < 0.8)
            {
                cell.ReligionId = religion.Id;
            }
        }
    }

    private void ExpandHomeland(Religion religion)
    {
        var center = _map.Cells[religion.CenterCellId];
        double maxDistance = (_map.Width + _map.Height) / 10.0;

        foreach (var cell in _map.Cells.Where(c => c.Population > 0 && c.ReligionId == 0))
        {
            double distance = Distance(center.Center, cell.Center);
            if (distance < maxDistance && _random.NextDouble() < 0.7)
            {
                cell.ReligionId = religion.Id;
            }
        }
    }

    private void CollectStatistics(List<Religion> religions)
    {
        foreach (var religion in religions.Where(r => r.Id > 0))
        {
            religion.CellCount = 0;
            religion.Area = 0;
            religion.RuralPopulation = 0;
            religion.UrbanPopulation = 0;
        }

        foreach (var cell in _map.Cells.Where(c => c.Population > 0))
        {
            if (cell.ReligionId <= 0 || cell.ReligionId >= religions.Count) continue;

            var religion = religions[cell.ReligionId];
            religion.CellCount++;
            religion.Area += 1.0;
            religion.RuralPopulation += cell.Population;
        }

        if (_map.Burgs != null)
        {
            foreach (var burg in _map.Burgs.Where(b => b != null))
            {
                var cell = _map.Cells[burg.CellId];
                if (cell.ReligionId <= 0 || cell.ReligionId >= religions.Count) continue;

                var religion = religions[cell.ReligionId];
                religion.UrbanPopulation += burg.Population;
            }
        }
    }

    private List<string> GenerateReligionColors(int count)
    {
        var colors = new List<string>();

        for (int i = 0; i < count; i++)
        {
            float hue = (i * 360f / count + _random.Next(0, 30)) % 360f;
            float saturation = 0.4f + (float)_random.NextDouble() * 0.3f;
            float lightness = 0.5f + (float)_random.NextDouble() * 0.2f;

            colors.Add(HSLToHex(hue, saturation, lightness));
        }

        return colors;
    }

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

    private double Distance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

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
}
