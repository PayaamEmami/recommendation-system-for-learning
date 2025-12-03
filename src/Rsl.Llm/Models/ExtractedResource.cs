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

    /// <summary>
    /// Optional: When the resource was published.
    /// </summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// Optional: Author(s) of the resource (for papers and blog posts).
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Optional: Channel or creator (for videos).
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Optional: Duration of video content.
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Optional: Thumbnail URL for videos.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Optional: DOI for academic papers.
    /// </summary>
    public string? DOI { get; set; }

    /// <summary>
    /// Optional: Journal or conference for papers.
    /// </summary>
    public string? Journal { get; set; }
}

