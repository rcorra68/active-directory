using ActiveDirectory.Core.Interfaces;
using ActiveDirectory.Core.Models;
using System.DirectoryServices;
using System.Reflection;
using System.Runtime.Versioning;

namespace ActiveDirectory.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly string _ldapPath;
    private long? _maxPwdAgeTicks; // cache: la policy password è a livello di dominio, non cambia ad ogni richiesta

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

            SearchResult? result = searcher.FindOne();
            if (result == null) return null;

            var details = new AdUserExtendedDetails();
            var rawMap = new SortedDictionary<string, string>();

            foreach (string propName in result.Properties.PropertyNames)
            {
                var valCollection = result.Properties[propName];
                var rawFirstValue = valCollection.Count > 0 ? valCollection[0] : null;

                var values = valCollection.Cast<object>().Select(v => v?.ToString() ?? string.Empty).ToList();
                string combinedValue = string.Join(", ", values);
                rawMap[propName] = combinedValue;

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

                    // userAccountControl è un intero "normale" (Integer syntax), niente LargeInteger
                    case "useraccountcontrol":
                        if (long.TryParse(combinedValue, out var uac))
                        {
                            details.IsEnabled = (uac & 0x2) == 0;           // ACCOUNTDISABLE
                            details.PasswordNeverExpires = (uac & 0x10000) != 0; // DONT_EXPIRE_PASSWORD
                        }
                        break;

                    // Questi tre sono Integer8 (LargeInteger) -> serve la conversione dedicata
                    case "pwdlastset":
                        details.PasswordLastSet = FileTimeToDateTime(ParseLargeInteger(rawFirstValue));
                        break;
                    case "lockouttime":
                        details.IsLockedOut = ParseLargeInteger(rawFirstValue) > 0;
                        break;
                    case "lastlogontimestamp":
                        details.LastLogon = FileTimeToDateTime(ParseLargeInteger(rawFirstValue));
                        break;
                }
            }

            // Scadenza password = pwdLastSet + maxPwdAge (policy di dominio)
            if (!details.PasswordNeverExpires && details.PasswordLastSet.HasValue)
            {
                var maxPwdAgeTicks = GetMaxPwdAgeTicks();
                if (maxPwdAgeTicks != 0) // 0 = "le password non scadono mai" a livello di dominio
                {
                    details.PasswordExpiryDate = details.PasswordLastSet.Value.AddTicks(Math.Abs(maxPwdAgeTicks));
                }
            }

            details.RawAttributes = new Dictionary<string, string>(rawMap);
            return details;
        }, cancellationToken);
    }

    /// <summary>
    /// Legge maxPwdAge dall'oggetto dominio (root LDAP). Risultato in cache: è una policy
    /// di dominio, non cambia tra una richiesta e l'altra nella vita del servizio.
    /// </summary>
    private long GetMaxPwdAgeTicks()
    {
        if (_maxPwdAgeTicks.HasValue) return _maxPwdAgeTicks.Value;

        using var domainRoot = new DirectoryEntry(_ldapPath);
        var raw = domainRoot.Properties["maxPwdAge"].Count > 0
            ? domainRoot.Properties["maxPwdAge"][0]
            : null;

        _maxPwdAgeTicks = ParseLargeInteger(raw);
        return _maxPwdAgeTicks.Value;
    }

    /// <summary>
    /// Gli attributi AD di tipo "Interval" (pwdLastSet, lastLogon, lastLogonTimestamp,
    /// accountExpires, lockoutTime, maxPwdAge...) vengono restituiti da System.DirectoryServices
    /// come oggetti COM IADsLargeInteger, non come long: HighPart/LowPart vanno letti via reflection.
    /// </summary>
    private static long ParseLargeInteger(object? rawValue)
    {
        if (rawValue is null) return 0;
        if (rawValue is long l) return l;
        if (rawValue is int i) return i; // capita per il valore 0

        try
        {
            var type = rawValue.GetType();
            int highPart = (int)type.InvokeMember("HighPart", BindingFlags.GetProperty, null, rawValue, null)!;
            int lowPart = (int)type.InvokeMember("LowPart", BindingFlags.GetProperty, null, rawValue, null)!;
            return (((long)highPart) << 32) + (uint)lowPart;
        }
        catch
        {
            return 0;
        }
    }

    private static DateTime? FileTimeToDateTime(long fileTime)
    {
        // 0 = "mai impostato/mai avvenuto", valori negativi non hanno senso per queste date
        if (fileTime <= 0) return null;

        try
        {
            return DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static string GetPropertyValue(SearchResult result, string propertyName)
    {
        return result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0
            ? result.Properties[propertyName][0]?.ToString() ?? string.Empty
            : string.Empty;
    }
}