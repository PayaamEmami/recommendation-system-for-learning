using Rsl.Api.DTOs.Sources.Responses;
using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Resources.Responses;

/// <summary>
/// Response model for resource information.
/// </summary>
public class ResourceResponse
{
    /// <summary>
    /// The resource's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The title of the resource.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A description of the resource.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the resource can be accessed.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The type of resource.
    /// </summary>
    public ResourceType Type { get; set; }

    /// <summary>
    /// When the resource was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the resource was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The source this resource was ingested from (if any).
    /// </summary>
    public SourceResponse? SourceInfo { get; set; }
}

