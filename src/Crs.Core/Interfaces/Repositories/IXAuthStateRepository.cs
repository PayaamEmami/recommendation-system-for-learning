using Crs.Core.Entities;

namespace Crs.Core.Interfaces;

/// <summary>
/// Repository for ephemeral X OAuth auth states.
/// </summary>
public interface IXAuthStateRepository
{
    Task AddAsync(XAuthState state, CancellationToken cancellationToken = default);
    Task<XAuthState?> GetAndRemoveAsync(string state, CancellationToken cancellationToken = default);
    Task RemoveExpiredAsync(CancellationToken cancellationToken = default);
}
