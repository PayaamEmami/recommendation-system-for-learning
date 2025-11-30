using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents an academic paper or research publication.
/// </summary>
public class Paper : Resource
{
    public override ResourceType Type { get; protected set; } = ResourceType.Paper;

    /// <summary>
    /// Digital Object Identifier (DOI) for the paper.
    /// </summary>
    public string? DOI { get; set; }

    /// <summary>
    /// The journal or conference where the paper was published.
    /// </summary>
    public string? Journal { get; set; }

    /// <summary>
    /// List of authors who wrote the paper.
    /// </summary>
    public List<string> Authors { get; set; } = new();

    /// <summary>
    /// The year the paper was published.
    /// </summary>
    public int? PublicationYear { get; set; }
}

