namespace FantasyMapGenerator.Core.Models;

/// <summary>
/// Represents a lake or inland water body with evaporation modeling.
/// Based on Azgaar's Fantasy Map Generator lake system.
/// </summary>
public class Lake
{
    public int Id { get; set; }

    /// <summary>
    /// Cells that make up the lake
    /// </summary>
    public List<int> Cells { get; set; } = new();

    /// <summary>
    /// Shoreline cells (land cells adjacent to lake)
    /// </summary>
    public List<int> Shoreline { get; set; } = new();

    /// <summary>
    /// Cell where water exits the lake (if any)
    /// </summary>
    public int OutletCell { get; set; } = -1;

    /// <summary>
    /// River ID that drains this lake
    /// </summary>
    public int? OutletRiver { get; set; }

    /// <summary>
    /// Total water flux entering the lake (m³/s)
    /// </summary>
    public double Inflow { get; set; }

    /// <summary>
    /// Water lost to evaporation (m³/s)
    /// </summary>
    public double Evaporation { get; set; }

    /// <summary>
    /// Net flux leaving the lake (Inflow - Evaporation)
    /// </summary>
    public double NetOutflow => Math.Max(Inflow - Evaporation, 0);

    /// <summary>
    /// True if lake has no outlet (evaporation >= inflow)
    /// </summary>
    public bool IsClosed => Evaporation >= Inflow;

    /// <summary>
    /// Lake type based on closure and salinity
    /// </summary>
    public LakeType Type { get; set; }

    /// <summary>
    /// Average temperature of lake cells
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Average precipitation over lake
    /// </summary>
    public double Precipitation { get; set; }

    /// <summary>
    /// Surface area in square kilometers
    /// </summary>
    public double SurfaceArea { get; set; }

    /// <summary>
    /// Rivers flowing into this lake
    /// </summary>
    public List<int> InflowingRivers { get; set; } = new();
}

/// <summary>
/// Classification of lake types based on hydrology
/// </summary>
public enum LakeType
{
    /// <summary>
    /// Open lake with outlet (normal lake)
    /// </summary>
    Freshwater,

    /// <summary>
    /// Closed lake with no outlet (salt lake like Dead Sea)
    /// </summary>
    Saltwater,

    /// <summary>
    /// Partially closed lake (brackish water)
    /// </summary>
    Brackish,

    /// <summary>
    /// Lake that dries up seasonally
    /// </summary>
    Seasonal
}
