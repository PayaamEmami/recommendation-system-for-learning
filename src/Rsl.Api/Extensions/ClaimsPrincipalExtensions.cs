using System.Security.Claims;

namespace Rsl.Api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the claims principal.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the user's email from the claims principal.
    /// </summary>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value;
    }
}

