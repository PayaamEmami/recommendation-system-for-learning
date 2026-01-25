using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents a user's vote (upvote or downvote) on a resource.
/// Users can change their vote, so each user-resource pair has at most one vote record.
/// </summary>
public class ResourceVote
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Whether this is an upvote or downvote.
    /// </summary>
    public VoteType VoteType { get; set; }

    /// <summary>
    /// When the vote was originally cast.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the vote was last modified (if the user changed their vote).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Resource Resource { get; set; } = null!;
}

