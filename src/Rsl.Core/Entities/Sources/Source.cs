using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents a URL-based source from which resources are ingested.
/// Examples: RSS feeds, YouTube channels, blogs, newsletters.
/// </summary>
public class Source
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns/configured this source.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user who owns this source.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The name of the source (e.g., "ArXiv AI Papers", "3Blue1Brown").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the source (RSS feed, channel URL, etc.).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this source provides.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The category/type of content this source provides.
    /// </summary>
    public ResourceType Category { get; set; }

    /// <summary>
    /// Whether this source is currently active for ingestion.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this source was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this source was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Resources that have been ingested from this source.
    /// </summary>
    public List<Resource> Resources { get; set; } = new();
}

