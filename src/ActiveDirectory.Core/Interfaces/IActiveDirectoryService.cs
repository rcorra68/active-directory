using ActiveDirectory.Core.Models;

namespace ActiveDirectory.Core.Interfaces;

public interface IActiveDirectoryService
{
    Task<IEnumerable<AdUserDto>> SearchUsersAsync(string? firstName, string? lastName, CancellationToken cancellationToken = default);
    Task<AdUserDto?> GetUserDetailsAsync(string samAccountName, CancellationToken cancellationToken = default);
}