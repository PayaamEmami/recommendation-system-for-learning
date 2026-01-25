using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Votes.Responses;

/// <summary>
/// Response model for vote information.
/// </summary>
public class VoteResponse
{
    /// <summary>
    /// The vote's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user's ID who cast the vote.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The resource's ID that was voted on.
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// The type of vote.
    /// </summary>
    public VoteType VoteType { get; set; }

    /// <summary>
    /// When the vote was cast.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the vote was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

