using Crs.Recommendation.Models;

namespace Crs.Recommendation.Filters;

/// <summary>
/// Filters out content the user has already seen or interacted with.
/// </summary>
public class SeenContentFilter : IRecommendationFilter
{
    public Task<List<ScoredContent>> FilterAsync(
        List<ScoredContent> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Remove content that:
        // 1. User has already voted on (seen)
        // 2. Were recently recommended
        var filtered = candidates
            .Where(sr => !context.SeenContentIds.Contains(sr.Content.Id))
            .Where(sr => !context.RecentlyRecommendedIds.Contains(sr.Content.Id))
            .ToList();

        return Task.FromResult(filtered);
    }
}

