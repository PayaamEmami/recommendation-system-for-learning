using Crs.Core.Entities;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Scorers;

/// <summary>
/// Scores content based on source alignment with user interests.
/// Boosts content from sources the user has historically liked.
/// </summary>
public class SourceScorer : IContentScorer
{
    public double Weight => 0.5; // 50% of final score

    public Task<double> ScoreAsync(
        Content content,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // If no user profile or no source, return neutral score
        if (context.UserProfile == null || !content.SourceId.HasValue)
        {
            return Task.FromResult(0.5);
        }

        // Get the score for this content's source
        var sourceScore = context.UserProfile.GetTopicScore(content.SourceId.Value);

        return Task.FromResult(sourceScore);
    }
}

