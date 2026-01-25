using Rsl.Core.Entities;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for managing XFollowedAccount entities.
/// </summary>
public interface IXFollowedAccountRepository
{
    Task<List<XFollowedAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ReplaceForUserAsync(Guid userId, List<XFollowedAccount> accounts, CancellationToken cancellationToken = default);
}

