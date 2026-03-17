using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Abstract base class representing a learning content item.
/// All specific content types (Paper, Video, etc.) inherit from this.
/// </summary>
public abstract class Content
{
    public Guid Id { get; set; }

    /// <summary>
    /// The title of the content.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A description or summary of the content.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the content can be accessed.
    /// </summary>
    public string Url { get; set; } = string.Empty;


    /// <summary>
    /// When this content was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this content was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The type of content (discriminator for inheritance).
    /// </summary>
    public abstract ContentType Type { get; protected set; }

    /// <summary>
    /// The ID of the source this content was ingested from (optional - can be null for manually added content).
    /// </summary>
    public Guid? SourceId { get; set; }

    // Navigation properties

    /// <summary>
    /// The source this content was ingested from.
    /// </summary>
    public Source? Source { get; set; }

    /// <summary>
    /// User votes (upvotes/downvotes) on this content.
    /// </summary>
    public List<ContentVote> Votes { get; set; } = new();

    /// <summary>
    /// Recommendations that include this content.
    /// </summary>
    public List<Recommendation> Recommendations { get; set; } = new();
}
