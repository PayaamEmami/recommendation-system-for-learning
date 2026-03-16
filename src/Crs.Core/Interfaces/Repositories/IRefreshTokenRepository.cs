using Crs.Core.Entities;

namespace Crs.Core.Interfaces;

/// <summary>
/// Repository for JWT refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default);
    Task RemoveExpiredAsync(CancellationToken cancellationToken = default);
}
