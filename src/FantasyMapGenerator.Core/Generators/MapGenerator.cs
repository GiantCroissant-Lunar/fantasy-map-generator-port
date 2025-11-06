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
        var hydrologyRng = CreateChildRng(rootRng, 3);
        var politicalRng = CreateChildRng(rootRng, 4);

        var mapData = new MapData(settings.Width, settings.Height, settings.NumPoints);
        
        // Generate random points
        // settings.NumPoints is a target count, but GeneratePoissonDiskPoints expects a minimum distance.
        // Use the same scale heuristic as MapData does for cell sizing.
        var target = Math.Max(1, settings.NumPoints);
        var minDistance = Math.Sqrt((double)settings.Width * settings.Height / target);
        var points = GeometryUtils.GeneratePoissonDiskPoints(settings.Width, settings.Height, minDistance, terrainRng);
        if (points.Count < target / 5)
        {
            // Fallback to uniform grid distribution if Poisson produced too few sites
            points = GeometryUtils.GenerateUniformGridPoints(settings.Width, settings.Height, target, terrainRng);
        }
        mapData.Points = points;
        
        // Create simple cells first (needed for heightmap generation)
        mapData.Cells = CreateSimpleCells(points, settings.Width, settings.Height);
        
        // Generate Voronoi diagram to get cell vertices and neighbors
        GenerateVoronoiDiagram(mapData, points, settings.Width, settings.Height);
        
        // Generate heightmap using either advanced noise or template system
        Console.WriteLine($"[MapGen] UseAdvancedNoise: {settings.UseAdvancedNoise}, Template: {settings.HeightmapTemplate ?? "null"}");

        if (settings.UseAdvancedNoise)
        {
            Console.WriteLine("[MapGen] Using FastNoiseHeightmapGenerator");
            var noiseGenerator = new FastNoiseHeightmapGenerator((int)settings.Seed);
            mapData.Heights = noiseGenerator.Generate(mapData, settings);

            // Print stats
            var avg = mapData.Heights.Average(h => (double)h);
            var land = mapData.Heights.Count(h => h > 30);
            var landPercent = (land * 100.0) / mapData.Heights.Length;
            Console.WriteLine($"[MapGen] FastNoise Result - Avg: {avg:F1}, Land: {landPercent:F1}%, Range: {mapData.Heights.Min()}-{mapData.Heights.Max()}");
        }
        else
        {
            Console.WriteLine("[MapGen] Using old HeightmapGenerator");
            // Use existing template-based generator
            var heightmapGenerator = new HeightmapGenerator(mapData);

            // Use template if specified, otherwise use noise
            if (!string.IsNullOrEmpty(settings.HeightmapTemplate))
            {
                mapData.Heights = heightmapGenerator.FromTemplate(settings.HeightmapTemplate, terrainRng);
            }
            else
            {
                mapData.Heights = heightmapGenerator.FromNoise(terrainRng);
            }

            // Print stats
            var avg = mapData.Heights.Average(h => (double)h);
            var land = mapData.Heights.Count(h => h > 30);
            var landPercent = (land * 100.0) / mapData.Heights.Length;
            Console.WriteLine($"[MapGen] Old Result - Avg: {avg:F1}, Land: {landPercent:F1}%, Range: {mapData.Heights.Min()}-{mapData.Heights.Max()}");
        }
        
        // Apply heights to cells
        ApplyHeightsToCells(mapData);
        
        // Generate biomes
        var biomeGenerator = new BiomeGenerator(mapData);
        biomeGenerator.GenerateBiomes(climateRng);
        
        // Generate hydrology (needs terrain + climate)
        var hydrologyGenerator = new HydrologyGenerator(mapData, hydrologyRng);
        hydrologyGenerator.Generate();
        
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
    
    private void GenerateVoronoiDiagram(MapData mapData, List<Point> points, int width, int height)
    {
        try
        {
            // Generate Voronoi diagram directly from points using NTS with boundary clipping
            var voronoi = Voronoi.FromPoints(points.ToArray(), points.Count, width, height);
            
            // Update map data with Voronoi vertices
            mapData.Vertices.Clear();
            mapData.Vertices.AddRange(voronoi.Vertices.Coordinates);
            
            // Update cells with their vertices and neighbors
            int cellsWithVertices = 0;
            int cellsWithoutVertices = 0;
            for (int i = 0; i < mapData.Cells.Count && i < voronoi.Cells.Vertices.Length; i++)
            {
                var cell = mapData.Cells[i];
                var cellVertices = voronoi.Cells.Vertices[i];
                var cellNeighbors = voronoi.Cells.Neighbors[i];

                if (cellVertices != null && cellVertices.Count > 0)
                {
                    cell.Vertices.Clear();
                    cell.Vertices.AddRange(cellVertices);
                    cellsWithVertices++;
                }
                else
                {
                    cellsWithoutVertices++;
                }

                if (cellNeighbors != null)
                {
                    cell.Neighbors.Clear();
                    cell.Neighbors.AddRange(cellNeighbors);
                }

                cell.IsBorder = voronoi.IsCellBorder(i);
            }

            Console.WriteLine($"Voronoi: {cellsWithVertices} cells with vertices, {cellsWithoutVertices} without, {mapData.Vertices.Count} total vertices");
        }
        catch (Exception ex)
        {
            // Do not inject degenerate triangle vertices. Let downstream adapters
            // synthesize safe polygons (e.g., hex fallback) for rendering when Voronoi fails.
            // Cells and their centers remain valid for overlays and adapter fallbacks.
            Console.WriteLine($"ERROR: Voronoi generation failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
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
