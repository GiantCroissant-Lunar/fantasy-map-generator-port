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
    public List<Biome> Biomes { get; set; } = new();
    
    // Rivers and routes
    public List<River> Rivers { get; set; } = new();
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
    /// Gets a cell by world coordinates
    /// </summary>
    public Cell? GetCellAt(double x, double y)
    {
        int gridX = (int)(x / Scale);
        int gridY = (int)(y / Scale);
        return GetCellAt(gridX, gridY);
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
/// Represents a river
/// </summary>
public class River
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<int> Cells { get; set; } = new();
    public int Source { get; set; } = -1;
    public int Mouth { get; set; } = -1;
    public double Length { get; set; }
    public int Width { get; set; }
    public RiverType Type { get; set; }
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