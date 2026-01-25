namespace Rsl.Core.Models;

/// <summary>
/// Token response from X OAuth.
/// </summary>
public class XTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
}

/// <summary>
/// Basic X user profile.
/// </summary>
public class XUserProfile
{
    public string XUserId { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Represents a followed X account.
/// </summary>
public class XFollowedAccountInfo
{
    public string XUserId { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Media info attached to a post.
/// </summary>
public class XMediaInfo
{
    public string Type { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? PreviewImageUrl { get; set; }
}

/// <summary>
/// Post information from X API.
/// </summary>
public class XPostInfo
{
    public string PostId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public XUserProfile Author { get; set; } = new();
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int QuoteCount { get; set; }
    public List<XMediaInfo> Media { get; set; } = new();
}

