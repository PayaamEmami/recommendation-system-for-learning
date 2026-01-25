using Rsl.Core.Entities;
using Rsl.Core.Enums;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for ResourceVote entity operations.
/// </summary>
public interface IResourceVoteRepository
{
    Task<ResourceVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResourceVote?> GetByUserAndResourceAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResourceVote>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResourceVote>> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);
    Task<ResourceVote> CreateAsync(ResourceVote vote, CancellationToken cancellationToken = default);
    Task<ResourceVote> UpdateAsync(ResourceVote vote, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetUpvoteCountAsync(Guid resourceId, CancellationToken cancellationToken = default);
    Task<int> GetDownvoteCountAsync(Guid resourceId, CancellationToken cancellationToken = default);
}

