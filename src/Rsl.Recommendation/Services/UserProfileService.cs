using Microsoft.Extensions.Logging;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Services;

/// <summary>
/// Builds user interest profiles from voting history using embeddings and source preferences.
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IResourceVoteRepository _voteRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IResourceVoteRepository voteRepository,
        IResourceRepository resourceRepository,
        IEmbeddingService embeddingService,
        ILogger<UserProfileService> logger)
    {
        _voteRepository = voteRepository;
        _resourceRepository = resourceRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<UserInterestProfile> BuildProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = new UserInterestProfile
        {
            UserId = userId,
            LastUpdated = DateTime.UtcNow
        };

        // Get all user votes
        var votes = await _voteRepository.GetByUserAsync(userId, cancellationToken);
        var votesList = votes.ToList();

        if (!votesList.Any())
        {
            _logger.LogInformation("No voting history for user {UserId}, returning empty profile", userId);
            return profile;
        }

        profile.TotalInteractions = votesList.Count;

        // Build user embedding from upvoted resources
        await BuildUserEmbeddingAsync(profile, votesList, cancellationToken);

        // Calculate source scores based on votes (legacy, kept for hybrid scoring)
        BuildSourceScores(profile, votesList);

        _logger.LogInformation(
            "Built profile for user {UserId} with {Interactions} interactions, embedding dimensions: {Dimensions}",
            userId,
            profile.TotalInteractions,
            profile.UserEmbedding?.Length ?? 0);

        return profile;
    }

    /// <summary>
    /// Build user preference embedding by aggregating embeddings of upvoted resources.
    /// </summary>
    private async Task BuildUserEmbeddingAsync(
        UserInterestProfile profile,
        List<Core.Entities.ResourceVote> votes,
        CancellationToken cancellationToken)
    {
        try
        {
            // Filter to upvoted resources only
            var upvotedResources = votes
                .Where(v => v.VoteType == VoteType.Upvote)
                .Select(v => v.Resource)
                .ToList();

            if (!upvotedResources.Any())
            {
                _logger.LogInformation("No upvoted resources for user {UserId}, cannot build embedding", profile.UserId);
                return;
            }

            // Generate embeddings for all upvoted resources
            var texts = upvotedResources
                .Select(r => $"{r.Title} {r.Description}".Trim())
                .ToList();

            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

            if (!embeddings.Any())
            {
                _logger.LogWarning("Failed to generate embeddings for user {UserId}", profile.UserId);
                return;
            }

            // Average the embeddings to create user preference vector
            var embeddingsList = embeddings.ToList();
            var dimensions = embeddingsList[0].Length;
            var averageEmbedding = new float[dimensions];

            foreach (var embedding in embeddingsList)
            {
                for (int i = 0; i < dimensions; i++)
                {
                    averageEmbedding[i] += embedding[i];
                }
            }

            for (int i = 0; i < dimensions; i++)
            {
                averageEmbedding[i] /= embeddingsList.Count;
            }

            // Normalize the vector (L2 normalization)
            var magnitude = Math.Sqrt(averageEmbedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < dimensions; i++)
                {
                    averageEmbedding[i] /= (float)magnitude;
                }
            }

            profile.UserEmbedding = averageEmbedding;

            _logger.LogDebug(
                "Built user embedding from {Count} upvoted resources for user {UserId}",
                upvotedResources.Count,
                profile.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building user embedding for user {UserId}", profile.UserId);
            // Continue without embedding - other scorers can still work
        }
    }

    /// <summary>
    /// Build source preference scores (legacy method, kept for hybrid scoring).
    /// </summary>
    private void BuildSourceScores(UserInterestProfile profile, List<Core.Entities.ResourceVote> votes)
    {
        var sourceScores = new Dictionary<Guid, double>();

        foreach (var vote in votes)
        {
            var weight = vote.VoteType == VoteType.Upvote ? 1.0 : -0.5;

            // If resource has a source, track score for that source
            if (vote.Resource.SourceId.HasValue)
            {
                var sourceId = vote.Resource.SourceId.Value;

                if (!sourceScores.ContainsKey(sourceId))
                {
                    sourceScores[sourceId] = 0;
                }

                sourceScores[sourceId] += weight;
            }
        }

        // Normalize scores to 0.0 - 1.0 range
        if (sourceScores.Any())
        {
            var maxScore = sourceScores.Values.Max();
            var minScore = sourceScores.Values.Min();
            var range = maxScore - minScore;

            foreach (var sourceId in sourceScores.Keys)
            {
                if (range > 0)
                {
                    var normalizedScore = (sourceScores[sourceId] - minScore) / range;
                    profile.SetTopicScore(sourceId, normalizedScore);
                }
                else
                {
                    profile.SetTopicScore(sourceId, 0.5);
                }
            }
        }
    }
}


