using Microsoft.Extensions.Logging;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Engine;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Services;

/// <summary>
/// Generates and persists daily recommendation feeds.
/// </summary>
public class FeedGenerator : IFeedGenerator
{
    private readonly IRecommendationEngine _engine;
    private readonly IUserProfileService _profileService;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IResourceVoteRepository _voteRepository;
    private readonly ILogger<FeedGenerator> _logger;

    public FeedGenerator(
        IRecommendationEngine engine,
        IUserProfileService profileService,
        IRecommendationRepository recommendationRepository,
        IResourceVoteRepository voteRepository,
        ILogger<FeedGenerator> logger)
    {
        _engine = engine;
        _profileService = profileService;
        _recommendationRepository = recommendationRepository;
        _voteRepository = voteRepository;
        _logger = logger;
    }

    public async Task<List<Core.Entities.Recommendation>> GenerateFeedAsync(
        Guid userId,
        ResourceType feedType,
        DateOnly date,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating feed for user {UserId}, type {FeedType}, date {Date}, count {Count}",
            userId, feedType, date, count);

        // Check if recommendations already exist for this date/feed
        var existing = await _recommendationRepository.GetByUserDateAndTypeAsync(
            userId, date, feedType, cancellationToken);

        if (existing.Any())
        {
            _logger.LogInformation(
                "Recommendations already exist for user {UserId}, feed {FeedType}, date {Date}",
                userId, feedType, date);
            return existing.ToList();
        }

        // Build user profile
        var userProfile = await _profileService.BuildProfileAsync(userId, cancellationToken);

        // Get resources user has already seen (voted on)
        var userVotes = await _voteRepository.GetByUserAsync(userId, cancellationToken);
        var seenResourceIds = userVotes.Select(v => v.ResourceId).ToHashSet();

        // Get recently recommended resources (last 7 days) to avoid repetition
        var recentRecommendations = await _recommendationRepository.GetRecentByUserAsync(
            userId, date.AddDays(-7), date, cancellationToken);
        var recentlyRecommendedIds = recentRecommendations.Select(r => r.ResourceId).ToHashSet();

        // Build recommendation context
        var context = new RecommendationContext
        {
            UserId = userId,
            FeedType = feedType,
            Date = date,
            Count = count,
            UserProfile = userProfile,
            SeenResourceIds = seenResourceIds,
            RecentlyRecommendedIds = recentlyRecommendedIds
        };

        // Generate recommendations
        var scoredResources = await _engine.GenerateRecommendationsAsync(context, cancellationToken);

        if (!scoredResources.Any())
        {
            _logger.LogWarning(
                "No recommendations generated for user {UserId}, feed {FeedType}",
                userId, feedType);
            return new List<Core.Entities.Recommendation>();
        }

        // Convert to Recommendation entities and persist
        var recommendations = new List<Core.Entities.Recommendation>();
        var position = 1;

        foreach (var scored in scoredResources)
        {
            var recommendation = new Core.Entities.Recommendation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ResourceId = scored.Resource.Id,
                FeedType = feedType,
                Date = date,
                Position = position++,
                Score = scored.FinalScore,
                GeneratedAt = DateTime.UtcNow
            };

            await _recommendationRepository.AddAsync(recommendation, cancellationToken);
            recommendations.Add(recommendation);
        }

        _logger.LogInformation(
            "Generated and saved {Count} recommendations for user {UserId}, feed {FeedType}",
            recommendations.Count, userId, feedType);

        return recommendations;
    }

    public async Task<List<Core.Entities.Recommendation>> GenerateAllFeedsAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating all feeds for user {UserId}, date {Date}",
            userId, date);

        var allRecommendations = new List<Core.Entities.Recommendation>();
        var feedTypes = Enum.GetValues<ResourceType>();

        foreach (var feedType in feedTypes)
        {
            var feedRecommendations = await GenerateFeedAsync(
                userId, feedType, date, count: 5, cancellationToken);

            allRecommendations.AddRange(feedRecommendations);
        }

        _logger.LogInformation(
            "Generated {Count} total recommendations across all feeds for user {UserId}",
            allRecommendations.Count, userId);

        return allRecommendations;
    }
}

