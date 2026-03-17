using System.ComponentModel.DataAnnotations;

namespace Crs.Api.DTOs.Content.Requests;

/// <summary>
/// Request model for updating an existing content.
/// </summary>
public class UpdateContentRequest
{
    /// <summary>
    /// The title of the content.
    /// </summary>
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// A description of the content.
    /// </summary>
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The URL where the content can be accessed.
    /// </summary>
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(2000, ErrorMessage = "URL cannot exceed 2000 characters")]
    public string? Url { get; set; }

    /// <summary>
    /// Optional source ID if this content was ingested from a configured source.
    /// </summary>
    public Guid? SourceId { get; set; }
}

