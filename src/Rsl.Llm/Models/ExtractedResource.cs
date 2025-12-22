using Rsl.Core.Enums;

namespace Rsl.Llm.Models;

/// <summary>
/// Represents a learning resource extracted by the LLM agent from a source URL.
/// </summary>
public class ExtractedResource
{
    /// <summary>
    /// The title of the resource.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL where the resource can be accessed.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// A brief description or summary of the resource.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The type/category of resource (Paper, Video, BlogPost, etc.).
    /// </summary>
    public ResourceType Type { get; set; }
}

