using System.ComponentModel.DataAnnotations;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request model for refreshing an expired access token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

