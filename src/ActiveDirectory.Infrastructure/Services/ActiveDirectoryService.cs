using System.DirectoryServices;
using System.Runtime.Versioning;
using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Core.Models;

namespace ActiveDirectory.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly string _ldapPath;

    public ActiveDirectoryService(string ldapPath = "LDAP://DC=dipvvf,DC=it")
    {
        _ldapPath = ldapPath;
    }

    public Task<IEnumerable<AdUserDto>> SearchUsersAsync(string? firstName, string? lastName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var results = new List<AdUserDto>();

            using var entry = new DirectoryEntry(_ldapPath);
            using var searcher = new DirectorySearcher(entry);

            string filter = (string.IsNullOrEmpty(firstName), string.IsNullOrEmpty(lastName)) switch
            {
                (false, true) => $"(givenname={firstName})",
                (true, false) => $"(sn={lastName})",
                (false, false) => $"(&(givenname={firstName})(sn={lastName}))",
                _ => "(objectClass=user)"
            };

            searcher.Filter = filter;
            searcher.PropertiesToLoad.AddRange(["sn", "givenname", "samaccountname"]);

            using SearchResultCollection searchResults = searcher.FindAll();
            foreach (SearchResult item in searchResults)
            {
                results.Add(new AdUserDto
                {
                    FirstName = GetPropertyValue(item, "givenname").ToUpperInvariant(),
                    LastName = GetPropertyValue(item, "sn").ToUpperInvariant(),
                    SamAccountName = GetPropertyValue(item, "samaccountname").ToUpperInvariant()
                });
            }

            return (IEnumerable<AdUserDto>)results;
        }, cancellationToken);
    }

    public Task<AdUserDto?> GetUserDetailsAsync(string samAccountName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var entry = new DirectoryEntry(_ldapPath);
            using var searcher = new DirectorySearcher(entry);

            searcher.Filter = $"(samaccountname={samAccountName})";
            searcher.PropertiesToLoad.AddRange([
                "physicaldeliveryofficename", "sn", "givenname", "samaccountname",
                "userprincipalname", "description", "distinguishedname", "telephonenumber", "adspath"
            ]);

            SearchResult? result = searcher.FindOne();
            if (result == null) return null;

            return new AdUserDto
            {
                FirstName = GetPropertyValue(result, "givenname"),
                LastName = GetPropertyValue(result, "sn"),
                SamAccountName = GetPropertyValue(result, "samaccountname"),
                UserPrincipalName = GetPropertyValue(result, "userprincipalname"),
                PhysicalOfficeName = GetPropertyValue(result, "physicaldeliveryofficename"),
                DistinguishedName = GetPropertyValue(result, "distinguishedname"),
                Description = GetPropertyValue(result, "description"),
                TelephoneNumber = GetPropertyValue(result, "telephonenumber"),
                AdsPath = GetPropertyValue(result, "adspath")
            };
        }, cancellationToken);
    }

    private static string GetPropertyValue(SearchResult result, string propertyName)
    {
        return result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0
            ? result.Properties[propertyName][0]?.ToString() ?? string.Empty
            : string.Empty;
    }
}