using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Recommendation.Filters;
using Crs.Recommendation.Models;

namespace Crs.Tests.Unit.Recommendation;

[TestClass]
public sealed class SeenContentFilterTests
{
    [TestMethod]
    public async Task FilterAsync_RemovesSeenAndRecentlyRecommended()
    {
        var filter = new SeenContentFilter();
        var seenId = Guid.NewGuid();
        var recentId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ContentType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            SeenContentIds = new HashSet<Guid> { seenId },
            RecentlyRecommendedIds = new HashSet<Guid> { recentId }
        };

        var candidates = new List<ScoredContent>
        {
            BuildCandidate(seenId),
            BuildCandidate(recentId),
            BuildCandidate(keepId)
        };

        var filtered = await filter.FilterAsync(candidates, context);

        Assert.HasCount(1, filtered);
        Assert.AreEqual(keepId, filtered[0].Content.Id);
    }

    private static ScoredContent BuildCandidate(Guid id)
    {
        var content = new BlogPost
        {
            Id = id,
            Title = "Content",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return new ScoredContent
        {
            Content = content,
            FinalScore = 0.5
        };
    }
}
