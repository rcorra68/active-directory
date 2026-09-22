namespace ActiveDirectory.Core.Models;

public class AdUserExtendedDetails
{
    // General
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;

    // Organization
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;

    // Member Of
    public List<string> Groups { get; set; } = new();

    // Raw Attributes (Key-Value Dictionary)
    public Dictionary<string, string> RawAttributes { get; set; } = new();
}