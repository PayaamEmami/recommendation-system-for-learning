using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Recommendations.Responses;

/// <summary>
/// Response model for recommendations in a specific feed type.
/// </summary>
public class FeedRecommendationsResponse
{
    /// <summary>
    /// The type of feed (Papers, Videos, etc.).
    /// </summary>
    public ResourceType FeedType { get; set; }

    /// <summary>
    /// The date these recommendations are for.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The list of recommendations for this feed.
    /// </summary>
    public List<RecommendationResponse> Recommendations { get; set; } = new();
}

