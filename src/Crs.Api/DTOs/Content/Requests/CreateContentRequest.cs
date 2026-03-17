using System.ComponentModel.DataAnnotations;
using Crs.Core.Enums;

namespace Crs.Api.DTOs.Content.Requests;

/// <summary>
/// Request model for creating a new content item.
/// </summary>
public class CreateContentRequest
{
    /// <summary>
    /// The title of the content.
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A description of the content.
    /// </summary>
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the content can be accessed.
    /// </summary>
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(2000, ErrorMessage = "URL cannot exceed 2000 characters")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional source ID if this content was ingested from a configured source.
    /// If provided, the content will be linked to the configured source.
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// The type of content.
    /// </summary>
    [Required(ErrorMessage = "Content type is required")]
    public ContentType ContentType { get; set; }
}
