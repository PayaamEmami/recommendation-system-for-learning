using Rsl.Core.Entities;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Engine;

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
    /// <returns>List of recommended resources with scores</returns>
    Task<List<ScoredResource>> GenerateRecommendationsAsync(
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

