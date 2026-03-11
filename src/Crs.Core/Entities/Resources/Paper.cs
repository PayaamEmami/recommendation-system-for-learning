using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents an academic paper or research publication.
/// </summary>
public class Paper : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.Paper;
}

