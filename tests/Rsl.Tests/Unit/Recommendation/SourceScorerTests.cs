using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Recommendation.Models;
using Rsl.Recommendation.Scorers;

namespace Rsl.Tests.Unit.Recommendation;

[TestClass]
public sealed class SourceScorerTests
{
    [TestMethod]
    public async Task ScoreAsync_WhenNoProfile_ReturnsNeutral()
    {
        var scorer = new SourceScorer();
        var resource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Resource",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SourceId = Guid.NewGuid()
        };

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ResourceType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var score = await scorer.ScoreAsync(resource, context);

        Assert.AreEqual(0.5, score, 0.0001);
    }

    [TestMethod]
    public async Task ScoreAsync_UsesUserProfileTopicScore()
    {
        var scorer = new SourceScorer();
        var sourceId = Guid.NewGuid();
        var resource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Resource",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SourceId = sourceId
        };

        var profile = new UserInterestProfile { UserId = Guid.NewGuid() };
        profile.SetTopicScore(sourceId, 0.82);

        var context = new RecommendationContext
        {
            UserId = profile.UserId,
            FeedType = ResourceType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            UserProfile = profile
        };

        var score = await scorer.ScoreAsync(resource, context);

        Assert.AreEqual(0.82, score, 0.0001);
    }
}
