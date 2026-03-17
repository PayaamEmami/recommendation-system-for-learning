using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// Represents an academic paper or research publication.
/// </summary>
public class Paper : Content
{
    public override ContentType Type { get; protected set; } = ContentType.Paper;
}

