using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Scorers;

/// <summary>
/// Scores resources based on similarity to previously upvoted content.
/// Boosts resources with topics similar to upvoted items.
/// Penalizes resources similar to downvoted items.
/// </summary>
public class VoteHistoryScorer : IResourceScorer
{
    private readonly IResourceVoteRepository _voteRepository;

    public VoteHistoryScorer(IResourceVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public double Weight => 0.2; // 20% of final score

    public async Task<double> ScoreAsync(
        Resource resource,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Get user's vote history
        var userVotes = await _voteRepository.GetByUserAsync(context.UserId, cancellationToken);

        if (!userVotes.Any() || !resource.Topics.Any())
        {
            return 0.5; // Neutral score
        }

        var resourceTopicIds = resource.Topics.Select(t => t.Id).ToHashSet();

        double upvoteScore = 0;
        double downvoteScore = 0;
        int upvoteCount = 0;
        int downvoteCount = 0;

        foreach (var vote in userVotes)
        {
            var votedResourceTopics = vote.Resource.Topics.Select(t => t.Id).ToHashSet();
            var overlap = resourceTopicIds.Intersect(votedResourceTopics).Count();

            if (overlap == 0) continue;

            // Calculate topic similarity (0.0 to 1.0)
            var similarity = (double)overlap / Math.Max(resourceTopicIds.Count, votedResourceTopics.Count);

            if (vote.VoteType == VoteType.Upvote)
            {
                upvoteScore += similarity;
                upvoteCount++;
            }
            else if (vote.VoteType == VoteType.Downvote)
            {
                downvoteScore += similarity;
                downvoteCount++;
            }
        }

        // Calculate average similarity to upvoted/downvoted content
        var avgUpvoteScore = upvoteCount > 0 ? upvoteScore / upvoteCount : 0;
        var avgDownvoteScore = downvoteCount > 0 ? downvoteScore / downvoteCount : 0;

        // Combine: boost for upvote similarity, penalty for downvote similarity
        var finalScore = 0.5 + (avgUpvoteScore * 0.5) - (avgDownvoteScore * 0.5);

        return Math.Clamp(finalScore, 0.0, 1.0);
    }
}

