using Rsl.Core.Enums;

namespace Rsl.Core.Models;

/// <summary>
/// Document representation of a resource for vector indexing.
/// Contains the resource data along with its embedding vector.
/// </summary>
public class ResourceDocument
{
    /// <summary>
    /// Resource ID.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Resource title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Resource description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Resource URL.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Resource type.
    /// </summary>
    public required ResourceType Type { get; set; }

    /// <summary>
    /// Source ID (optional).
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Created date.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated date.
    /// </summary>
    public required DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Embedding vector for the resource.
    /// </summary>
    public required float[] Embedding { get; set; }

    /// <summary>
    /// Searchable text representation (combination of title and description).
    /// </summary>
    public string SearchableText => $"{Title} {Description}".Trim();
}
