using Rsl.Core.Enums;

namespace Rsl.Core.Entities;

/// <summary>
/// Represents a current event or news article related to learning topics.
/// </summary>
public class CurrentEvent : Resource
{
    public override ResourceType Type => ResourceType.CurrentEvent;

    /// <summary>
    /// The news outlet or publication.
    /// </summary>
    public string? NewsOutlet { get; set; }

    /// <summary>
    /// The author or journalist who wrote the article.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Geographical region or location related to the event.
    /// </summary>
    public string? Region { get; set; }
}

