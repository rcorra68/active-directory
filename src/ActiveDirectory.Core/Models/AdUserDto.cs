namespace ActiveDirectory.Core.Models;

public record AdUserDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string SamAccountName { get; init; } = string.Empty;
    public string UserPrincipalName { get; init; } = string.Empty;
    public string PhysicalOfficeName { get; init; } = string.Empty;
    public string DistinguishedName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TelephoneNumber { get; init; } = string.Empty;
    public string AdsPath { get; init; } = string.Empty;

    public string DisplayName => $"{FirstName} {LastName} ({UserPrincipalName.Split('@')[0]})";
}