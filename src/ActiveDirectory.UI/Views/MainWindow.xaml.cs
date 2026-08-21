using Wpf.Ui.Controls;
using ActiveDirectory.UI.ViewModels;

namespace ActiveDirectory.UI.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}