using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents an educational video content.
/// </summary>
public class Video : Content
{
    public override ContentType Type { get; protected set; } = ContentType.Video;
}

