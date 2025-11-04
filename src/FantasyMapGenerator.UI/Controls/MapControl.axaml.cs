using Avalonia.Controls;
using FantasyMapGenerator.UI.ViewModels;

namespace FantasyMapGenerator.UI.Controls;

public partial class MapControl : UserControl
{
    public MapControl()
    {
        InitializeComponent();
        DataContext = new MapControlViewModel();
    }
    
    public MapControlViewModel ViewModel => (MapControlViewModel)DataContext!;
}