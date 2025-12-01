using Rsl.Core.Entities;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Scorers;

/// <summary>
/// Scores resources based on source alignment with user interests.
/// </summary>
public class TopicScorer : IResourceScorer
{
    public double Weight => 0.5; // 50% of final score

    public Task<double> ScoreAsync(
        Resource resource,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // If no user profile or no source, return neutral score
        if (context.UserProfile == null || !resource.SourceId.HasValue)
        {
            return Task.FromResult(0.5);
        }

        // Get the score for this resource's source
        var sourceScore = context.UserProfile.GetTopicScore(resource.SourceId.Value);

        return Task.FromResult(sourceScore);
    }
}

