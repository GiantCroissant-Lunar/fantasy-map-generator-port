using Avalonia.Controls;
using FantasyMapGenerator.UI.ViewModels;
using FantasyMapGenerator.UI.Controls;

namespace FantasyMapGenerator.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        viewModel.SetParentWindow(this);
        DataContext = viewModel;
        
        // Wire up events
        if (ParametersPanel?.ViewModel != null)
        {
            ParametersPanel.ViewModel.MapGenerated += OnMapGenerated;
        }
    }
    
    private async void OnMapGenerated(object? sender, System.EventArgs e)
    {
        if (ParametersPanel?.ViewModel != null && MapDisplay?.ViewModel != null)
        {
            var genSettings = ParametersPanel.ViewModel.GetGenerationSettings();
            var renderSettings = ParametersPanel.ViewModel.GetRenderSettings();
            
            await MapDisplay.ViewModel.GenerateMapWithSettingsAsync(genSettings);
            MapDisplay.ViewModel.RenderSettings = renderSettings;
        }
    }
}