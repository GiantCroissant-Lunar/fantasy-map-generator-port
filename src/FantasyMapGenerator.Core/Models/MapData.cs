namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Root container for all map data
/// </summary>
public class MapData
{
    // Basic properties
    public int Width { get; set; }
    public int Height { get; set; }
    public int CellsX { get; set; }
    public int CellsY { get; set; }
    public int CellsDesired { get; set; }
    public double Scale { get; set; }

    // Geometry
    public List<Point> Points { get; set; } = new();
    public List<Cell> Cells { get; set; } = new();
    public List<Point> Vertices { get; set; } = new();

    // Heightmap
    public byte[]? Heights { get; set; }
    public string? HeightmapTemplate { get; set; }

    // Features
    public List<Burg> Burgs { get; set; } = new();
    public List<State> States { get; set; } = new();
    public List<Culture> Cultures { get; set; } = new();
    public List<Religion> Religions { get; set; } = new();
    public List<Biome> Biomes { get; set; } = new();

    // Rivers, lakes, and routes
    public List<River> Rivers { get; set; } = new();
    public List<Lake> Lakes { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
    public List<Marker> Markers { get; set; } = new();

    // Provinces
    public List<Province> Provinces { get; set; } = new();

    // Names and labels
    public NamesBase Names { get; set; } = new();

    // Metadata
    public DateTime Created { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Diagnostics (optional)
    public HydrologyReport? Hydrology { get; set; }

    // Landmarks and dungeons (optional high-zoom overlays)
    public List<Landmark> Landmarks { get; set; } = new();
    public List<Dungeon> Dungeons { get; set; } = new();

    // Grid data for fast access
    private Cell[][]? _grid;

    public MapData(int width, int height, int cellsDesired)
    {
        Width = width;
        Height = height;
        CellsDesired = cellsDesired;
        Scale = Math.Sqrt(width * height / cellsDesired);
        CellsX = (int)(width / Scale);
        CellsY = (int)(height / Scale);
    }

    /// <summary>
    /// Gets a cell by its ID
    /// </summary>
    public Cell? GetCell(int id)
    {
        return id >= 0 && id < Cells.Count ? Cells[id] : null;
    }

    /// <summary>
    /// Gets a cell by grid coordinates
    /// </summary>
    public Cell? GetCellAt(int x, int y)
    {
        if (_grid == null)
        {
            InitializeGrid();
        }

        return x >= 0 && x < CellsX && y >= 0 && y < CellsY ? _grid![y][x] : null;
    }

    /// <summary>
    /// Gets a cell by world coordinates using nearest-neighbor search
    /// </summary>
    public Cell? GetCellAt(double x, double y)
    {
        // Use grid as a starting hint, then search nearby cells
        int gridX = (int)(x / Scale);
        int gridY = (int)(y / Scale);

        if (_grid == null)
        {
            InitializeGrid();
        }

        // Start with grid cell if valid
        Cell? closest = null;
        double minDist = double.MaxValue;

        // Search 3x3 neighborhood around the grid cell
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int gx = gridX + dx;
                int gy = gridY + dy;

                if (gx >= 0 && gx < CellsX && gy >= 0 && gy < CellsY)
                {
                    var cell = _grid![gy][gx];
                    if (cell != null)
                    {
                        double dist = (cell.Center.X - x) * (cell.Center.X - x) +
                                     (cell.Center.Y - y) * (cell.Center.Y - y);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = cell;
                        }
                    }
                }
            }
        }

        return closest;
    }

    /// <summary>
    /// Initializes the spatial grid for fast cell lookup
    /// </summary>
    private void InitializeGrid()
    {
        _grid = new Cell[CellsY][];
        for (int y = 0; y < CellsY; y++)
        {
            _grid[y] = new Cell[CellsX];
        }

        foreach (var cell in Cells)
        {
            int gridX = (int)(cell.Center.X / Scale);
            int gridY = (int)(cell.Center.Y / Scale);

            if (gridX >= 0 && gridX < CellsX && gridY >= 0 && gridY < CellsY)
            {
                _grid[gridY][gridX] = cell;
            }
        }
    }

    /// <summary>
    /// Gets all cells belonging to a specific state
    /// </summary>
    public IEnumerable<Cell> GetStateCells(int stateId)
    {
        return Cells.Where(c => c.State == stateId);
    }

    /// <summary>
    /// Gets all cells belonging to a specific culture
    /// </summary>
    public IEnumerable<Cell> GetCultureCells(int cultureId)
    {
        return Cells.Where(c => c.Culture == cultureId);
    }

    /// <summary>
    /// Gets all land cells
    /// </summary>
    public IEnumerable<Cell> GetLandCells()
    {
        return Cells.Where(c => c.IsLand);
    }

    /// <summary>
    /// Gets all ocean cells
    /// </summary>
    public IEnumerable<Cell> GetOceanCells()
    {
        return Cells.Where(c => c.IsOcean);
    }
}

/// <summary>
/// Diagnostics for hydrology and river generation.
/// </summary>
public class HydrologyReport
{
    public int RiverFormationThreshold { get; set; }
    public int CandidateSources { get; set; }
    public int RiversGenerated { get; set; }
    public int RiversRejectedTooShort { get; set; }
    public int LakesFilled { get; set; }
    public int DownhillAssigned { get; set; }
    public int TotalLandCells { get; set; }

    // Basic distribution stats (flow accumulation on all land cells)
    public int[] AccumulationQuantiles { get; set; } = Array.Empty<int>(); // e.g., [p5,p25,p50,p75,p95]

    // Top candidate sources by accumulation (cell IDs with accumulation)
    public List<(int CellId, int Accumulation)> TopSources { get; set; } = new();

    // Per-river basics
    public List<(int RiverId, int Length, int MaxAccumulation)> Rivers { get; set; } = new();

    // Precipitation stats
    public double PrecipMin { get; set; }
    public double PrecipMax { get; set; }
    public double PrecipAvg { get; set; }
    public int FinalThresholdUsed { get; set; }
}

/// <summary>
/// Represents a river
/// </summary>
public class River
{
    public int Id { get; set; }

    /// <summary>
    /// Source cell (highest point)
    /// </summary>
    public int Source { get; set; } = -1;

    /// <summary>
    /// Mouth cell (where it enters ocean/lake)
    /// </summary>
    public int Mouth { get; set; } = -1;

    /// <summary>
    /// Cells along river path (source → mouth)
    /// </summary>
    public List<int> Cells { get; set; } = new();

    /// <summary>
    /// River width (visual representation)
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// River length (number of cells)
    /// </summary>
    public double Length { get; set; }

    /// <summary>
    /// River name (optional, for major rivers)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Is this a seasonal/intermittent river?
    /// </summary>
    public bool IsSeasonal { get; set; }

    /// <summary>
    /// Parent river ID (for tributaries)
    /// </summary>
    public int? ParentRiver { get; set; }

    /// <summary>
    /// Tributary IDs
    /// </summary>
    public List<int> Tributaries { get; set; } = new();

    /// <summary>
    /// River type based on width and flow
    /// </summary>
    public RiverType Type { get; set; }

    /// <summary>
    /// Meandered path points for smooth curve rendering.
    /// External rendering projects use these points to draw natural curves.
    /// </summary>
    public List<Point> MeanderedPath { get; set; } = new();
}

public enum RiverType
{
    Stream,
    River,
    MajorRiver
}

/// <summary>
/// Represents a trade route or road
/// </summary>
public class Route
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<int> Cells { get; set; } = new();
    public int Start { get; set; } = -1;
    public int End { get; set; } = -1;
    public RouteType Type { get; set; }
    public double Length { get; set; }
}

public enum RouteType
{
    Road,
    Trail,
    SeaRoute,
    TradeRoute
}

/// <summary>
/// Represents a map marker or point of interest
/// </summary>
public class Marker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Point Position { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string? Note { get; set; }
    public MarkerType Type { get; set; }
}

public enum MarkerType
{
    PointOfInterest,
    City,
    Mountain,
    Forest,
    Lake,
    Custom
}

/// <summary>
/// Represents a named landmark on the map (e.g., burg, ruin, dungeon entrance)
/// </summary>
public class Landmark
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Point Position { get; set; } = Point.Zero;
    public string Type { get; set; } = "generic"; // e.g., burg, dungeon
    public int? CellId { get; set; }
}

/// <summary>
/// Minimal dungeon model for high-zoom overlay
/// </summary>
public class Dungeon
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Point Origin { get; set; } = Point.Zero; // world coords for anchor
    public int Width { get; set; } = 32;
    public int Height { get; set; } = 32;
    public bool[,] Cells { get; set; } = new bool[0, 0]; // true = floor, false = wall
    public int? AnchorCellId { get; set; }
}

/// <summary>
/// Represents a province (administrative division)
/// </summary>
public class Province
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int State { get; set; } = -1;
    public int Capital { get; set; } = -1;
    public List<int> Cells { get; set; } = new();
    public string Color { get; set; } = "#000000";
    public int Population { get; set; }
}

/// <summary>
/// Contains name generation data
/// </summary>
public class NamesBase
{
    public Dictionary<string, List<string>> MaleNames { get; set; } = new();
    public Dictionary<string, List<string>> FemaleNames { get; set; } = new();
    public Dictionary<string, List<string>> BurgNames { get; set; } = new();
    public Dictionary<string, List<string>> StateNames { get; set; } = new();
    public Dictionary<string, List<string>> CultureNames { get; set; } = new();
}
