namespace Rsl.Core.Entities;

/// <summary>
/// Represents a followed X account selected by the user for their feed.
/// </summary>
public class XSelectedAccount
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid XFollowedAccountId { get; set; }
    public XFollowedAccount FollowedAccount { get; set; } = null!;

    public DateTime SelectedAt { get; set; }

    public List<XPost> Posts { get; set; } = new();
}
