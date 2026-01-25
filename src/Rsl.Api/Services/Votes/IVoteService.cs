using Rsl.Api.DTOs.Votes.Requests;
using Rsl.Api.DTOs.Votes.Responses;

namespace Rsl.Api.Services;

/// <summary>
/// Service interface for voting operations.
/// </summary>
public interface IVoteService
{
    /// <summary>
    /// Casts or updates a vote on a resource.
    /// </summary>
    Task<VoteResponse> VoteOnResourceAsync(Guid userId, Guid resourceId, VoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a vote from a resource.
    /// </summary>
    Task RemoveVoteAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all votes by a user.
    /// </summary>
    Task<List<VoteResponse>> GetUserVotesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's vote on a specific resource.
    /// </summary>
    Task<VoteResponse?> GetUserVoteOnResourceAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);
}

