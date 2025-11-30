namespace Rsl.Api.DTOs.Responses;

/// <summary>
/// Response model for topic information.
/// </summary>
public class TopicResponse
{
    /// <summary>
    /// The topic's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the topic.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the topic.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the topic was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

