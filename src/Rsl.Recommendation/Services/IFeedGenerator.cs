using Rsl.Core.Enums;

namespace Rsl.Recommendation.Services;

/// <summary>
/// Service for generating daily recommendation feeds.
/// </summary>
public interface IFeedGenerator
{
    /// <summary>
    /// Generate and persist recommendations for a specific feed type and date.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="feedType">Type of feed to generate</param>
    /// <param name="date">Date to generate recommendations for</param>
    /// <param name="count">Number of recommendations to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated recommendations</returns>
    Task<List<Core.Entities.Recommendation>> GenerateFeedAsync(
        Guid userId,
        ResourceType feedType,
        DateOnly date,
        int count = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate recommendations for all feed types for a given date.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="date">Date to generate recommendations for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All generated recommendations across all feeds</returns>
    Task<List<Core.Entities.Recommendation>> GenerateAllFeedsAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}

