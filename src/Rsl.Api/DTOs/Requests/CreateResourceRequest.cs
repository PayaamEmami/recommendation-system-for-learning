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
    /// The source or platform (e.g., "arXiv", "YouTube").
    /// </summary>
    [StringLength(200, ErrorMessage = "Source cannot exceed 200 characters")]
    public string? Source { get; set; }

    /// <summary>
    /// List of topic IDs this resource belongs to.
    /// </summary>
    [Required(ErrorMessage = "At least one topic is required")]
    [MinLength(1, ErrorMessage = "At least one topic is required")]
    public List<Guid> TopicIds { get; set; } = new();

    /// <summary>
    /// The type of resource.
    /// </summary>
    [Required(ErrorMessage = "Resource type is required")]
    public ResourceType ResourceType { get; set; }
}

