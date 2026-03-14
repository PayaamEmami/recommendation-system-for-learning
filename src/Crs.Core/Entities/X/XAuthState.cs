namespace Crs.Core.Entities;

/// <summary>
/// Ephemeral OAuth state for X connect flow. Stored in DB so multi-instance deployments can look it up.
/// </summary>
public class XAuthState
{
    /// <summary>
    /// OAuth state parameter (PK).
    /// </summary>
    public string State { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    /// <summary>
    /// PKCE code verifier for token exchange.
    /// </summary>
    public string CodeVerifier { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URI used in the authorization request.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// When this state expires (typically 10 minutes).
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
