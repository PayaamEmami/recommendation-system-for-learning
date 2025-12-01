namespace Rsl.Recommendation.Models;

/// <summary>
/// Represents a user's interest profile based on their interaction history.
/// Used to score resources based on topic alignment.
/// </summary>
public class UserInterestProfile
{
    /// <summary>
    /// User ID this profile belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Topic scores - higher values indicate stronger interest.
    /// Key: Topic ID, Value: Interest score (0.0 - 1.0)
    /// </summary>
    public Dictionary<Guid, double> TopicScores { get; set; } = new();

    /// <summary>
    /// When this profile was last calculated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Total number of interactions (votes) used to build this profile.
    /// </summary>
    public int TotalInteractions { get; set; }

    /// <summary>
    /// Get interest score for a specific topic (0.0 if not found).
    /// </summary>
    public double GetTopicScore(Guid topicId)
    {
        return TopicScores.TryGetValue(topicId, out var score) ? score : 0.0;
    }

    /// <summary>
    /// Add or update a topic score.
    /// </summary>
    public void SetTopicScore(Guid topicId, double score)
    {
        TopicScores[topicId] = Math.Clamp(score, 0.0, 1.0);
    }
}

