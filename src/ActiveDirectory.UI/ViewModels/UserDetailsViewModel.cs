using ActiveDirectory.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActiveDirectory.UI.ViewModels;

public partial class UserDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private AdUserExtendedDetails _userDetails;

    public UserDetailsViewModel(AdUserExtendedDetails userDetails)
    {
        _userDetails = userDetails;
    }
}