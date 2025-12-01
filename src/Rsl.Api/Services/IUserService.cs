using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;

namespace Rsl.Api.Services;

/// <summary>
/// Service interface for user-related operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<UserDetailResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile information.
    /// </summary>
    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
}

