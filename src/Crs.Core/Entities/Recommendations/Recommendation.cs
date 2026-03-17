using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents a single recommendation made to a user for a specific date and feed type.
/// Recommendations are generated daily (at midnight) and stored for historical tracking.
/// Each feed type generates 3-7 recommendations per day.
/// </summary>
public class Recommendation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid ContentId { get; set; }

    /// <summary>
    /// The feed type this recommendation belongs to (Papers, Videos, etc.).
    /// </summary>
    public ContentType FeedType { get; set; }

    /// <summary>
    /// The date this recommendation was generated for (just the date, no time).
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The position/rank of this recommendation in the feed (1 = top).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Optional confidence score or relevance score from the recommendation engine.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// When this recommendation was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Content Content { get; set; } = null!;
}

