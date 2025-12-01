using Rsl.Core.Entities;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Scorers;

/// <summary>
/// Interface for components that score resources for recommendation.
/// </summary>
public interface IResourceScorer
{
    /// <summary>
    /// Calculate a score for a resource (0.0 to 1.0).
    /// </summary>
    /// <param name="resource">Resource to score</param>
    /// <param name="context">Recommendation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Score between 0.0 and 1.0</returns>
    Task<double> ScoreAsync(
        Resource resource,
        RecommendationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Weight/importance of this scorer in the final score (0.0 to 1.0).
    /// </summary>
    double Weight { get; }
}

