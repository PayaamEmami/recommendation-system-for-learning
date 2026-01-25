using Rsl.Core.Entities;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for managing XPost entities.
/// </summary>
public interface IXPostRepository
{
    Task<List<XPost>> GetLatestByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task UpsertRangeAsync(IEnumerable<XPost> posts, CancellationToken cancellationToken = default);
    Task PruneOldPostsAsync(Guid userId, int keepPerAccount, CancellationToken cancellationToken = default);
}

