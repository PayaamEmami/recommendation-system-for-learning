using Microsoft.Extensions.Logging;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Filters;
using Rsl.Recommendation.Models;
using Rsl.Recommendation.Scorers;

namespace Rsl.Recommendation.Engine;

/// <summary>
/// Main recommendation engine that orchestrates scoring and filtering.
/// </summary>
public class RecommendationEngine : IRecommendationEngine
{
    private readonly IResourceRepository _resourceRepository;
    private readonly CompositeScorer _compositeScorer;
    private readonly IEnumerable<IRecommendationFilter> _filters;
    private readonly ILogger<RecommendationEngine> _logger;

    public RecommendationEngine(
        IResourceRepository resourceRepository,
        CompositeScorer compositeScorer,
        IEnumerable<IRecommendationFilter> filters,
        ILogger<RecommendationEngine> logger)
    {
        _resourceRepository = resourceRepository;
        _compositeScorer = compositeScorer;
        _filters = filters;
        _logger = logger;
    }

    public async Task<List<ScoredResource>> GenerateRecommendationsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating {Count} recommendations for user {UserId}, feed type {FeedType}, date {Date}",
            context.Count, context.UserId, context.FeedType, context.Date);

        // Step 1: Fetch candidate resources (unscored)
        var candidates = await FetchCandidatesAsync(context, cancellationToken);

        if (!candidates.Any())
        {
            _logger.LogWarning("No candidate resources found for user {UserId}", context.UserId);
            return new List<ScoredResource>();
        }

        _logger.LogDebug("Found {Count} candidate resources", candidates.Count);

        // Step 2: Score all candidates
        var scoredCandidates = await _compositeScorer.ScoreResourcesAsync(
            candidates,
            context,
            cancellationToken);

        _logger.LogDebug("Scored {Count} candidates", scoredCandidates.Count);

        // Step 3: Apply filters (in order)
        var filteredCandidates = scoredCandidates;
        foreach (var filter in _filters)
        {
            filteredCandidates = await filter.FilterAsync(
                filteredCandidates,
                context,
                cancellationToken);

            _logger.LogDebug(
                "After {FilterName}: {Count} candidates remaining",
                filter.GetType().Name, filteredCandidates.Count);
        }

        // Step 4: Sort by final score and take top N
        var recommendations = filteredCandidates
            .OrderByDescending(sr => sr.FinalScore)
            .Take(context.Count)
            .ToList();

        _logger.LogInformation(
            "Generated {Count} recommendations for user {UserId}",
            recommendations.Count, context.UserId);

        return recommendations;
    }

    /// <summary>
    /// Fetch candidate resources that could be recommended.
    /// </summary>
    private async Task<List<Core.Entities.Resource>> FetchCandidatesAsync(
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        // Get resources of the specified type
        // Limit to recent resources (last 90 days) to keep candidate pool manageable
        var cutoffDate = context.Date.AddDays(-90);

        var candidates = await _resourceRepository.GetByTypeAsync(
            context.FeedType,
            cancellationToken);

        // Filter to recent resources only
        var recentCandidates = candidates
            .Where(r => r.CreatedAt >= cutoffDate.ToDateTime(TimeOnly.MinValue))
            .ToList();

        return recentCandidates;
    }
}

