using Microsoft.Extensions.Logging;
using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Services;

/// <summary>
/// Builds user interest profiles from voting history using embeddings and source preferences.
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IContentVoteRepository _voteRepository;
    private readonly IManualContentFeedbackRepository _manualContentFeedbackRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IContentVoteRepository voteRepository,
        IManualContentFeedbackRepository manualContentFeedbackRepository,
        IEmbeddingService embeddingService,
        ILogger<UserProfileService> logger)
    {
        _voteRepository = voteRepository;
        _manualContentFeedbackRepository = manualContentFeedbackRepository;
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
        var manualFeedback = (await _manualContentFeedbackRepository.GetByUserAsync(userId, cancellationToken)).ToList();

        if (!votesList.Any() && !manualFeedback.Any())
        {
            _logger.LogInformation("No voting history or manual feedback for user {UserId}, returning empty profile", userId);
            return profile;
        }

        profile.TotalInteractions = votesList.Count + manualFeedback.Count;

        // Build user embedding from voted content and manual preference entries.
        await BuildUserEmbeddingAsync(profile, votesList, manualFeedback, cancellationToken);

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
    /// Build user preference embedding from both normal votes and manual preference entries.
    /// </summary>
    private async Task BuildUserEmbeddingAsync(
        UserInterestProfile profile,
        List<Core.Entities.ContentVote> votes,
        List<ManualContentFeedback> manualFeedback,
        CancellationToken cancellationToken)
    {
        try
        {
            var preferenceSignals = votes
                .Select(v => new PreferenceSignal
                {
                    Text = $"{v.Content.Title} {v.Content.Description}".Trim(),
                    Weight = v.VoteType == VoteType.Upvote ? 1.0f : -0.5f
                })
                .Concat(manualFeedback.Select(f => new PreferenceSignal
                {
                    Text = $"{f.Title} {f.Description}".Trim(),
                    Weight = f.VoteType == VoteType.Upvote ? 1.0f : -0.5f
                }))
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .ToList();

            if (!preferenceSignals.Any())
            {
                _logger.LogInformation("No usable preference text for user {UserId}, cannot build embedding", profile.UserId);
                return;
            }

            var texts = preferenceSignals.Select(s => s.Text).ToList();

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

            for (var embeddingIndex = 0; embeddingIndex < embeddingsList.Count; embeddingIndex++)
            {
                var embedding = embeddingsList[embeddingIndex];
                var weight = preferenceSignals[embeddingIndex].Weight;

                for (int i = 0; i < dimensions; i++)
                {
                    averageEmbedding[i] += embedding[i] * weight;
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
                "Built user embedding from {Count} preference signals for user {UserId}",
                preferenceSignals.Count,
                profile.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building user embedding for user {UserId}", profile.UserId);
            // Continue without embedding - other scorers can still work
        }
    }

    private sealed class PreferenceSignal
    {
        public string Text { get; set; } = string.Empty;
        public float Weight { get; set; }
    }

    /// <summary>
    /// Build source preference scores (legacy method, kept for hybrid scoring).
    /// </summary>
    private void BuildSourceScores(UserInterestProfile profile, List<Core.Entities.ContentVote> votes)
    {
        var sourceScores = new Dictionary<Guid, double>();

        foreach (var vote in votes)
        {
            var weight = vote.VoteType == VoteType.Upvote ? 1.0 : -0.5;

            // If content has a source, track score for that source
            if (vote.Content.SourceId.HasValue)
            {
                var sourceId = vote.Content.SourceId.Value;

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
