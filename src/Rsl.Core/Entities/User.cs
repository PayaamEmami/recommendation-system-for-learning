namespace Rsl.Core.Entities;

/// <summary>
/// Represents a user of the recommendation system.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user's email address (used for login/identification).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The user's hashed password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last logged in or accessed the system.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    /// <summary>
    /// URL-based sources configured by the user.
    /// </summary>
    public List<Source> Sources { get; set; } = new();

    /// <summary>
    /// Votes the user has cast on resources.
    /// </summary>
    public List<ResourceVote> Votes { get; set; } = new();

    /// <summary>
    /// Recommendations generated for this user.
    /// </summary>
    public List<Recommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Connected X accounts for this user (usually one).
    /// </summary>
    public List<XConnection> XConnections { get; set; } = new();

    /// <summary>
    /// X accounts the user follows.
    /// </summary>
    public List<XFollowedAccount> XFollowedAccounts { get; set; } = new();

    /// <summary>
    /// X accounts selected by the user for their feed.
    /// </summary>
    public List<XSelectedAccount> XSelectedAccounts { get; set; } = new();

    /// <summary>
    /// Posts ingested for the user's X feed.
    /// </summary>
    public List<XPost> XPosts { get; set; } = new();
}

