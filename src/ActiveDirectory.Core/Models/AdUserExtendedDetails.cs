namespace ActiveDirectory.Core.Models;

public class AdUserExtendedDetails
{
    // General Tab
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string TelephoneNumber { get; set; } = string.Empty;

    // Account Status (nuovo)
    public bool IsEnabled { get; set; } = true;
    public bool IsLockedOut { get; set; }
    public bool PasswordNeverExpires { get; set; }
    public DateTime? PasswordLastSet { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public DateTime? LastLogon { get; set; }

    // Stringhe già formattate per il binding XAML (niente converter necessari)
    public string AccountStatusDisplay =>
        IsLockedOut ? "Bloccato" : (IsEnabled ? "Attivo" : "Disabilitato");

    public string PasswordExpiryDisplay =>
        PasswordNeverExpires ? "Non scade mai"
        : PasswordExpiryDate.HasValue ? PasswordExpiryDate.Value.ToString("dd/MM/yyyy")
        : "Non disponibile";

    public string LastLogonDisplay =>
        LastLogon.HasValue ? LastLogon.Value.ToString("dd/MM/yyyy HH:mm") : "Mai / non disponibile";

    // Organization Tab
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string PhysicalOfficeName { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;

    // Groups Tab
    public List<string> Groups { get; set; } = new();

    // Raw LDAP Attributes Tab
    public Dictionary<string, string> RawAttributes { get; set; } = new();
}