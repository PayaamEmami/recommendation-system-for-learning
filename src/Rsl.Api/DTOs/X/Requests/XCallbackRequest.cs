namespace Rsl.Api.DTOs.X.Requests;

/// <summary>
/// Request payload for X OAuth callback.
/// </summary>
public class XCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

