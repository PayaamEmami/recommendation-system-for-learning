using Rsl.Recommendation.Models;

namespace Rsl.Recommendation.Services;

/// <summary>
/// Service for building and managing user interest profiles.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Build a user's interest profile based on their interaction history.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User interest profile</returns>
    Task<UserInterestProfile> BuildProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

