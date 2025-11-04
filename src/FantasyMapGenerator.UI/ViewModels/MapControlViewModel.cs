using System;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FantasyMapGenerator.Core;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Rendering;

namespace FantasyMapGenerator.UI.ViewModels;

public partial class MapControlViewModel : ObservableObject
{
    [ObservableProperty]
    private Bitmap? _mapImage;
    
    [ObservableProperty]
    private int _mapWidth = 800;
    
    [ObservableProperty]
    private int _mapHeight = 600;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _loadingMessage = "Generating map...";
    
    [ObservableProperty]
    private MapData? _currentMap;
    
    [ObservableProperty]
    private MapRenderSettings _renderSettings = new();
    
    [RelayCommand]
    public async Task GenerateMapAsync()
    {
        await GenerateMapWithSettingsAsync();
    }
    
    public async Task GenerateMapWithSettingsAsync(MapGenerationSettings? settings = null)
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Generating heightmap...";
            
            settings ??= new MapGenerationSettings
            {
                Width = MapWidth,
                Height = MapHeight,
                Seed = DateTime.Now.Ticks,
                NumPoints = 1000,
                SeaLevel = 0.3f
            };
            
            // Generate map data
            var generator = new MapGenerator();
            CurrentMap = await Task.Run(() => generator.Generate(settings));
            
            LoadingMessage = "Rendering map...";
            
            // Render map to memory stream
            await Task.Run(() =>
            {
                var renderer = new MapRenderer();
                using var stream = new MemoryStream();
                renderer.RenderToStream(CurrentMap, RenderSettings, stream);
                
                // Convert to Avalonia Bitmap
                stream.Position = 0;
                MapImage = new Bitmap(stream);
            });
        }
        catch (Exception ex)
        {
            LoadingMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    public async Task ExportMapAsync(string filePath)
    {
        if (CurrentMap == null) return;
        
        try
        {
            IsLoading = true;
            LoadingMessage = "Exporting map...";
            
            await Task.Run(() =>
            {
                var exporter = new MapExporter();
                exporter.ExportMap(CurrentMap, RenderSettings, filePath);
            });
        }
        catch (Exception ex)
        {
            LoadingMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    partial void OnRenderSettingsChanged(MapRenderSettings value)
    {
        // Re-render map when settings change
        if (CurrentMap != null && !IsLoading)
        {
            _ = Task.Run(async () =>
            {
                await Task.Run(() =>
                {
                    var renderer = new MapRenderer();
                    using var stream = new MemoryStream();
                    renderer.RenderToStream(CurrentMap, value, stream);
                    
                    stream.Position = 0;
                    MapImage = new Bitmap(stream);
                });
            });
        }
    }
}