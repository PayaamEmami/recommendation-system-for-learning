using Crs.Core.Enums;

namespace Crs.Core.Models;

/// <summary>
/// Document representation of a piece of content for vector indexing.
/// Contains the content data along with its embedding vector.
/// </summary>
public class ContentDocument
{
    /// <summary>
    /// Content ID.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Content title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Content description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Content URL.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Content type.
    /// </summary>
    public required ContentType Type { get; set; }

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
    /// Published/discovered date (used for date-based filtering in vector search).
    /// Falls back to CreatedAt if no specific published date is available.
    /// </summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// Embedding vector for the content.
    /// </summary>
    public required float[] Embedding { get; set; }

    /// <summary>
    /// Searchable text representation (combination of title and description).
    /// </summary>
    public string SearchableText => $"{Title} {Description}".Trim();
}
