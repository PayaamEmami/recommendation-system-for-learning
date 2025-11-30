using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents a social media post (e.g., Twitter/X thread, LinkedIn post).
/// </summary>
public class SocialMediaPost : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.SocialMediaPost;

    /// <summary>
    /// The platform where the post was published (e.g., "Twitter", "LinkedIn").
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// The username or handle of the person who posted.
    /// </summary>
    public string? Username { get; set; }
}

