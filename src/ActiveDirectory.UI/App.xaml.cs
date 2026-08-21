using System.Windows;
using Wpf.Ui.Appearance;

namespace ActiveDirectory.UI;

public partial class App : Application
{
    public App()
    {
        // Parse App.xaml merged dictionaries (ui:ControlsDictionary)
        InitializeComponent();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply Windows 11 Dark Theme globally
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }
}