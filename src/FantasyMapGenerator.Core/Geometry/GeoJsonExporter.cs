using System.Text.Json;
using System.Text.Json.Serialization;
using FantasyMapGenerator.Core.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using FmgPoint = FantasyMapGenerator.Core.Models.Point;

namespace FantasyMapGenerator.Core.Geometry;

/// <summary>
/// Exports map data to GeoJSON format for external tool compatibility
/// </summary>
public class GeoJsonExporter
{
    private readonly NtsGeometryAdapter _adapter;
    private readonly GeometryFactory _factory;
    private readonly GeoJsonWriter _geoJsonWriter;

    /// <summary>
    /// Configuration for GeoJSON export
    /// </summary>
    public class GeoJsonExportConfig
    {
        /// <summary>
        /// Include cell geometries
        /// </summary>
        public bool IncludeCells { get; set; } = true;

        /// <summary>
        /// Include state boundaries
        /// </summary>
        public bool IncludeStateBoundaries { get; set; } = true;

        /// <summary>
        /// Include rivers
        /// </summary>
        public bool IncludeRivers { get; set; } = true;

        /// <summary>
        /// Include burgs (cities/towns)
        /// </summary>
        public bool IncludeBurgs { get; set; } = true;

        /// <summary>
        /// Include elevation data as properties
        /// </summary>
        public bool IncludeElevation { get; set; } = true;

        /// <summary>
        /// Include precipitation data as properties
        /// </summary>
        public bool IncludePrecipitation { get; set; } = true;

        /// <summary>
        /// Include biome data as properties
        /// </summary>
        public bool IncludeBiomes { get; set; } = true;

        /// <summary>
        /// Simplify geometries for smaller file size
        /// </summary>
        public bool SimplifyGeometries { get; set; } = false;

        /// <summary>
        /// Simplification tolerance when SimplifyGeometries is true
        /// </summary>
        public double SimplificationTolerance { get; set; } = 0.1;

        /// <summary>
        /// Export coordinate system (default: WGS84)
        /// </summary>
        public string Crs { get; set; } = "EPSG:4326";
    }

    public GeoJsonExporter()
    {
        _adapter = new NtsGeometryAdapter();
        _factory = new GeometryFactory();
        _geoJsonWriter = new GeoJsonWriter();
    }

    /// <summary>
    /// Export map data to GeoJSON string
    /// </summary>
    /// <param name="mapData">Map data to export</param>
    /// <param name="config">Export configuration</param>
    /// <returns>GeoJSON string</returns>
    public string ExportToGeoJson(MapData mapData, GeoJsonExportConfig? config = null)
    {
        config ??= new GeoJsonExportConfig();

        var features = new List<GeoJsonFeature>();

        // Export cells
        if (config.IncludeCells)
        {
            var cellFeatures = ExportCells(mapData, config);
            features.AddRange(cellFeatures);
        }

        // Export state boundaries
        if (config.IncludeStateBoundaries)
        {
            var boundaryFeatures = ExportStateBoundaries(mapData, config);
            features.AddRange(boundaryFeatures);
        }

        // Export rivers
        if (config.IncludeRivers)
        {
            var riverFeatures = ExportRivers(mapData, config);
            features.AddRange(riverFeatures);
        }

        // Export burgs
        if (config.IncludeBurgs)
        {
            var burgFeatures = ExportBurgs(mapData, config);
            features.AddRange(burgFeatures);
        }

        var featureCollection = new GeoJsonFeatureCollection
        {
            Type = "FeatureCollection",
            Features = features
        };

        // Add CRS information
        if (!string.IsNullOrEmpty(config.Crs))
        {
            featureCollection.Crs = new GeoJsonCrs
            {
                Type = "name",
                Properties = new { name = config.Crs }
            };
        }

        return JsonSerializer.Serialize(featureCollection, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Export GeoJSON to file
    /// </summary>
    /// <param name="mapData">Map data to export</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="config">Export configuration</param>
    public async Task ExportToFileAsync(MapData mapData, string filePath, GeoJsonExportConfig? config = null)
    {
        var geoJson = ExportToGeoJson(mapData, config);
        await File.WriteAllTextAsync(filePath, geoJson);
    }

    /// <summary>
    /// Export cells as polygon features
    /// </summary>
    private List<GeoJsonFeature> ExportCells(MapData mapData, GeoJsonExportConfig config)
    {
        var features = new List<GeoJsonFeature>();

        foreach (var cell in mapData.Cells)
        {
            var polygon = _adapter.CellToPolygon(cell, mapData.Vertices);

            if (config.SimplifyGeometries)
            {
                polygon = SimplifyGeometry(polygon, config.SimplificationTolerance) as Polygon ?? polygon;
            }

            var properties = new Dictionary<string, object>
            {
                ["id"] = cell.Id,
                ["type"] = "cell"
            };

            if (config.IncludeElevation)
                properties["elevation"] = Math.Round((double)cell.Height, 3);

            if (config.IncludePrecipitation)
                properties["precipitation"] = Math.Round((double)cell.Precipitation, 3);

            if (config.IncludeBiomes)
                properties["biome"] = cell.Biome.ToString();

            var feature = new GeoJsonFeature
            {
                Type = "Feature",
                Geometry = ConvertToGeoJsonGeometry(polygon),
                Properties = properties
            };

            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Export state boundaries as polygon features
    /// </summary>
    private List<GeoJsonFeature> ExportStateBoundaries(MapData mapData, GeoJsonExportConfig config)
    {
        var features = new List<GeoJsonFeature>();
        var boundaryGenerator = new StateBoundaryGenerator();

        foreach (var state in mapData.States)
        {
            var boundary = boundaryGenerator.GetStateBoundary(state.Id, mapData);

            if (config.SimplifyGeometries)
            {
                boundary = SimplifyGeometry(boundary, config.SimplificationTolerance);
            }

            var properties = new Dictionary<string, object>
            {
                ["id"] = state.Id,
                ["name"] = state.Name ?? $"State {state.Id}",
                ["type"] = "state",
                ["color"] = $"#{state.Color:X6}"
            };

            properties["government"] = state.Government.ToString();

            if (state.Culture >= 0)
                properties["culture"] = state.Culture;

            var feature = new GeoJsonFeature
            {
                Type = "Feature",
                Geometry = ConvertToGeoJsonGeometry(boundary),
                Properties = properties
            };

            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Export rivers as linestring features
    /// </summary>
    private List<GeoJsonFeature> ExportRivers(MapData mapData, GeoJsonExportConfig config)
    {
        var features = new List<GeoJsonFeature>();

        foreach (var river in mapData.Rivers)
        {
            var lineString = _adapter.RiverToLineString(river, mapData);

            if (config.SimplifyGeometries)
            {
                lineString = SimplifyGeometry(lineString, config.SimplificationTolerance) as LineString ?? lineString;
            }

            var properties = new Dictionary<string, object>
            {
                ["id"] = river.Id,
                ["name"] = river.Name ?? $"River {river.Id}",
                ["type"] = "river",
                ["width"] = river.Width,
                ["source"] = river.Source
            };

            var feature = new GeoJsonFeature
            {
                Type = "Feature",
                Geometry = ConvertToGeoJsonGeometry(lineString),
                Properties = properties
            };

            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Export burgs as point features
    /// </summary>
    private List<GeoJsonFeature> ExportBurgs(MapData mapData, GeoJsonExportConfig config)
    {
        var features = new List<GeoJsonFeature>();

        foreach (var burg in mapData.Burgs)
        {
            var center = mapData.Cells[burg.Cell].Center;
            var point = _factory.CreatePoint(new Coordinate(center.X, center.Y));

            var properties = new Dictionary<string, object>
            {
                ["id"] = burg.Id,
                ["name"] = burg.Name ?? $"Burg {burg.Id}",
                ["type"] = "burg",
                ["cell"] = burg.Cell,
                ["capital"] = burg.IsCapital,
                ["port"] = burg.IsPort
            };

            if (burg.Population > 0)
                properties["population"] = burg.Population;

            var feature = new GeoJsonFeature
            {
                Type = "Feature",
                Geometry = ConvertToGeoJsonGeometry(point),
                Properties = properties
            };

            features.Add(feature);
        }

        return features;
    }

    /// <summary>
    /// Simplify geometry using Douglas-Peucker algorithm
    /// </summary>
    private NetTopologySuite.Geometries.Geometry SimplifyGeometry(NetTopologySuite.Geometries.Geometry geometry, double tolerance)
    {
        // Note: This would require NetTopologySuite.SimplifyTopology
        // For now, return the original geometry
        // In a full implementation, you would use:
        // return TopologyPreservingSimplifier.Simplify(geometry, tolerance);
        return geometry;
    }

    /// <summary>
    /// Convert NTS geometry to GeoJSON geometry representation
    /// </summary>
    private object ConvertToGeoJsonGeometry(NetTopologySuite.Geometries.Geometry geometry)
    {
        return _geoJsonWriter.Write(geometry);
    }

    /// <summary>
    /// Get export statistics
    /// </summary>
    public GeoJsonExportStatistics GetExportStatistics(MapData mapData, GeoJsonExportConfig? config = null)
    {
        config ??= new GeoJsonExportConfig();

        var stats = new GeoJsonExportStatistics
        {
            CellCount = config.IncludeCells ? mapData.Cells.Count : 0,
            StateCount = config.IncludeStateBoundaries ? mapData.States.Count : 0,
            RiverCount = config.IncludeRivers ? mapData.Rivers.Count : 0,
            BurgCount = config.IncludeBurgs ? mapData.Burgs.Count : 0
        };

        // Estimate file size (rough calculation)
        var estimatedFeatureSize = 200; // bytes per feature (rough estimate)
        stats.EstimatedFileSizeBytes = (stats.CellCount + stats.StateCount + stats.RiverCount + stats.BurgCount) * estimatedFeatureSize;

        return stats;
    }
}

/// <summary>
/// GeoJSON Feature representation
/// </summary>
public class GeoJsonFeature
{
    public string Type { get; set; } = "Feature";
    public object Geometry { get; set; } = null!;
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? Id { get; set; }
}

/// <summary>
/// GeoJSON FeatureCollection representation
/// </summary>
public class GeoJsonFeatureCollection
{
    public string Type { get; set; } = "FeatureCollection";
    public List<GeoJsonFeature> Features { get; set; } = new();
    public GeoJsonCrs? Crs { get; set; }
}

/// <summary>
/// GeoJSON CRS representation
/// </summary>
public class GeoJsonCrs
{
    public string Type { get; set; } = "name";
    public object Properties { get; set; } = null!;
}

/// <summary>
/// Statistics for GeoJSON export
/// </summary>
public class GeoJsonExportStatistics
{
    public int CellCount { get; set; }
    public int StateCount { get; set; }
    public int RiverCount { get; set; }
    public int BurgCount { get; set; }
    public long EstimatedFileSizeBytes { get; set; }
    public string EstimatedFileSize => FormatFileSize(EstimatedFileSizeBytes);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public override string ToString()
    {
        return $"Cells: {CellCount}, States: {StateCount}, Rivers: {RiverCount}, Burgs: {BurgCount}, " +
               $"Est. Size: {EstimatedFileSize}";
    }
}
