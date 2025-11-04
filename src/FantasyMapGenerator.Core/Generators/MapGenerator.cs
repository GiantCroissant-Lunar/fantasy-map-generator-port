using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Core.Generators;

/// <summary>
/// Main map generator class that orchestrates the entire generation process
/// </summary>
public class MapGenerator
{
    public MapGenerator()
    {
    }
    
    /// <summary>
    /// Generates a complete map based on the provided settings
    /// </summary>
    public MapData Generate(MapGenerationSettings settings)
    {
        // Create root RNG from seed
        var rootRng = settings.CreateRandom();

        // Create child RNGs for each subsystem (different streams)
        var terrainRng = CreateChildRng(rootRng, 1);
        var climateRng = CreateChildRng(rootRng, 2);
        var politicalRng = CreateChildRng(rootRng, 3);

        var mapData = new MapData(settings.Width, settings.Height, settings.NumPoints);
        
        // Generate random points
        var points = GeometryUtils.GeneratePoissonDiskPoints(settings.Width, settings.Height, settings.NumPoints, terrainRng);
        mapData.Points = points;
        
        // Create simple cells first (needed for heightmap generation)
        mapData.Cells = CreateSimpleCells(points, settings.Width, settings.Height);
        
        // Generate simple heightmap
        var heightmapGenerator = new HeightmapGenerator(mapData);
        mapData.Heights = heightmapGenerator.FromNoise(terrainRng);
        
        // Apply heights to cells
        ApplyHeightsToCells(mapData);
        
        // Generate biomes
        var biomeGenerator = new BiomeGenerator(mapData);
        biomeGenerator.GenerateBiomes(climateRng);
        
        // Generate basic states
        GenerateBasicStates(mapData, settings, politicalRng);
        
        return mapData;
    }

    private IRandomSource CreateChildRng(IRandomSource parent, ulong offset)
    {
        // For PCG, use proper child creation
        if (parent is PcgRandomSource pcg)
        {
            return pcg.CreateChild(offset);
        }

        // For System.Random, use state-derived seed
        int childSeed = parent.Next();
        return new SystemRandomSource(childSeed);
    }
    

    
    private List<Cell> CreateSimpleCells(List<Point> points, int width, int height)
    {
        var cells = new List<Cell>();
        for (int i = 0; i < points.Count; i++)
        {
            var cell = new Cell(i, points[i])
            {
                Neighbors = new List<int>()
            };
            cells.Add(cell);
        }
        return cells;
    }
    
    private void ApplyHeightsToCells(MapData mapData)
    {
        if (mapData.Heights == null) return;
        
        for (int i = 0; i < mapData.Cells.Count && i < mapData.Heights.Length; i++)
        {
            mapData.Cells[i].Height = mapData.Heights[i];
        }
    }
    
    private void GenerateBasicStates(MapData mapData, MapGenerationSettings settings, IRandomSource random)
    {
        var landCells = mapData.Cells.Where(c => c.Height > settings.SeaLevel).ToList();
        
        for (int i = 0; i < Math.Min(settings.NumStates, landCells.Count); i++)
        {
            var capitalCell = landCells[random.Next(landCells.Count)];
            var state = new State(i)
            {
                Name = $"State {i + 1}",
                Capital = capitalCell.Id,
                Color = GenerateRandomColor(random),
                Culture = i % Math.Max(1, settings.NumCultures),
                Founded = DateTime.Now.AddDays(-random.Next(100, 1000))
            };
            
            mapData.States.Add(state);
            capitalCell.State = i;
            
            var burg = new Burg(i, capitalCell.Center, capitalCell.Id)
            {
                Name = state.Name,
                State = i,
                Culture = state.Culture,
                Type = BurgType.Capital,
                IsCapital = true
            };
            
            capitalCell.Burg = burg.Id;
            mapData.Burgs.Add(burg);
        }
        
        // Assign remaining land cells to nearest state
        foreach (var cell in landCells.Where(c => c.State < 0))
        {
            var nearestState = mapData.States.FirstOrDefault();
            if (nearestState != null)
            {
                cell.State = nearestState.Id;
                cell.Culture = nearestState.Culture;
            }
        }
    }
    

    
    private double Distance(Point a, Point b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }
    
    private string GenerateRandomColor(IRandomSource random)
    {
        return $"#{random.Next(256):X2}{random.Next(256):X2}{random.Next(256):X2}";
    }
}