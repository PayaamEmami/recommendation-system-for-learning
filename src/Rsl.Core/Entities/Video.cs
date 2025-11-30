using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents an educational video resource.
/// </summary>
public class Video : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.Video;

    /// <summary>
    /// The duration of the video.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// The channel or creator who published the video.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Thumbnail image URL for the video.
    /// </summary>
    public string? ThumbnailUrl { get; set; }
}

