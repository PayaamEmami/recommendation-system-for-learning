using Crs.Core.Enums;

namespace Crs.Core.Entities;

/// <summary>
/// User-supplied content feedback that should influence preferences
/// without becoming part of the recommendable content catalog.
/// </summary>
public class ManualContentFeedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public ContentType? ContentType { get; set; }
    public VoteType VoteType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
