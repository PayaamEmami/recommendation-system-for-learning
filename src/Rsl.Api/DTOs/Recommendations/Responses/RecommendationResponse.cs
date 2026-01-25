using Rsl.Api.DTOs.Resources.Responses;

namespace Rsl.Api.DTOs.Recommendations.Responses;

/// <summary>
/// Response model for a single recommendation.
/// </summary>
public class RecommendationResponse
{
    /// <summary>
    /// The recommendation's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The recommended resource.
    /// </summary>
    public ResourceResponse Resource { get; set; } = null!;

    /// <summary>
    /// The position/rank in the feed (1 = top).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Confidence score from the recommendation engine.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// When this recommendation was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

