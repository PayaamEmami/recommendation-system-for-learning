namespace Rsl.Core.Entities;

/// <summary>
/// Represents a subject area or category (e.g., "Machine Learning", "Mathematics").
/// Topics are database-driven and can be added/modified without code changes.
/// </summary>
public class Topic
{
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the topic (e.g., "Artificial Intelligence").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A URL-friendly slug for the topic (e.g., "artificial-intelligence").
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this topic covers.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this topic was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Users who are interested in this topic.
    /// </summary>
    public List<User> InterestedUsers { get; set; } = new();

    /// <summary>
    /// Resources tagged with this topic.
    /// </summary>
    public List<Resource> Resources { get; set; } = new();
}

