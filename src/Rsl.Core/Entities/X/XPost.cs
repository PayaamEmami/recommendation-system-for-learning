namespace Rsl.Core.Entities;

/// <summary>
/// Represents a post ingested for a user's X feed.
/// </summary>
public class XPost
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid XSelectedAccountId { get; set; }
    public XSelectedAccount SelectedAccount { get; set; } = null!;

    /// <summary>
    /// Post id from X.
    /// </summary>
    public string PostId { get; set; } = string.Empty;

    /// <summary>
    /// Post text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Post URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// When the post was created on X.
    /// </summary>
    public DateTime PostCreatedAt { get; set; }

    /// <summary>
    /// Author's X user id.
    /// </summary>
    public string AuthorXUserId { get; set; } = string.Empty;

    /// <summary>
    /// Author handle (without @).
    /// </summary>
    public string AuthorHandle { get; set; } = string.Empty;

    /// <summary>
    /// Author display name.
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Author profile image URL.
    /// </summary>
    public string? AuthorProfileImageUrl { get; set; }

    /// <summary>
    /// Optional JSON string containing media URLs/types.
    /// </summary>
    public string? MediaJson { get; set; }

    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int QuoteCount { get; set; }

    public DateTime IngestedAt { get; set; }
}

