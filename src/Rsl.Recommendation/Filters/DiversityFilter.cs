using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Filters;

/// <summary>
/// Ensures topic diversity in recommendations.
/// Prevents all recommendations from being about the same topic.
/// </summary>
public class DiversityFilter : IRecommendationFilter
{
    // Maximum number of resources from the same topic
    private const int MaxPerTopic = 2;

    public Task<List<ScoredResource>> FilterAsync(
        List<ScoredResource> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var topicCounts = new Dictionary<Guid, int>();
        var diversified = new List<ScoredResource>();

        // Sort by score descending (best first)
        var sortedCandidates = candidates.OrderByDescending(sr => sr.FinalScore).ToList();

        foreach (var candidate in sortedCandidates)
        {
            // Check topic counts for this resource
            var resourceTopicIds = candidate.Resource.Topics.Select(t => t.Id).ToList();

            // Check if any topic is at max count
            var hasOverrepresentedTopic = resourceTopicIds.Any(topicId =>
                topicCounts.TryGetValue(topicId, out var count) && count >= MaxPerTopic);

            if (!hasOverrepresentedTopic)
            {
                // Add this resource
                diversified.Add(candidate);

                // Increment topic counts
                foreach (var topicId in resourceTopicIds)
                {
                    topicCounts[topicId] = topicCounts.GetValueOrDefault(topicId, 0) + 1;
                }

                // Apply small diversity penalty to score (for transparency)
                var diversityPenalty = CalculateDiversityPenalty(resourceTopicIds, topicCounts);
                candidate.Scores.DiversityPenalty = diversityPenalty;
                candidate.FinalScore -= diversityPenalty;
            }
        }

        return Task.FromResult(diversified);
    }

    private double CalculateDiversityPenalty(List<Guid> topicIds, Dictionary<Guid, int> topicCounts)
    {
        // Penalize topics that are becoming overrepresented
        var maxCount = topicIds.Select(id => topicCounts.GetValueOrDefault(id, 0)).Max();

        return maxCount switch
        {
            0 => 0.0,      // First occurrence - no penalty
            1 => 0.02,     // Second occurrence - small penalty
            _ => 0.05      // Beyond - larger penalty
        };
    }
}

