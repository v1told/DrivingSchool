using System.Windows;
using System.Windows.Controls;
using DrivingSchool.App.ViewModels;

namespace DrivingSchool.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    /// <summary>При переході на вкладку дані оновлюються, щоб відобразити зміни з інших вкладок.</summary>
    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource == Tabs)
        {
            _vm.RefreshTab(Tabs.SelectedIndex);
        }
    }
}
