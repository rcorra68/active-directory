using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Core.Models;
using ActiveDirectory.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ActiveDirectory.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly IFiscalCodeDecoder _fiscalCodeDecoder;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _physicalOfficeName = string.Empty;

    [ObservableProperty]
    private AdUserDto? _selectedUser;

    [ObservableProperty]
    private FiscalCodeInfo? _fiscalCodeDetails;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string AppVersion { get; }

    public ObservableCollection<AdUserDto> SearchResults { get; } = new();

    public MainViewModel(IActiveDirectoryService adService, IFiscalCodeDecoder fiscalCodeDecoder)
    {
        _adService = adService;
        _fiscalCodeDecoder = fiscalCodeDecoder;

        AppVersion = GetApplicationVersion();
    }

    [RelayCommand]
    private async Task SearchUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
        {
            StatusMessage = "Please enter first name or last name.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Searching Active Directory...";
        SearchResults.Clear();
        SelectedUser = null;

        try
        {
            var users = await _adService.SearchUsersAsync(FirstName, LastName);
            foreach (var user in users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName))
            {
                SearchResults.Add(user);
            }

            StatusMessage = SearchResults.Count > 0 ? $"Found {SearchResults.Count} user(s)." : "No users found.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedUserChanged(AdUserDto? value)
    {
        FiscalCodeDetails = null;

        if (value == null || string.IsNullOrWhiteSpace(value.SamAccountName))
        {
            return;
        }

        // Se il record ha già i campi completi (perché rieseguito qui sotto dopo il fetch),
        // evitiamo una seconda chiamata ad AD e un loop ricorsivo.
        if (!string.IsNullOrWhiteSpace(value.DistinguishedName))
        {
            FiscalCodeDetails = _fiscalCodeDecoder.Decode(value.SamAccountName);
            return;
        }

        _ = LoadUserDetailsAsync(value.SamAccountName);
    }

    private async Task LoadUserDetailsAsync(string samAccountName)
    {
        IsBusy = true;
        StatusMessage = "Caricamento dettagli utente...";

        try
        {
            var details = await _adService.GetUserDetailsAsync(samAccountName);
            if (details != null)
            {
                // Riassegna SelectedUser con il record completo: la view è bindata su SelectedUser,
                // quindi questo aggiorna automaticamente Ufficio, Telefono, UPN, DN, AdsPath ecc.
                SelectedUser = details;
                StatusMessage = string.Empty;
            }
            else
            {
                StatusMessage = $"Utente '{samAccountName}' non trovato durante il caricamento dei dettagli.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Caricamento dettagli fallito: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Retrieve InformationalVersion attribute (Semantic Version)
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            return FormatSemVer(infoVersion);
        }

        // 2. Fallback to FileVersionInfo
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        if (!string.IsNullOrWhiteSpace(fileVersionInfo.ProductVersion))
        {
            return FormatSemVer(fileVersionInfo.ProductVersion);
        }

        // 3. Fallback to System.Version (truncating to Major.Minor.Build)
        var version = assembly.GetName().Version;
        return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v0.2.1";
    }

    private static string FormatSemVer(string rawVersion)
    {
        // Strip build metadata after '+' (e.g., '0.2.1.4+abc' -> '0.2.1.4')
        var cleanVersion = rawVersion.Split('+')[0].TrimStart('v', 'V');

        // Normalize 4-digit versions (0.2.1.4) to 3-digit Semantic Versions (0.2.1)
        if (Version.TryParse(cleanVersion, out var parsedVersion))
        {
            return $"v{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}";
        }

        return $"v{cleanVersion}";
    }

    [RelayCommand]
    private void CopySamAccountName()
    {
        if (SelectedUser != null && !string.IsNullOrEmpty(SelectedUser.SamAccountName))
        {
            Clipboard.SetText(SelectedUser.SamAccountName);
            StatusMessage = "sAMAccountName copied to clipboard.";
        }
    }

    [RelayCommand]
    private void OpenFullUserDetails()
    {
        if (SelectedUser == null) return;

        // TODO: Map or fetch AdUserExtendedDetails from service
        var extendedDetails = _adService.GetExtendedDetails(SelectedUser.DistinguishedName);

        var vm = new UserDetailsViewModel(extendedDetails);
        var window = new UserDetailsWindow
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }
}