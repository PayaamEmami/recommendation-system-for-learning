using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Recommendation.Models;
using Rsl.Recommendation.Scorers;

namespace Rsl.Tests.Unit.Recommendation;

[TestClass]
public sealed class RecencyScorerTests
{
    [TestMethod]
    public async Task ScoreAsync_WhenResourceIsToday_ReturnsOne()
    {
        var scorer = new RecencyScorer();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "New",
            Url = "https://example.com/new",
            CreatedAt = today.ToDateTime(TimeOnly.MinValue),
            UpdatedAt = today.ToDateTime(TimeOnly.MinValue)
        };

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ResourceType.BlogPost,
            Date = today
        };

        var score = await scorer.ScoreAsync(resource, context);

        Assert.AreEqual(1.0, score, 0.0001);
    }

    [TestMethod]
    public async Task ScoreAsync_WhenResourceIsThirtyDaysOld_Decays()
    {
        var scorer = new RecencyScorer();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdAt = today.AddDays(-30).ToDateTime(TimeOnly.MinValue);
        var resource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Older",
            Url = "https://example.com/old",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ResourceType.BlogPost,
            Date = today
        };

        var score = await scorer.ScoreAsync(resource, context);

        var expected = Math.Exp(-30.0 / 30.0);
        Assert.AreEqual(expected, score, 0.0001);
    }
}
