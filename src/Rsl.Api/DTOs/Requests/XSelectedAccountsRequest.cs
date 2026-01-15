namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request payload for selecting followed X accounts.
/// </summary>
public class XSelectedAccountsRequest
{
    public List<Guid> FollowedAccountIds { get; set; } = new();
}
