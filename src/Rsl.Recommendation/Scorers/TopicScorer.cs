using Rsl.Core.Entities;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Scorers;

/// <summary>
/// Scores resources based on topic alignment with user interests.
/// </summary>
public class TopicScorer : IResourceScorer
{
    public double Weight => 0.5; // 50% of final score

    public Task<double> ScoreAsync(
        Resource resource,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // If no user profile or no topics, return neutral score
        if (context.UserProfile == null || !resource.Topics.Any())
        {
            return Task.FromResult(0.5);
        }

        // Calculate average interest score across all resource topics
        var topicScores = resource.Topics
            .Select(topic => context.UserProfile.GetTopicScore(topic.Id))
            .ToList();

        if (!topicScores.Any())
        {
            return Task.FromResult(0.5);
        }

        // Use average of topic scores
        var averageScore = topicScores.Average();

        // Boost resources with multiple relevant topics
        var topicBonus = Math.Min(topicScores.Count * 0.05, 0.2); // Up to 20% bonus

        var finalScore = Math.Clamp(averageScore + topicBonus, 0.0, 1.0);

        return Task.FromResult(finalScore);
    }
}

