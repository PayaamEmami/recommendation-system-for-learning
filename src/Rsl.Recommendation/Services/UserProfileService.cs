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

        // Calculate topic scores based on votes
        var topicScores = new Dictionary<Guid, double>();
        var topicCounts = new Dictionary<Guid, int>();

        foreach (var vote in votesList)
        {
            var weight = vote.VoteType == VoteType.Upvote ? 1.0 : -0.5;

            foreach (var topic in vote.Resource.Topics)
            {
                if (!topicScores.ContainsKey(topic.Id))
                {
                    topicScores[topic.Id] = 0;
                    topicCounts[topic.Id] = 0;
                }

                topicScores[topic.Id] += weight;
                topicCounts[topic.Id]++;
            }
        }

        // Normalize scores to 0.0 - 1.0 range
        if (topicScores.Any())
        {
            var maxScore = topicScores.Values.Max();
            var minScore = topicScores.Values.Min();
            var range = maxScore - minScore;

            foreach (var topicId in topicScores.Keys.ToList())
            {
                if (range > 0)
                {
                    // Normalize to 0-1
                    var normalizedScore = (topicScores[topicId] - minScore) / range;
                    profile.SetTopicScore(topicId, normalizedScore);
                }
                else
                {
                    // All scores are the same
                    profile.SetTopicScore(topicId, 0.5);
                }
            }
        }

        return profile;
    }
}

