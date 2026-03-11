using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents an educational video resource.
/// </summary>
public class Video : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.Video;
}

