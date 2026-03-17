using Crs.Core.Entities;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Scorers;

/// <summary>
/// Scores content based on recency/freshness.
/// Newer content gets higher scores with exponential decay.
/// </summary>
public class RecencyScorer : IContentScorer
{
    public double Weight => 0.3; // 30% of final score

    // How many days until score decays to ~37% (1/e)
    private const double HalfLifeDays = 30.0;

    public Task<double> ScoreAsync(
        Content content,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var publishedDate = content.CreatedAt;
        var today = context.Date.ToDateTime(TimeOnly.MinValue);
        var ageInDays = (today - publishedDate).TotalDays;

        // Exponential decay: score = e^(-age / halfLife)
        // Recent content (0 days) = 1.0
        // 30 days old ≈ 0.37
        // 60 days old ≈ 0.14
        var score = Math.Exp(-ageInDays / HalfLifeDays);

        return Task.FromResult(Math.Clamp(score, 0.0, 1.0));
    }
}

