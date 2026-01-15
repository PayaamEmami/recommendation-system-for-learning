namespace Rsl.Api.DTOs.Responses;

/// <summary>
/// Response payload for followed X accounts.
/// </summary>
public class XFollowedAccountResponse
{
    public Guid Id { get; set; }
    public string XUserId { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsSelected { get; set; }
}
