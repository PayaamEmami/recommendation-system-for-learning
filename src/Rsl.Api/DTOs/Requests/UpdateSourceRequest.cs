using System.ComponentModel.DataAnnotations;
using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing source.
/// </summary>
public class UpdateSourceRequest
{
    /// <summary>
    /// The name of the source.
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// The URL of the source.
    /// </summary>
    [Url]
    [StringLength(2000, MinimumLength = 1)]
    public string? Url { get; set; }

    /// <summary>
    /// Optional description of the source.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The category/type of content this source provides.
    /// </summary>
    public ResourceType? Category { get; set; }

    /// <summary>
    /// Whether this source is active.
    /// </summary>
    public bool? IsActive { get; set; }
}

