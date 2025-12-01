using Rsl.Core.Entities;

namespace Rsl.Recommendation.Models;

/// <summary>
/// Represents a resource with calculated recommendation scores.
/// </summary>
public class ScoredResource
{
    public Resource Resource { get; set; } = null!;

    /// <summary>
    /// Individual component scores.
    /// </summary>
    public ScoreBreakdown Scores { get; set; } = new();

    /// <summary>
    /// Final combined score used for ranking.
    /// </summary>
    public double FinalScore { get; set; }

    /// <summary>
    /// Breakdown of score components for debugging/explanation.
    /// </summary>
    public class ScoreBreakdown
    {
        public double TopicScore { get; set; }
        public double RecencyScore { get; set; }
        public double VoteHistoryScore { get; set; }
        public double DiversityPenalty { get; set; }
    }
}

