using Crs.Core.Entities;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Engine;

/// <summary>
/// Core recommendation engine that generates personalized recommendations.
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Generate recommendations for a user based on context.
    /// </summary>
    /// <param name="context">Recommendation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommended content with scores</returns>
    Task<List<ScoredContent>> GenerateRecommendationsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

