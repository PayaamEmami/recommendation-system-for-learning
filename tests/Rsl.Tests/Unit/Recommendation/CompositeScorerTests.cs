using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Recommendation.Models;
using Rsl.Recommendation.Scorers;

namespace Rsl.Tests.Unit.Recommendation;

[TestClass]
public sealed class CompositeScorerTests
{
    [TestMethod]
    public async Task ScoreResourceAsync_CombinesWeightedScores()
    {
        var scorers = new IResourceScorer[]
        {
            new FixedScoreScorer(0.2, 0.4),
            new FixedScoreScorer(0.8, 0.6)
        };

        var composite = new CompositeScorer(scorers);
        var resource = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Resource",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ResourceType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var scored = await composite.ScoreResourceAsync(resource, context);

        var expected = (0.2 * 0.4 + 0.8 * 0.6) / (0.4 + 0.6);
        Assert.AreEqual(expected, scored.FinalScore, 0.0001);
        Assert.IsTrue(scored.Scores.ContainsKey("fixedscore"));
    }

    private sealed class FixedScoreScorer : IResourceScorer
    {
        private readonly double _score;

        public FixedScoreScorer(double score, double weight)
        {
            _score = score;
            Weight = weight;
        }

        public double Weight { get; }

        public Task<double> ScoreAsync(Resource resource, RecommendationContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_score);
        }
    }
}
