using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Services;

/// <summary>
/// Builds user interest profiles from voting history.
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly IResourceVoteRepository _voteRepository;

    public UserProfileService(IResourceVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<UserInterestProfile> BuildProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = new UserInterestProfile
        {
            UserId = userId,
            LastUpdated = DateTime.UtcNow
        };

        // Get all user votes
        var votes = await _voteRepository.GetByUserAsync(userId, cancellationToken);

        var votesList = votes.ToList();

        if (!votesList.Any())
        {
            // No interaction history - return empty profile
            return profile;
        }

        profile.TotalInteractions = votesList.Count;

        // Calculate source scores based on votes
        var sourceScores = new Dictionary<Guid, double>();
        var sourceCounts = new Dictionary<Guid, int>();

        foreach (var vote in votesList)
        {
            var weight = vote.VoteType == VoteType.Upvote ? 1.0 : -0.5;

            // If resource has a source, track score for that source
            if (vote.Resource.SourceId.HasValue)
            {
                var sourceId = vote.Resource.SourceId.Value;

                if (!sourceScores.ContainsKey(sourceId))
                {
                    sourceScores[sourceId] = 0;
                    sourceCounts[sourceId] = 0;
                }

                sourceScores[sourceId] += weight;
                sourceCounts[sourceId]++;
            }
        }

        // Normalize scores to 0.0 - 1.0 range
        if (sourceScores.Any())
        {
            var maxScore = sourceScores.Values.Max();
            var minScore = sourceScores.Values.Min();
            var range = maxScore - minScore;

            foreach (var sourceId in sourceScores.Keys.ToList())
            {
                if (range > 0)
                {
                    // Normalize to 0-1
                    var normalizedScore = (sourceScores[sourceId] - minScore) / range;
                    profile.SetTopicScore(sourceId, normalizedScore);
                }
                else
                {
                    // All scores are the same
                    profile.SetTopicScore(sourceId, 0.5);
                }
            }
        }

        return profile;
    }
}

