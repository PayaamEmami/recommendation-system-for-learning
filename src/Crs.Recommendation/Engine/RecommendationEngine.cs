using Microsoft.Extensions.Logging;
using Crs.Core.Interfaces;
using Crs.Core.Models;
using Crs.Recommendation.Filters;
using Crs.Recommendation.Models;
using Crs.Recommendation.Scorers;

namespace Crs.Recommendation.Engine;

/// <summary>
/// Hybrid recommendation engine that combines vector similarity with traditional scoring.
/// Primary recommendations come from vector search, with additional heuristic signals layered on top.
/// </summary>
public class RecommendationEngine : IRecommendationEngine
{
    private readonly IVectorStore _vectorStore;
    private readonly IContentRepository _contentRepository;
    private readonly CompositeScorer _compositeScorer;
    private readonly IEnumerable<IRecommendationFilter> _filters;
    private readonly ILogger<RecommendationEngine> _logger;

    public RecommendationEngine(
        IVectorStore vectorStore,
        IContentRepository contentRepository,
        CompositeScorer compositeScorer,
        IEnumerable<IRecommendationFilter> filters,
        ILogger<RecommendationEngine> logger)
    {
        _vectorStore = vectorStore;
        _contentRepository = contentRepository;
        _compositeScorer = compositeScorer;
        _filters = filters;
        _logger = logger;
    }

    public async Task<List<ScoredContent>> GenerateRecommendationsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating {Count} recommendations for user {UserId}, feed type {FeedType}, date {Date}",
            context.Count, context.UserId, context.FeedType, context.Date);

        // Step 1: Get candidates via vector similarity (if user has embedding)
        List<ScoredContent> scoredCandidates;

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
            _logger.LogWarning("No candidate content found for user {UserId}", context.UserId);
            return new List<ScoredContent>();
        }

        _logger.LogDebug("Found {Count} candidate content", scoredCandidates.Count);

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
    private async Task<List<ScoredContent>> GetVectorSimilarityCandidatesAsync(
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
                ContentType = context.FeedType,
                PublishedAfter = cutoffDate,
                ExcludeContentIds = context.SeenContentIds
                    .Union(context.RecentlyRecommendedIds)
                    .ToHashSet()
            };

            var searchResults = await _vectorStore.SearchAsync(searchRequest, cancellationToken);

            if (!searchResults.Any())
            {
                _logger.LogWarning(
                    "Vector search returned no results, falling back to traditional candidates");
                return await GetTraditionalCandidatesAsync(context, cancellationToken);
            }

            // Load full content entities
            var contentIds = searchResults.Select(r => r.ContentId).ToList();
            var content = await _contentRepository.GetByIdsAsync(contentIds, cancellationToken);
            var contentMap = content.ToDictionary(r => r.Id);

            // Create scored content with vector similarity as the primary score
            var scoredContent = searchResults
                .Where(sr => contentMap.ContainsKey(sr.ContentId))
                .Select(sr => new ScoredContent
                {
                    Content = contentMap[sr.ContentId],
                    Scores = new Dictionary<string, double>
                    {
                        { "vector_similarity", sr.SimilarityScore }
                    },
                    FinalScore = sr.SimilarityScore // Will be adjusted by heuristic scoring
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} candidates via vector search", scoredContent.Count);
            return scoredContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector similarity search, falling back to traditional approach");
            return await GetTraditionalCandidatesAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// Fallback method: get candidates using traditional approach (fetch recent content by type).
    /// </summary>
    private async Task<List<ScoredContent>> GetTraditionalCandidatesAsync(
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        var cutoffDate = context.Date.AddDays(-90).ToDateTime(TimeOnly.MinValue);

        var candidates = await _contentRepository.GetByTypeAsync(context.FeedType, cancellationToken);

        var recentCandidates = candidates
            .Where(r => r.CreatedAt >= cutoffDate)
            .ToList();

        // Convert to scored content with neutral vector similarity
        var scoredContent = recentCandidates.Select(r => new ScoredContent
        {
            Content = r,
            Scores = new Dictionary<string, double>
            {
                { "vector_similarity", 0.5 } // Neutral score when not using vector search
            },
            FinalScore = 0.5
        }).ToList();

        return scoredContent;
    }

    /// <summary>
    /// Apply additional heuristic scoring (recency, source preferences, vote history) on top of vector similarity.
    /// </summary>
    private async Task<List<ScoredContent>> ApplyHeuristicScoringAsync(
        List<ScoredContent> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        // Score each content using the composite scorer (recency, source, vote history)
        var heuristicScored = await _compositeScorer.ScoreContentAsync(
            candidates.Select(c => c.Content).ToList(),
            context,
            cancellationToken);

        // Merge heuristic scores with vector similarity scores
        var heuristicScoreMap = heuristicScored.ToDictionary(sr => sr.Content.Id);

        foreach (var candidate in candidates)
        {
            if (heuristicScoreMap.TryGetValue(candidate.Content.Id, out var heuristicScore))
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