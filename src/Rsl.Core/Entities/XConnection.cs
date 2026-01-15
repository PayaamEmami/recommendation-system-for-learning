namespace Rsl.Core.Entities;

/// <summary>
/// Represents a user's connected X account and stored tokens.
/// </summary>
public class XConnection
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// X user id for the connected account.
    /// </summary>
    public string XUserId { get; set; } = string.Empty;

    /// <summary>
    /// X handle (without @).
    /// </summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the X account.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Encrypted access token.
    /// </summary>
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted refresh token.
    /// </summary>
    public string RefreshTokenEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// OAuth scopes granted for this connection.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// When the connection was created.
    /// </summary>
    public DateTime ConnectedAt { get; set; }

    /// <summary>
    /// When the connection was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
