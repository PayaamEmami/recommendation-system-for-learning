namespace Rsl.Core.Entities;

/// <summary>
/// Represents an X account followed by a user.
/// </summary>
public class XFollowedAccount
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// X user id of the followed account.
    /// </summary>
    public string XUserId { get; set; } = string.Empty;

    /// <summary>
    /// Handle (without @) of the followed account.
    /// </summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the followed account.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Profile image URL of the followed account.
    /// </summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// When this account was last synced as followed.
    /// </summary>
    public DateTime FollowedAt { get; set; }

    public List<XSelectedAccount> SelectedAccounts { get; set; } = new();
}
