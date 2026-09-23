using System.Windows;

namespace ActiveDirectory.UI.Views;

public partial class UserDetailsWindow
{
    public UserDetailsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}