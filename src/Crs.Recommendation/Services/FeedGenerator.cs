using Microsoft.Extensions.Logging;
using Crs.Core.Enums;
using Crs.Core.Interfaces;
using Crs.Recommendation.Engine;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Services;

/// <summary>
/// Generates and persists daily recommendation feeds.
/// </summary>
public class FeedGenerator : IFeedGenerator
{
    private readonly IRecommendationEngine _engine;
    private readonly IUserProfileService _profileService;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IContentVoteRepository _voteRepository;
    private readonly ILogger<FeedGenerator> _logger;

    public FeedGenerator(
        IRecommendationEngine engine,
        IUserProfileService profileService,
        IRecommendationRepository recommendationRepository,
        IContentVoteRepository voteRepository,
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
        ContentType feedType,
        DateOnly date,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating feed for user {UserId}, type {FeedType}, date {Date}, count {Count}",
            userId, feedType, date, count);

        // Check if recommendations already exist for this date/feed
        var existing = (await _recommendationRepository.GetByUserDateAndTypeAsync(
            userId, date, feedType, cancellationToken))
            .ToList();

        if (IsCompleteFeed(existing, count))
        {
            _logger.LogInformation(
                "Complete recommendations already exist for user {UserId}, feed {FeedType}, date {Date}",
                userId, feedType, date);
            return existing;
        }

        if (existing.Any())
        {
            _logger.LogWarning(
                "Found incomplete recommendation feed for user {UserId}, feed {FeedType}, date {Date}. Expected {ExpectedCount} items but found {ExistingCount}. Regenerating.",
                userId,
                feedType,
                date,
                count,
                existing.Count);
        }

        // Build user profile
        var userProfile = await _profileService.BuildProfileAsync(userId, cancellationToken);

        // Get content user has already seen (voted on)
        var userVotes = await _voteRepository.GetByUserAsync(userId, cancellationToken);
        var seenContentIds = userVotes.Select(v => v.ContentId).ToHashSet();

        // Get recently recommended content (last 7 days) to avoid repetition
        var recentRecommendations = await _recommendationRepository.GetRecentByUserAsync(
            userId, date.AddDays(-7), date.AddDays(-1), cancellationToken);
        var recentlyRecommendedIds = recentRecommendations.Select(r => r.ContentId).ToHashSet();

        // Build recommendation context
        var context = new RecommendationContext
        {
            UserId = userId,
            FeedType = feedType,
            Date = date,
            Count = count,
            UserProfile = userProfile,
            SeenContentIds = seenContentIds,
            RecentlyRecommendedIds = recentlyRecommendedIds
        };

        // Generate recommendations
        var scoredContent = await _engine.GenerateRecommendationsAsync(context, cancellationToken);

        if (!scoredContent.Any())
        {
            _logger.LogWarning(
                "No recommendations generated for user {UserId}, feed {FeedType}",
                userId, feedType);
            return new List<Core.Entities.Recommendation>();
        }

        // Convert to Recommendation entities and persist
        var recommendations = new List<Core.Entities.Recommendation>();
        var position = 1;

        foreach (var scored in scoredContent)
        {
            var recommendation = new Core.Entities.Recommendation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ContentId = scored.Content.Id,
                FeedType = feedType,
                Date = date,
                Position = position++,
                Score = scored.FinalScore,
                GeneratedAt = DateTime.UtcNow
            };

            recommendations.Add(recommendation);
        }

        await _recommendationRepository.ReplaceFeedAsync(
            userId,
            date,
            feedType,
            recommendations,
            cancellationToken);

        _logger.LogInformation(
            "Generated and saved {Count} recommendations for user {UserId}, feed {FeedType}",
            recommendations.Count, userId, feedType);

        return recommendations;
    }

    private static bool IsCompleteFeed(
        IReadOnlyCollection<Core.Entities.Recommendation> recommendations,
        int expectedCount)
    {
        if (recommendations.Count != expectedCount)
        {
            return false;
        }

        var positions = recommendations
            .Select(r => r.Position)
            .OrderBy(position => position)
            .ToList();

        return positions.Distinct().Count() == expectedCount &&
               positions.First() == 1 &&
               positions.Last() == expectedCount;
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
        var feedTypes = Enum.GetValues<ContentType>();

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
