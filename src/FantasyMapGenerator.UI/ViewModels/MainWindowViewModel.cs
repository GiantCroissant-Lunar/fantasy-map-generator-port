using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FantasyMapGenerator.UI.Controls;

namespace FantasyMapGenerator.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Welcome to Fantasy Map Generator!";
    
    [ObservableProperty]
    private MapControl _mapControl = new();
    
    private Window? _parentWindow;
    
    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }
    
    [RelayCommand]
    private void GenerateNewMap()
    {
        _ = MapControl.ViewModel.GenerateMapAsync();
    }
    
    [RelayCommand]
    private async Task ExportMapAsync()
    {
        if (_parentWindow == null) return;
        
        var exportDialog = new ExportDialog();
        exportDialog.ViewModel.SetParentWindow(_parentWindow);
        
        await exportDialog.ShowDialog(_parentWindow);
        
        if (!string.IsNullOrEmpty(exportDialog.ViewModel.FilePath))
        {
            await MapControl.ViewModel.ExportMapAsync(exportDialog.ViewModel.FilePath);
        }
    }
}
