using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Sources.Responses;

/// <summary>
/// Response DTO for source data.
/// </summary>
public class SourceResponse
{
    /// <summary>
    /// The unique identifier of the source.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user ID who owns this source.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The name of the source.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the source.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the source.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The category/type of content this source provides.
    /// </summary>
    public ResourceType Category { get; set; }

    /// <summary>
    /// Whether this source is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the source was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the source was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Number of resources ingested from this source.
    /// </summary>
    public int ResourceCount { get; set; }
}

