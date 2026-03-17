using Crs.Recommendation.Models;

namespace Crs.Recommendation.Filters;

/// <summary>
/// Interface for filters that refine recommendation candidates.
/// </summary>
public interface IRecommendationFilter
{
    /// <summary>
    /// Filter and/or adjust scored content.
    /// </summary>
    /// <param name="candidates">Scored content to filter</param>
    /// <param name="context">Recommendation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered/adjusted list of scored content</returns>
    Task<List<ScoredContent>> FilterAsync(
        List<ScoredContent> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

