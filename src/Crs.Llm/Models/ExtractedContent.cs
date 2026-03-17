using Crs.Core.Enums;

namespace Crs.Llm.Models;

/// <summary>
/// Represents a learning content extracted by the LLM agent from a source URL.
/// </summary>
public class ExtractedContent
{
    /// <summary>
    /// The title of the content.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL where the content can be accessed.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// A brief description or summary of the content.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The type/category of content (Paper, Video, BlogPost, etc.).
    /// </summary>
    public ContentType Type { get; set; }
}

