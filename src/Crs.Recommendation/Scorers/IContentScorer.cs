using Crs.Core.Entities;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Scorers;

/// <summary>
/// Interface for components that score content for recommendation.
/// </summary>
public interface IContentScorer
{
    /// <summary>
    /// Calculate a score for a piece of content (0.0 to 1.0).
    /// </summary>
    /// <param name="content">Content to score</param>
    /// <param name="context">Recommendation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Score between 0.0 and 1.0</returns>
    Task<double> ScoreAsync(
        Content content,
        RecommendationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Weight/importance of this scorer in the final score (0.0 to 1.0).
    /// </summary>
    double Weight { get; }
}
