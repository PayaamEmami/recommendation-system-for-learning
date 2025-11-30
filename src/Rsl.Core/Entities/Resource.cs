using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Abstract base class representing a learning resource.
/// All specific resource types (Paper, Video, etc.) inherit from this.
/// </summary>
public abstract class Resource
{
    public Guid Id { get; set; }

    /// <summary>
    /// The title of the resource.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A description or summary of the resource content.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the resource can be accessed.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// When this resource was published or created.
    /// </summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// The source or platform (e.g., "arXiv", "YouTube", "Medium").
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// When this resource was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this resource was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The type of resource (discriminator for inheritance).
    /// </summary>
    public abstract ResourceType Type { get; protected set; }

    // Navigation properties

    /// <summary>
    /// Topics/categories this resource belongs to.
    /// </summary>
    public List<Topic> Topics { get; set; } = new();

    /// <summary>
    /// User votes (upvotes/downvotes) on this resource.
    /// </summary>
    public List<ResourceVote> Votes { get; set; } = new();

    /// <summary>
    /// Recommendations that include this resource.
    /// </summary>
    public List<Recommendation> Recommendations { get; set; } = new();
}

