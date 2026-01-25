namespace Rsl.Api.DTOs.X.Responses;

/// <summary>
/// Response payload for X posts.
/// </summary>
public class XPostResponse
{
    public Guid Id { get; set; }
    public string PostId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime PostCreatedAt { get; set; }
    public string AuthorHandle { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? AuthorProfileImageUrl { get; set; }
    public string? MediaJson { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int QuoteCount { get; set; }
}

