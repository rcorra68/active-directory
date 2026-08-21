using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    public ObservableCollection<AdUserDto> SearchResults { get; } = new();

    public MainViewModel(IActiveDirectoryService adService, IFiscalCodeDecoder fiscalCodeDecoder)
    {
        _adService = adService;
        _fiscalCodeDecoder = fiscalCodeDecoder;
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

    [RelayCommand]
    private void CopySamAccountName()
    {
        if (SelectedUser != null && !string.IsNullOrEmpty(SelectedUser.SamAccountName))
        {
            Clipboard.SetText(SelectedUser.SamAccountName);
            StatusMessage = "sAMAccountName copied to clipboard.";
        }
    }
}