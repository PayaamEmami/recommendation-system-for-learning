using System.ComponentModel.DataAnnotations;
using Crs.Core.Enums;

namespace Crs.Api.DTOs.Preferences.Requests;

public class CreateManualContentFeedbackRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(2000, ErrorMessage = "URL cannot exceed 2000 characters")]
    public string? Url { get; set; }

    public ContentType? ContentType { get; set; }

    [Required(ErrorMessage = "Vote type is required")]
    public VoteType VoteType { get; set; }
}
