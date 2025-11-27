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
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last logged in or accessed the system.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Topics the user is interested in.
    /// </summary>
    public List<Topic> InterestedTopics { get; set; } = new();

    /// <summary>
    /// Votes the user has cast on resources.
    /// </summary>
    public List<ResourceVote> Votes { get; set; } = new();

    /// <summary>
    /// Recommendations generated for this user.
    /// </summary>
    public List<Recommendation> Recommendations { get; set; } = new();
}

