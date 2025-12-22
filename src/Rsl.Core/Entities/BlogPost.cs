using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents a blog post or article.
/// </summary>
public class BlogPost : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.BlogPost;
}

