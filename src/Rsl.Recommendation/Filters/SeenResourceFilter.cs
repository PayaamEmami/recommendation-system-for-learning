using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Filters;

/// <summary>
/// Filters out resources the user has already seen or interacted with.
/// </summary>
public class SeenResourceFilter : IRecommendationFilter
{
    public Task<List<ScoredResource>> FilterAsync(
        List<ScoredResource> candidates,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Remove resources that:
        // 1. User has already voted on (seen)
        // 2. Were recently recommended
        var filtered = candidates
            .Where(sr => !context.SeenResourceIds.Contains(sr.Resource.Id))
            .Where(sr => !context.RecentlyRecommendedIds.Contains(sr.Resource.Id))
            .ToList();

        return Task.FromResult(filtered);
    }
}

