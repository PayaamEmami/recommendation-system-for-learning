using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents a blog post or article.
/// </summary>
public class BlogPost : Resource
{
    public override ResourceType Type => ResourceType.BlogPost;

    /// <summary>
    /// The author(s) of the blog post.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The blog or website where the post was published.
    /// </summary>
    public string? Blog { get; set; }

    /// <summary>
    /// Estimated reading time in minutes.
    /// </summary>
    public int? ReadingTimeMinutes { get; set; }
}

