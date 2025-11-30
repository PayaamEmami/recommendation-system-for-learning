using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;

namespace Rsl.Api.Services;

/// <summary>
/// Service for handling voting operations.
/// </summary>
public class VoteService : IVoteService
{
    private readonly IResourceVoteRepository _voteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<VoteService> _logger;

    public VoteService(
        IResourceVoteRepository voteRepository,
        IUserRepository userRepository,
        IResourceRepository resourceRepository,
        ILogger<VoteService> logger)
    {
        _voteRepository = voteRepository;
        _userRepository = userRepository;
        _resourceRepository = resourceRepository;
        _logger = logger;
    }

    public async Task<VoteResponse> VoteOnResourceAsync(
        Guid userId,
        Guid resourceId,
        VoteRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Verify resource exists
        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);
        if (resource == null)
        {
            throw new KeyNotFoundException($"Resource with ID {resourceId} not found");
        }

        // Check if user already voted on this resource
        var existingVote = await _voteRepository.GetByUserAndResourceAsync(userId, resourceId, cancellationToken);

        ResourceVote vote;

        if (existingVote != null)
        {
            // Update existing vote
            existingVote.VoteType = request.VoteType;
            existingVote.UpdatedAt = DateTime.UtcNow;
            vote = await _voteRepository.UpdateAsync(existingVote, cancellationToken);

            _logger.LogInformation("User {UserId} updated vote on resource {ResourceId} to {VoteType}",
                userId, resourceId, request.VoteType);
        }
        else
        {
            // Create new vote
            vote = new ResourceVote
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ResourceId = resourceId,
                VoteType = request.VoteType,
                CreatedAt = DateTime.UtcNow
            };

            vote = await _voteRepository.CreateAsync(vote, cancellationToken);

            _logger.LogInformation("User {UserId} voted {VoteType} on resource {ResourceId}",
                userId, request.VoteType, resourceId);
        }

        return MapToVoteResponse(vote);
    }

    public async Task RemoveVoteAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByUserAndResourceAsync(userId, resourceId, cancellationToken);

        if (vote == null)
        {
            throw new KeyNotFoundException($"No vote found for user {userId} on resource {resourceId}");
        }

        await _voteRepository.DeleteAsync(vote.Id, cancellationToken);

        _logger.LogInformation("User {UserId} removed vote from resource {ResourceId}", userId, resourceId);
    }

    public async Task<List<VoteResponse>> GetUserVotesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var votes = await _voteRepository.GetByUserAsync(userId, cancellationToken);

        return votes.Select(MapToVoteResponse).ToList();
    }

    public async Task<VoteResponse?> GetUserVoteOnResourceAsync(
        Guid userId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByUserAndResourceAsync(userId, resourceId, cancellationToken);

        return vote != null ? MapToVoteResponse(vote) : null;
    }

    private static VoteResponse MapToVoteResponse(ResourceVote vote)
    {
        return new VoteResponse
        {
            Id = vote.Id,
            UserId = vote.UserId,
            ResourceId = vote.ResourceId,
            VoteType = vote.VoteType,
            CreatedAt = vote.CreatedAt,
            UpdatedAt = vote.UpdatedAt
        };
    }
}

