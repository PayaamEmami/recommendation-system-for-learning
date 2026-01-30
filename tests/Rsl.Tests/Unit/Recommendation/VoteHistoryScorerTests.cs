using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Recommendation.Models;
using Rsl.Recommendation.Scorers;
using Rsl.Tests.Unit.Infrastructure;

namespace Rsl.Tests.Unit.Recommendation;

[TestClass]
public sealed class VoteHistoryScorerTests
{
    [TestMethod]
    public async Task ScoreAsync_WhenNoVotes_ReturnsNeutral()
    {
        var voteRepository = new InMemoryResourceVoteRepository();
        var scorer = new VoteHistoryScorer(voteRepository);
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
    public async Task ScoreAsync_UsesUpvoteRatioForSource()
    {
        var voteRepository = new InMemoryResourceVoteRepository();
        var scorer = new VoteHistoryScorer(voteRepository);
        var sourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var votedResource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Voted",
            Url = "https://example.com/voted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SourceId = sourceId
        };

        await voteRepository.CreateAsync(new ResourceVote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = votedResource.Id,
            Resource = votedResource,
            VoteType = VoteType.Upvote,
            CreatedAt = DateTime.UtcNow
        });

        await voteRepository.CreateAsync(new ResourceVote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = votedResource.Id,
            Resource = votedResource,
            VoteType = VoteType.Upvote,
            CreatedAt = DateTime.UtcNow
        });

        await voteRepository.CreateAsync(new ResourceVote
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = votedResource.Id,
            Resource = votedResource,
            VoteType = VoteType.Downvote,
            CreatedAt = DateTime.UtcNow
        });

        var targetResource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Target",
            Url = "https://example.com/target",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SourceId = sourceId
        };

        var context = new RecommendationContext
        {
            UserId = userId,
            FeedType = ResourceType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var score = await scorer.ScoreAsync(targetResource, context);

        Assert.AreEqual(2.0 / 3.0, score, 0.0001);
    }
}
