using Crs.Core.Entities;
using Crs.Recommendation.Models;

namespace Crs.Recommendation.Scorers;

/// <summary>
/// Combines multiple scorers into a weighted final score.
/// </summary>
public class CompositeScorer
{
    private readonly IEnumerable<IContentScorer> _scorers;

    public CompositeScorer(IEnumerable<IContentScorer> scorers)
    {
        _scorers = scorers;
    }

    /// <summary>
    /// Calculate final weighted score by combining all scorers.
    /// </summary>
    public async Task<ScoredContent> ScoreContentAsync(
        Content content,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var scoredContent = new ScoredContent
        {
            Content = content
        };

        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var scorer in _scorers)
        {
            var score = await scorer.ScoreAsync(content, context, cancellationToken);
            weightedSum += score * scorer.Weight;
            totalWeight += scorer.Weight;

            // Store individual scores for transparency (using scorer type name as key)
            var scorerName = scorer.GetType().Name.Replace("Scorer", "").ToLowerInvariant();
            scoredContent.Scores[scorerName] = score;
        }

        // Calculate final weighted average
        scoredContent.FinalScore = totalWeight > 0 ? weightedSum / totalWeight : 0.5;

        return scoredContent;
    }

    /// <summary>
    /// Score multiple content in parallel.
    /// </summary>
    public async Task<List<ScoredContent>> ScoreContentAsync(
        IEnumerable<Content> contentItems,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Process sequentially to avoid reusing scoped DbContexts across concurrent tasks
        var results = new List<ScoredContent>();
        foreach (var contentItem in contentItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ScoreContentAsync(contentItem, context, cancellationToken));
        }

        return results;
    }
}

