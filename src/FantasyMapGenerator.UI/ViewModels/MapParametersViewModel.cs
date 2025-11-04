using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FantasyMapGenerator.Core;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Rendering;

namespace FantasyMapGenerator.UI.ViewModels;

public partial class MapParametersViewModel : ObservableObject
{
    [ObservableProperty]
    private int _width = 800;
    
    [ObservableProperty]
    private int _height = 600;
    
    [ObservableProperty]
    private long _seed = DateTime.Now.Ticks;
    
    [ObservableProperty]
    private string _seedString = "";
    
    [ObservableProperty]
    private int _numPoints = 1000;
    
    [ObservableProperty]
    private float _seaLevel = 0.3f;
    
    [ObservableProperty]
    private bool _showTerrain = true;
    
    [ObservableProperty]
    private bool _showCoastlines = true;
    
    [ObservableProperty]
    private bool _showBorders = true;
    
    [ObservableProperty]
    private bool _showCities = true;
    
    [ObservableProperty]
    private bool _showLabels = true;
    
    public MapGenerationSettings GetGenerationSettings()
    {
        return new MapGenerationSettings
        {
            Width = Width,
            Height = Height,
            Seed = Seed,
            NumPoints = NumPoints,
            SeaLevel = SeaLevel
        };
    }
    
    public MapRenderSettings GetRenderSettings()
    {
        return new MapRenderSettings
        {
            ShowTerrain = ShowTerrain,
            ShowCoastlines = ShowCoastlines,
            ShowBorders = ShowBorders,
            ShowCities = ShowCities,
            ShowLabels = ShowLabels
        };
    }
    
    [RelayCommand]
    private void GenerateMap()
    {
        // This will be handled by the parent view model
        MapGenerated?.Invoke(this, EventArgs.Empty);
    }
    
    public event EventHandler? MapGenerated;
    
    partial void OnSeedStringChanged(string value)
    {
        if (long.TryParse(value, out var seed))
        {
            Seed = seed;
        }
    }
    
    partial void OnSeedChanged(long value)
    {
        SeedString = value.ToString();
    }
}