using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Scorers;

/// <summary>
/// Scores resources based on similarity to previously upvoted content.
/// Boosts resources from sources with upvoted items.
/// Penalizes resources from sources with downvoted items.
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

        if (!userVotes.Any() || !resource.SourceId.HasValue)
        {
            return 0.5; // Neutral score
        }

        var resourceSourceId = resource.SourceId.Value;

        double upvoteScore = 0;
        double downvoteScore = 0;
        int upvoteCount = 0;
        int downvoteCount = 0;

        foreach (var vote in userVotes)
        {
            // Check if voted resource is from the same source
            if (vote.Resource.SourceId == resourceSourceId)
            {
                if (vote.VoteType == VoteType.Upvote)
                {
                    upvoteScore += 1.0;
                    upvoteCount++;
                }
                else if (vote.VoteType == VoteType.Downvote)
                {
                    downvoteScore += 1.0;
                    downvoteCount++;
                }
            }
        }

        // Calculate average sentiment for this source
        var totalVotes = upvoteCount + downvoteCount;
        if (totalVotes == 0)
        {
            return 0.5; // No history with this source
        }

        // Calculate score based on upvote ratio
        var upvoteRatio = (double)upvoteCount / totalVotes;

        // Convert ratio to score (0.0 = all downvotes, 1.0 = all upvotes)
        var finalScore = upvoteRatio;

        return Math.Clamp(finalScore, 0.0, 1.0);
    }
}

