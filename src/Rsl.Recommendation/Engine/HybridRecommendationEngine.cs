using Microsoft.Extensions.Logging;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Recommendation.Filters;
using Rsl.Recommendation.Models;
using Rsl.Recommendation.Scorers;

namespace Rsl.Recommendation.Engine;

/// <summary>
/// Hybrid recommendation engine that combines vector similarity with traditional scoring.
/// Primary recommendations come from vector search, with additional heuristic signals layered on top.
/// </summary>
public class HybridRecommendationEngine : IRecommendationEngine
{
    private readonly IVectorStore _vectorStore;
    private readonly IResourceRepository _resourceRepository;
    private readonly CompositeScorer _compositeScorer;
    private readonly IEnumerable<IRecommendationFilter> _filters;
    private readonly ILogger<HybridRecommendationEngine> _logger;

    public HybridRecommendationEngine(
        IVectorStore vectorStore,
        IResourceRepository resourceRepository,
        CompositeScorer compositeScorer,
        IEnumerable<IRecommendationFilter> filters,
        ILogger<HybridRecommendationEngine> logger)
    {
        _vectorStore = vectorStore;
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

        // Step 1: Get candidates via vector similarity (if user has embedding)
        List<ScoredResource> scoredCandidates;

        if (context.UserProfile?.UserEmbedding != null && context.UserProfile.UserEmbedding.Length > 0)
        {
            scoredCandidates = await GetVectorSimilarityCandidatesAsync(context, cancellationToken);
        }
        else
        {
            // Fallback to traditional candidate fetching if no user embedding
            _logger.LogInformation("No user embedding available, falling back to traditional candidate fetching");
            scoredCandidates = await GetTraditionalCandidatesAsync(context, cancellationToken);
        }

        if (!scoredCandidates.Any())
        {
            _logger.LogWarning("No candidate resources found for user {UserId}", context.UserId);
            return new List<ScoredResource>();
        }

        _logger.LogDebug("Found {Count} candidate resources", scoredCandidates.Count);

        // Step 2: Apply additional heuristic scoring (recency, source preference, etc.)
        scoredCandidates = await ApplyHeuristicScoringAsync(scoredCandidates, context, cancellationToken);

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
    /// Get candidates using vector similarity search.
    /// </summary>
    private async Task<List<ScoredResource>> GetVectorSimilarityCandidatesAsync(
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = context.Date.AddDays(-90).ToDateTime(TimeOnly.MinValue);

            var searchRequest = new VectorSearchRequest
            {
                QueryVector = context.UserProfile!.UserEmbedding!,
                TopK = context.Count * 10, // Get more candidates to allow for filtering
                ResourceType = context.FeedType,
                PublishedAfter = cutoffDate,
                ExcludeResourceIds = context.SeenResourceIds
                    .Union(context.RecentlyRecommendedIds)
                    .ToHashSet()
            };

            var searchResults = await _vectorStore.SearchAsync(searchRequest, cancellationToken);

            if (!searchResults.Any())
            {
                _logger.LogWarning("Vector search returned no results");
                return new List<ScoredResource>();
            }

            // Load full resource entities
            var resourceIds = searchResults.Select(r => r.ResourceId).ToList();
            var resources = await _resourceRepository.GetByIdsAsync(resourceIds, cancellationToken);
            var resourceMap = resources.ToDictionary(r => r.Id);

            // Create scored resources with vector similarity as the primary score
            var scoredResources = searchResults
                .Where(sr => resourceMap.ContainsKey(sr.ResourceId))
                .Select(sr => new ScoredResource
                {
                    Resource = resourceMap[sr.ResourceId],
                    Scores = new Dictionary<string, double>
                    {
                        { "vector_similarity", sr.SimilarityScore }
                    },
                    FinalScore = sr.SimilarityScore // Will be adjusted by heuristic scoring
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} candidates via vector search", scoredResources.Count);
            return scoredResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector similarity search, falling back to traditional approach");
            return await GetTraditionalCandidatesAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// Fallback method: get candidates using traditional approach (fetch recent resources by type).
    /// </summary>
    private async Task<List<ScoredResource>> GetTraditionalCandidatesAsync(
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        var cutoffDate = context.Date.AddDays(-90).ToDateTime(TimeOnly.MinValue);

        var candidates = await _resourceRepository.GetByTypeAsync(context.FeedType, cancellationToken);

        var recentCandidates = candidates
            .Where(r => r.CreatedAt >= cutoffDate)
            .ToList();

        // Convert to scored resources with neutral vector similarity
        var scoredResources = recentCandidates.Select(r => new ScoredResource
        {
            Resource = r,
            Scores = new Dictionary<string, double>
            {
                { "vector_similarity", 0.5 } // Neutral score when not using vector search
            },
            FinalScore = 0.5
        }).ToList();

        return scoredResources;
    }

    /// <summary>
    /// Apply additional heuristic scoring (recency, source preferences, vote history) on top of vector similarity.
    /// </summary>
    private async Task<List<ScoredResource>> ApplyHeuristicScoringAsync(
        List<ScoredResource> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        // Score each resource using the composite scorer (recency, source, vote history)
        var heuristicScored = await _compositeScorer.ScoreResourcesAsync(
            candidates.Select(c => c.Resource).ToList(),
            context,
            cancellationToken);

        // Merge heuristic scores with vector similarity scores
        var heuristicScoreMap = heuristicScored.ToDictionary(sr => sr.Resource.Id);

        foreach (var candidate in candidates)
        {
            if (heuristicScoreMap.TryGetValue(candidate.Resource.Id, out var heuristicScore))
            {
                // Merge all scores
                foreach (var kvp in heuristicScore.Scores)
                {
                    candidate.Scores[kvp.Key] = kvp.Value;
                }

                // Combine vector similarity (70%) with heuristic signals (30%)
                var vectorScore = candidate.Scores.TryGetValue("vector_similarity", out var vs) ? vs : 0.5;
                var heuristicFinalScore = heuristicScore.FinalScore;

                candidate.FinalScore = (vectorScore * 0.7) + (heuristicFinalScore * 0.3);
            }
        }

        return candidates;
    }
}
