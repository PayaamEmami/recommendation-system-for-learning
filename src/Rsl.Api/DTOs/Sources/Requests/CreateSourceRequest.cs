using System.ComponentModel.DataAnnotations;
using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Sources.Requests;

/// <summary>
/// Request DTO for creating a new source.
/// </summary>
public class CreateSourceRequest
{
    /// <summary>
    /// The name of the source.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the source (RSS feed, YouTube channel, etc.).
    /// </summary>
    [Required]
    [Url]
    [StringLength(2000, MinimumLength = 1)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the source.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The category/type of content this source provides.
    /// </summary>
    [Required]
    public ResourceType Category { get; set; }

    /// <summary>
    /// Whether this source is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

