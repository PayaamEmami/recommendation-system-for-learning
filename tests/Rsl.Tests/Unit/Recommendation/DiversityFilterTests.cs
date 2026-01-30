using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Recommendation.Filters;
using Rsl.Recommendation.Models;

namespace Rsl.Tests.Unit.Recommendation;

[TestClass]
public sealed class DiversityFilterTests
{
    [TestMethod]
    public async Task FilterAsync_LimitsResourcesPerSource()
    {
        var filter = new DiversityFilter();
        var sourceId = Guid.NewGuid();
        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ResourceType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var candidates = new List<ScoredResource>
        {
            BuildCandidate(sourceId, 0.9),
            BuildCandidate(sourceId, 0.8),
            BuildCandidate(sourceId, 0.7),
            BuildCandidate(sourceId, 0.6),
            BuildCandidate(null, 0.5)
        };

        var filtered = await filter.FilterAsync(candidates, context);

        var sameSourceCount = filtered.Count(sr => sr.Resource.SourceId == sourceId);
        Assert.AreEqual(3, sameSourceCount);
        Assert.HasCount(4, filtered);
    }

    private static ScoredResource BuildCandidate(Guid? sourceId, double score)
    {
        var resource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Resource",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SourceId = sourceId
        };

        return new ScoredResource
        {
            Resource = resource,
            FinalScore = score
        };
    }
}
