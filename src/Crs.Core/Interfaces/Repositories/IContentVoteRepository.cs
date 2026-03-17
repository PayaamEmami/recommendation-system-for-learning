using Crs.Core.Entities;
using Crs.Core.Enums;

namespace Crs.Core.Interfaces;

/// <summary>
/// Repository interface for ContentVote entity operations.
/// </summary>
public interface IContentVoteRepository
{
    Task<ContentVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContentVote?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentVote>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentVote>> GetByContentAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<ContentVote> CreateAsync(ContentVote vote, CancellationToken cancellationToken = default);
    Task<ContentVote> UpdateAsync(ContentVote vote, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetUpvoteCountAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<int> GetDownvoteCountAsync(Guid contentId, CancellationToken cancellationToken = default);
}

