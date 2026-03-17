using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Recommendation.Models;
using Crs.Recommendation.Scorers;

namespace Crs.Tests.Unit.Recommendation;

[TestClass]
public sealed class CompositeScorerTests
{
    [TestMethod]
    public async Task ScoreContentAsync_CombinesWeightedScores()
    {
        var scorers = new IContentScorer[]
        {
            new FixedScoreScorer(0.2, 0.4),
            new FixedScoreScorer(0.8, 0.6)
        };

        var composite = new CompositeScorer(scorers);
        var content = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = "Content",
            Url = "https://example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var context = new RecommendationContext
        {
            UserId = Guid.NewGuid(),
            FeedType = ContentType.BlogPost,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var scored = await composite.ScoreContentAsync(content, context);

        var expected = (0.2 * 0.4 + 0.8 * 0.6) / (0.4 + 0.6);
        Assert.AreEqual(expected, scored.FinalScore, 0.0001);
        Assert.IsTrue(scored.Scores.ContainsKey("fixedscore"));
    }

    private sealed class FixedScoreScorer : IContentScorer
    {
        private readonly double _score;

        public FixedScoreScorer(double score, double weight)
        {
            _score = score;
            Weight = weight;
        }

        public double Weight { get; }

        public Task<double> ScoreAsync(Content content, RecommendationContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_score);
        }
    }
}
