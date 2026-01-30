using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Recommendation.Filters;
using Rsl.Recommendation.Models;

namespace Rsl.Tests.Unit.Recommendation;

[TestClass]
public sealed class SeenResourceFilterTests
{
    [TestMethod]
    public async Task FilterAsync_RemovesSeenAndRecentlyRecommended()
    {
        var filter = new SeenResourceFilter();
        var seenId = Guid.NewGuid();
        var recentId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ResourceType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            SeenResourceIds = new HashSet<Guid> { seenId },
            RecentlyRecommendedIds = new HashSet<Guid> { recentId }
        };

        var candidates = new List<ScoredResource>
        {
            BuildCandidate(seenId),
            BuildCandidate(recentId),
            BuildCandidate(keepId)
        };

        var filtered = await filter.FilterAsync(candidates, context);

        Assert.HasCount(1, filtered);
        Assert.AreEqual(keepId, filtered[0].Resource.Id);
    }

    private static ScoredResource BuildCandidate(Guid id)
    {
        var resource = new BlogPost
        {
            Id = id,
            Title = "Resource",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return new ScoredResource
        {
            Resource = resource,
            FinalScore = 0.5
        };
    }
}
