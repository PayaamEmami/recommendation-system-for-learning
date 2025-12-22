using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents an educational video resource.
/// </summary>
public class Video : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.Video;
}

