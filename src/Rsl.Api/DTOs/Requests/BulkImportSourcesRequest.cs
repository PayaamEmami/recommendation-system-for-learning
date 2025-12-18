using System.ComponentModel.DataAnnotations;
using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request DTO for bulk importing multiple sources.
/// </summary>
public class BulkImportSourcesRequest
{
    /// <summary>
    /// List of sources to import.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<BulkImportSourceItem> Sources { get; set; } = new();
}

/// <summary>
/// Represents a single source to import.
/// </summary>
public class BulkImportSourceItem
{
    /// <summary>
    /// The name of the source.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the source.
    /// </summary>
    [Required]
    [Url]
    [StringLength(2000, MinimumLength = 1)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The category/type of content this source provides.
    /// </summary>
    [Required]
    public ResourceType Category { get; set; }

    /// <summary>
    /// Optional description of the source.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }
}

