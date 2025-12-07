using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Filters;

/// <summary>
/// Ensures source diversity in recommendations.
/// Prevents all recommendations from being from the same source.
/// </summary>
public class DiversityFilter : IRecommendationFilter
{
    // Maximum number of resources from the same source
    private const int MaxPerSource = 3;

    public Task<List<ScoredResource>> FilterAsync(
        List<ScoredResource> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var sourceCounts = new Dictionary<Guid, int>();
        var diversified = new List<ScoredResource>();

        // Sort by score descending (best first)
        var sortedCandidates = candidates.OrderByDescending(sr => sr.FinalScore).ToList();

        foreach (var candidate in sortedCandidates)
        {
            // Check if this resource has a source
            if (candidate.Resource.SourceId.HasValue)
            {
                var sourceId = candidate.Resource.SourceId.Value;
                var currentCount = sourceCounts.GetValueOrDefault(sourceId, 0);

                // Check if source is at max count
                if (currentCount >= MaxPerSource)
                {
                    continue; // Skip this resource
                }

                // Add this resource
                diversified.Add(candidate);
                sourceCounts[sourceId] = currentCount + 1;

                // Apply small diversity penalty to score (for transparency)
                var diversityPenalty = CalculateDiversityPenalty(currentCount);
                candidate.Scores["diversity_penalty"] = diversityPenalty;
                candidate.FinalScore -= diversityPenalty;
            }
            else
            {
                // No source - always include (manual entries)
                diversified.Add(candidate);
            }
        }

        return Task.FromResult(diversified);
    }

    private double CalculateDiversityPenalty(int currentCount)
    {
        // Penalize sources that are becoming overrepresented
        return currentCount switch
        {
            0 => 0.0,      // First occurrence - no penalty
            1 => 0.02,     // Second occurrence - small penalty
            2 => 0.04,     // Third occurrence - larger penalty
            _ => 0.05      // Beyond - largest penalty
        };
    }
}

