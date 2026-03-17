using System.ComponentModel.DataAnnotations;
using Crs.Core.Enums;

namespace Crs.Api.DTOs.Sources.Requests;

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
    public ContentType? Category { get; set; }

    /// <summary>
    /// Whether this source is active.
    /// </summary>
    public bool? IsActive { get; set; }
}

