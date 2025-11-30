using System.ComponentModel.DataAnnotations;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request model for updating a user's interested topics.
/// </summary>
public class UpdateUserTopicsRequest
{
    /// <summary>
    /// List of topic IDs the user is interested in.
    /// </summary>
    [Required(ErrorMessage = "Topic IDs are required")]
    public List<Guid> TopicIds { get; set; } = new();
}

