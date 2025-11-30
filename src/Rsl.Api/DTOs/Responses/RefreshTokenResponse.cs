namespace Rsl.Api.DTOs.Responses;

/// <summary>
/// Response model for refresh token operation.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// The new JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The new refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the new access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

