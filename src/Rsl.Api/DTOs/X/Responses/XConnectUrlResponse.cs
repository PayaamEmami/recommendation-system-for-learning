namespace Rsl.Api.DTOs.X.Responses;

/// <summary>
/// Response payload for X OAuth connect URL.
/// </summary>
public class XConnectUrlResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
}

