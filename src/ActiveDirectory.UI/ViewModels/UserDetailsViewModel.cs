using ActiveDirectory.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActiveDirectory.UI.ViewModels;

public partial class UserDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private AdUserExtendedDetails _details;

    public UserDetailsViewModel(AdUserExtendedDetails details)
    {
        _details = details;
    }
}