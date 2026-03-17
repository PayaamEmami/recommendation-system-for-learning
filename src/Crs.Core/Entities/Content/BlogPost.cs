using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents a blog post or article.
/// </summary>
public class BlogPost : Content
{
    public override ContentType Type { get; protected set; } = ContentType.BlogPost;
}

