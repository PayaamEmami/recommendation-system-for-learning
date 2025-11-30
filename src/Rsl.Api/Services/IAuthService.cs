using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;

namespace Rsl.Api.Services;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and generates JWT tokens.
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using a refresh token.
    /// </summary>
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token and returns the user ID if valid.
    /// </summary>
    Task<Guid?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}

