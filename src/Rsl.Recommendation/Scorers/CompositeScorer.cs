using Rsl.Core.Entities;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Scorers;

/// <summary>
/// Combines multiple scorers into a weighted final score.
/// </summary>
public class CompositeScorer
{
    private readonly IEnumerable<IResourceScorer> _scorers;

    public CompositeScorer(IEnumerable<IResourceScorer> scorers)
    {
        _scorers = scorers;
    }

    /// <summary>
    /// Calculate final weighted score by combining all scorers.
    /// </summary>
    public async Task<ScoredResource> ScoreResourceAsync(
        Resource resource,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var scoredResource = new ScoredResource
        {
            Resource = resource
        };

        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var scorer in _scorers)
        {
            var score = await scorer.ScoreAsync(resource, context, cancellationToken);
            weightedSum += score * scorer.Weight;
            totalWeight += scorer.Weight;

            // Store individual scores for transparency
            switch (scorer)
            {
                case TopicScorer:
                    scoredResource.Scores.TopicScore = score;
                    break;
                case RecencyScorer:
                    scoredResource.Scores.RecencyScore = score;
                    break;
                case VoteHistoryScorer:
                    scoredResource.Scores.VoteHistoryScore = score;
                    break;
            }
        }

        // Calculate final weighted average
        scoredResource.FinalScore = totalWeight > 0 ? weightedSum / totalWeight : 0.5;

        return scoredResource;
    }

    /// <summary>
    /// Score multiple resources in parallel.
    /// </summary>
    public async Task<List<ScoredResource>> ScoreResourcesAsync(
        IEnumerable<Resource> resources,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var scoringTasks = resources.Select(r => ScoreResourceAsync(r, context, cancellationToken));
        var scoredResources = await Task.WhenAll(scoringTasks);
        return scoredResources.ToList();
    }
}

