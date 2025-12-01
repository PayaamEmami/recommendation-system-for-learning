using System.ComponentModel.DataAnnotations;
using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request model for creating a new resource.
/// </summary>
public class CreateResourceRequest
{
    /// <summary>
    /// The title of the resource.
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A description of the resource.
    /// </summary>
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the resource can be accessed.
    /// </summary>
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(2000, ErrorMessage = "URL cannot exceed 2000 characters")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// When the resource was published.
    /// </summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// Optional source ID if this resource was ingested from a configured source.
    /// If provided, the resource will be linked to the configured source.
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// The type of resource.
    /// </summary>
    [Required(ErrorMessage = "Resource type is required")]
    public ResourceType ResourceType { get; set; }
}

