using System.ComponentModel.DataAnnotations;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request model for updating an existing resource.
/// </summary>
public class UpdateResourceRequest
{
    /// <summary>
    /// The title of the resource.
    /// </summary>
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// A description of the resource.
    /// </summary>
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the resource can be accessed.
    /// </summary>
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(2000, ErrorMessage = "URL cannot exceed 2000 characters")]
    public string? Url { get; set; }

    /// <summary>
    /// When the resource was published.
    /// </summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// Optional source ID if this resource was ingested from a configured source.
    /// </summary>
    public Guid? SourceId { get; set; }
}

