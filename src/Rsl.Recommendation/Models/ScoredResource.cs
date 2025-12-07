using Rsl.Core.Entities;

namespace Rsl.Recommendation.Models;

/// <summary>
/// Represents a resource with calculated recommendation scores.
/// </summary>
public class ScoredResource
{
    public Resource Resource { get; set; } = null!;

    /// <summary>
    /// Individual component scores (key is scorer name, value is score).
    /// </summary>
    public Dictionary<string, double> Scores { get; set; } = new();

    /// <summary>
    /// Final combined score used for ranking.
    /// </summary>
    public double FinalScore { get; set; }

    /// <summary>
    /// Get a specific score by name, or 0.0 if not found.
    /// </summary>
    public double GetScore(string scoreName) => Scores.TryGetValue(scoreName, out var score) ? score : 0.0;
}

