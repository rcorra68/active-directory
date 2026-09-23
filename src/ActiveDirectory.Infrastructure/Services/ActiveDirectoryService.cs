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
            searcher.PropertiesToLoad.AddRange(["sn", "givenname", "samaccountname", "userprincipalname", "description"]);

            using SearchResultCollection searchResults = searcher.FindAll();
            foreach (SearchResult item in searchResults)
            {
                results.Add(new AdUserDto
                {
                    FirstName = GetPropertyValue(item, "givenname").ToUpperInvariant(),
                    LastName = GetPropertyValue(item, "sn").ToUpperInvariant(),
                    UserPrincipalName = GetPropertyValue(item, "userprincipalname").ToLowerInvariant(),
                    SamAccountName = GetPropertyValue(item, "samaccountname").ToUpperInvariant(),
                    Description = GetPropertyValue(item, "description").ToUpperInvariant()
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

    public async Task<AdUserExtendedDetails?> GetExtendedUserDetailsAsync(string distinguishedName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(distinguishedName))
                return null;

            using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=user)"
            };

            // Retrieve all populated LDAP attributes
            SearchResult? result = searcher.FindOne();
            if (result == null) return null;

            var details = new AdUserExtendedDetails();
            var rawMap = new SortedDictionary<string, string>();

            foreach (string propName in result.Properties.PropertyNames)
            {
                var valCollection = result.Properties[propName];
                var values = valCollection.Cast<object>().Select(v => v?.ToString() ?? string.Empty).ToList();
                string combinedValue = string.Join(", ", values);

                rawMap[propName] = combinedValue;

                // Map specific known fields
                switch (propName.ToLowerInvariant())
                {
                    case "displayname": details.DisplayName = combinedValue; break;
                    case "userprincipalname": details.UserPrincipalName = combinedValue; break;
                    case "samaccountname": details.SamAccountName = combinedValue; break;
                    case "mail": details.Mail = combinedValue; break;
                    case "telephonenumber": details.TelephoneNumber = combinedValue; break;
                    case "title": details.Title = combinedValue; break;
                    case "department": details.Department = combinedValue; break;
                    case "company": details.Company = combinedValue; break;
                    case "physicalofficename": details.PhysicalOfficeName = combinedValue; break;
                    case "manager": details.Manager = combinedValue; break;
                    case "memberof":
                        details.Groups = values;
                        break;
                }
            }

            details.RawAttributes = new Dictionary<string, string>(rawMap);
            return details;
        }, cancellationToken);
    }

    private static string GetPropertyValue(SearchResult result, string propertyName)
    {
        return result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0
            ? result.Properties[propertyName][0]?.ToString() ?? string.Empty
            : string.Empty;
    }
}