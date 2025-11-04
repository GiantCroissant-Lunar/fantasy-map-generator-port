using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FantasyMapGenerator.UI.ViewModels;

public partial class ExportDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedFormatIndex = 0;
    
    [ObservableProperty]
    private double _scale = 1.0;
    
    [ObservableProperty]
    private int _quality = 90;
    
    [ObservableProperty]
    private string _filePath = "";
    
    private Window? _parentWindow;
    
    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }
    
    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (_parentWindow == null) return;
        
        var formats = new[] { "png", "jpg", "svg" };
        var format = formats[SelectedFormatIndex];
        
        var file = await _parentWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Map",
            DefaultExtension = format,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Files") { Patterns = new[] { "*.png" } },
                new FilePickerFileType("JPEG Files") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                new FilePickerFileType("SVG Files") { Patterns = new[] { "*.svg" } }
            }
        });
        
        if (file != null)
        {
            FilePath = file.Path.LocalPath;
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        FilePath = "";
        if (_parentWindow != null)
        {
            _parentWindow.Close();
        }
    }
    
    [RelayCommand]
    private void Export()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            // Show error or require file selection
            return;
        }
        
        if (_parentWindow != null)
        {
            _parentWindow.Close();
        }
    }
    
    public string GetFileExtension()
    {
        return SelectedFormatIndex switch
        {
            0 => ".png",
            1 => ".jpg",
            2 => ".svg",
            _ => ".png"
        };
    }
}