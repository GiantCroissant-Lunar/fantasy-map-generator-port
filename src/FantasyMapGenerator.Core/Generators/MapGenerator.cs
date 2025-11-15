using System.Linq;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
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
        // Create root RNG from seed and cache seed string for reseeding if needed
        var rootRng = settings.CreateRandom();
        var seedString = settings.SeedString ?? settings.Seed.ToString();

        // Create child RNGs for each subsystem (different streams)
        var terrainRng = CreateChildRng(rootRng, 1);
        var climateRng = CreateChildRng(rootRng, 2);
        var hydrologyRng = CreateChildRng(rootRng, 3);
        var politicalRng = CreateChildRng(rootRng, 4);

        var mapData = new MapData(settings.Width, settings.Height, settings.NumPoints);

        // Generate random points based on GridMode
        var target = Math.Max(1, settings.NumPoints);
        List<Point> points;
        switch (settings.GridMode)
        {
            case GridMode.Jittered:
                {
                    var spacing = Math.Sqrt((double)settings.Width * settings.Height / target);
                    points = GeometryUtils.GenerateJitteredGridPoints(settings.Width, settings.Height, spacing, terrainRng);
                    break;
                }
            case GridMode.Poisson:
            default:
                {
                    var minDistance = Math.Sqrt((double)settings.Width * settings.Height / target);
                    points = GeometryUtils.GeneratePoissonDiskPoints(settings.Width, settings.Height, minDistance, terrainRng);
                    if (points.Count < target / 5)
                    {
                        points = GeometryUtils.GenerateUniformGridPoints(settings.Width, settings.Height, target, terrainRng);
                    }
                    break;
                }
        }

        // Apply Lloyd relaxation if enabled
        if (settings.ApplyLloydRelaxation && settings.LloydIterations > 0)
        {
            Console.WriteLine($"Applying Lloyd relaxation ({settings.LloydIterations} iterations)...");
            points = GeometryUtils.ApplyLloydRelaxation(points, settings.Width, settings.Height, settings.LloydIterations);
            Console.WriteLine($"Lloyd relaxation complete");
        }

        mapData.Points = points;

        // Create simple cells first (needed for heightmap generation)
        mapData.Cells = CreateSimpleCells(points, settings.Width, settings.Height);

        // Generate Voronoi diagram to get cell vertices and neighbors
        GenerateVoronoiDiagram(mapData, points, settings.Width, settings.Height);
        // Quick diagnostic: overall neighbor stats (pre-height)
        try
        {
            var neighborCountsAll = mapData.Cells.Select(c => c.Neighbors?.Count ?? 0).ToArray();
            double avgNeighborsAll = neighborCountsAll.Length > 0 ? neighborCountsAll.Average() : 0;
            int zeroNeighbors = neighborCountsAll.Count(n => n == 0);
            Console.WriteLine($"[Diag] Voronoi cells: {mapData.Cells.Count}, avg neighbors: {avgNeighborsAll:F2}, zero-neighbor cells: {zeroNeighbors}");
        }
        catch { }

        // Generate heightmap using either advanced noise or template system
        Console.WriteLine($"[MapGen] UseAdvancedNoise: {settings.UseAdvancedNoise}, Template: {settings.HeightmapTemplate ?? "null"}, HeightmapMode: {settings.HeightmapMode}");

        // Optional reseed before heightmap phase to mimic FMG behavior
        if (settings.ReseedAtPhaseStart && settings.RNGMode == RNGMode.Alea)
        {
            terrainRng = new AleaRandomSource(seedString);
        }

        bool useNoise = settings.HeightmapMode switch
        {
            HeightmapMode.Auto => settings.UseAdvancedNoise,
            HeightmapMode.Template => false,
            HeightmapMode.Noise => true,
            _ => settings.UseAdvancedNoise
        };

        if (useNoise)
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
        // Quick diagnostic: land neighbor coverage
        try
        {
            var land = mapData.Cells.Where(c => c.IsLand).ToList();
            int withNeighbors = land.Count(c => (c.Neighbors?.Count ?? 0) > 0);
            double avgNeighborsLand = land.Count > 0 ? land.Average(c => c.Neighbors?.Count ?? 0) : 0;
            Console.WriteLine($"[Diag] Land cells: {land.Count}, with neighbors: {withNeighbors} ({(land.Count > 0 ? withNeighbors * 100.0 / land.Count : 0):F1}%), avg neighbors (land): {avgNeighborsLand:F2}");
        }
        catch { }

        // Optional reseed before biomes
        if (settings.ReseedAtPhaseStart && settings.RNGMode == RNGMode.Alea)
        {
            climateRng = new AleaRandomSource(seedString);
        }

        // Generate biomes
        var biomeGenerator = new BiomeGenerator(mapData);
        biomeGenerator.GenerateBiomes(climateRng);

        // Generate hydrology (needs terrain + climate)
        if (settings.ReseedAtPhaseStart && settings.RNGMode == RNGMode.Alea)
        {
            hydrologyRng = new AleaRandomSource(seedString);
        }
        var hydrologyGenerator = new HydrologyGenerator(mapData, hydrologyRng);
        hydrologyGenerator.SetOptions(
            settings.HydrologyPrecipScale,
            settings.HydrologyMinFlux,
            settings.HydrologyMinRiverLength,
            settings.HydrologyAutoAdjust,
            settings.HydrologyTargetRivers,
            settings.HydrologyMinThreshold);
        hydrologyGenerator.SetMeanderingOptions(
            settings.EnableRiverMeandering,
            settings.MeanderingFactor);
        hydrologyGenerator.SetErosionOptions(
            settings.EnableRiverErosion,
            settings.MaxErosionDepth,
            settings.MinErosionHeight);
        hydrologyGenerator.SetAdvancedErosionOptions(
            settings.UseAdvancedErosion,
            settings.ErosionIterations,
            settings.ErosionAmount);
        hydrologyGenerator.SetLakeEvaporationOptions(
            settings.EnableLakeEvaporation,
            settings.BaseEvaporationRate);
        hydrologyGenerator.Generate();
        // Quick diagnostic: rivers summary
        try
        {
            int riverCount = mapData.Rivers?.Count ?? 0;
            double avgRiverCells = riverCount > 0 ? mapData.Rivers.Average(r => (double)r.Cells.Count) : 0;
            Console.WriteLine($"[Diag] Rivers: {riverCount}, avg cells per river: {avgRiverCells:F1}");
        }
        catch { }

        // Generate basic states
        GenerateBasicStates(mapData, settings, politicalRng);

        // Generate cultures
        if (settings.CultureCount > 0)
        {
            var culturesGenerator = new CulturesGenerator(mapData, politicalRng, settings);
            mapData.Cultures = culturesGenerator.Generate();
        }

        // Generate religions
        if (settings.ReligionCount > 0)
        {
            var religionsGenerator = new ReligionsGenerator(mapData, politicalRng, settings);
            mapData.Religions = religionsGenerator.Generate();
        }

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
            var state = new State
            {
                Id = i,
                Name = $"State {i + 1}",
                CapitalBurgId = capitalCell.Id,
                CenterCellId = capitalCell.Id,
                Color = GenerateRandomColor(random),
                CultureId = i % Math.Max(1, settings.NumCultures)
            };

            mapData.States.Add(state);
            capitalCell.State = i;

            var burg = new Burg
            {
                Id = i,
                Position = capitalCell.Center,
                CellId = capitalCell.Id,
                Name = state.Name,
                StateId = i,
                CultureId = state.Culture,
                Type = BurgType.Generic,
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
