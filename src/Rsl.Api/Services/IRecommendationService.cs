using Rsl.Api.DTOs.Responses;
using Rsl.Core.Enums;

namespace Rsl.Api.Services;

/// <summary>
/// Service interface for recommendation operations.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets recommendations for a specific feed type and date.
    /// </summary>
    Task<FeedRecommendationsResponse> GetFeedRecommendationsAsync(
        Guid userId,
        ResourceType feedType,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's recommendations across all feed types.
    /// </summary>
    Task<List<FeedRecommendationsResponse>> GetTodaysRecommendationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

