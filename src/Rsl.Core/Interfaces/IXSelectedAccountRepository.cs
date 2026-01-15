using Rsl.Core.Entities;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for managing XSelectedAccount entities.
/// </summary>
public interface IXSelectedAccountRepository
{
    Task<List<XSelectedAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ReplaceForUserAsync(Guid userId, List<XSelectedAccount> selectedAccounts, CancellationToken cancellationToken = default);
}
