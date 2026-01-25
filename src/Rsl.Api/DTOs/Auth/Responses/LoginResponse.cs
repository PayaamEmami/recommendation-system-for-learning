using Rsl.Api.DTOs.Users.Responses;

namespace Rsl.Api.DTOs.Auth.Responses;

/// <summary>
/// Response model for successful login or registration.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// The authenticated user's information.
    /// </summary>
    public UserResponse User { get; set; } = null!;
}

