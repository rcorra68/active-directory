namespace ActiveDirectory.Core.Models;

public class AdUserExtendedDetails
{
    // General Tab
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string TelephoneNumber { get; set; } = string.Empty;

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