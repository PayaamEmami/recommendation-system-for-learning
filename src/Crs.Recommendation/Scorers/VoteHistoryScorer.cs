using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;
using Crs.Recommendation.Models;
using System.Linq;

namespace Crs.Recommendation.Scorers;

/// <summary>
/// Scores content based on similarity to previously upvoted content.
/// Boosts content from sources with upvoted items.
/// Penalizes content from sources with downvoted items.
/// </summary>
public class VoteHistoryScorer : IContentScorer
{
    private readonly IContentVoteRepository _voteRepository;
    private Guid? _cachedUserId;
    private IEnumerable<ContentVote>? _cachedVotes;

    public VoteHistoryScorer(IContentVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public double Weight => 0.2; // 20% of final score

    public async Task<double> ScoreAsync(
        Content content,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Get user's vote history
        var userVotes = await GetUserVotesCachedAsync(context.UserId, cancellationToken);

        if (!userVotes.Any() || !content.SourceId.HasValue)
        {
            return 0.5; // Neutral score
        }

        var contentSourceId = content.SourceId.Value;

        double upvoteScore = 0;
        double downvoteScore = 0;
        int upvoteCount = 0;
        int downvoteCount = 0;

        foreach (var vote in userVotes)
        {
            // Check if voted content is from the same source
            if (vote.Content.SourceId == contentSourceId)
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

    private async Task<IEnumerable<ContentVote>> GetUserVotesCachedAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (_cachedUserId == userId && _cachedVotes != null)
        {
            return _cachedVotes;
        }

        var votes = await _voteRepository.GetByUserAsync(userId, cancellationToken);
        var voteList = votes.ToList();
        _cachedUserId = userId;
        _cachedVotes = voteList;
        return voteList;
    }
}

