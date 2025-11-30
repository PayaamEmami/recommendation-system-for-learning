namespace Rsl.Api.Configuration;

/// <summary>
/// JWT authentication configuration settings.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The secret key used to sign and validate JWT tokens.
    /// Must be at least 32 characters for HS256 algorithm.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The issuer claim (who created and signed the token).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The audience claim (who the token is intended for).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// How long access tokens are valid (in minutes).
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// How long refresh tokens are valid (in days).
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

