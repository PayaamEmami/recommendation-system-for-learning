using Rsl.Core.Entities;
using Rsl.Core.Enums;

namespace Rsl.Recommendation.Models;

/// <summary>
/// Context information for generating recommendations.
/// </summary>
public class RecommendationContext
{
    /// <summary>
    /// User to generate recommendations for.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Type of feed to generate recommendations for.
    /// </summary>
    public required ResourceType FeedType { get; set; }

    /// <summary>
    /// Date these recommendations are for.
    /// </summary>
    public required DateOnly Date { get; set; }

    /// <summary>
    /// Number of recommendations to generate.
    /// </summary>
    public int Count { get; set; } = 5;

    /// <summary>
    /// User's interest profile (calculated from history).
    /// </summary>
    public UserInterestProfile? UserProfile { get; set; }

    /// <summary>
    /// Resources the user has already interacted with (to exclude).
    /// </summary>
    public HashSet<Guid> SeenResourceIds { get; set; } = new();

    /// <summary>
    /// Resources already recommended to this user recently (to avoid repetition).
    /// </summary>
    public HashSet<Guid> RecentlyRecommendedIds { get; set; } = new();
}

