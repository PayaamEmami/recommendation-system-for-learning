using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Filters;

/// <summary>
/// Interface for filters that refine recommendation candidates.
/// </summary>
public interface IRecommendationFilter
{
    /// <summary>
    /// Filter and/or adjust scored resources.
    /// </summary>
    /// <param name="candidates">Scored resources to filter</param>
    /// <param name="context">Recommendation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered/adjusted list of scored resources</returns>
    Task<List<ScoredResource>> FilterAsync(
        List<ScoredResource> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

