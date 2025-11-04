using System.Threading.Tasks;
using Avalonia.Controls;
using FantasyMapGenerator.UI.ViewModels;

namespace FantasyMapGenerator.UI.Controls;

public partial class ExportDialog : Window
{
    public ExportDialog()
    {
        InitializeComponent();
        DataContext = new ExportDialogViewModel();
    }
    
    public ExportDialogViewModel ViewModel => (ExportDialogViewModel)DataContext!;
    
    public static async Task<string?> ShowExportDialog(Window parent)
    {
        var dialog = new ExportDialog();
        await dialog.ShowDialog(parent);
        return dialog.ViewModel.FilePath;
    }
}