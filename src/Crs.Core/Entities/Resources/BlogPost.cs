using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents a blog post or article.
/// </summary>
public class BlogPost : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.BlogPost;
}

