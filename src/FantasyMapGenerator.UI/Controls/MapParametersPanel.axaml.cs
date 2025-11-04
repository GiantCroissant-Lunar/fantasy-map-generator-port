using Avalonia.Controls;
using FantasyMapGenerator.UI.ViewModels;

namespace FantasyMapGenerator.UI.Controls;

public partial class MapParametersPanel : UserControl
{
    public MapParametersPanel()
    {
        InitializeComponent();
        DataContext = new MapParametersViewModel();
    }
    
    public MapParametersViewModel ViewModel => (MapParametersViewModel)DataContext!;
}