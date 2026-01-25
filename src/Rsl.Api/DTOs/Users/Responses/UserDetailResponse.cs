using Rsl.Api.DTOs.Sources.Responses;

namespace Rsl.Api.DTOs.Users.Responses;

/// <summary>
/// Detailed response model for user information including topics.
/// </summary>
public class UserDetailResponse
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// URL-based sources configured by the user.
    /// </summary>
    public List<SourceResponse> Sources { get; set; } = new();
}

