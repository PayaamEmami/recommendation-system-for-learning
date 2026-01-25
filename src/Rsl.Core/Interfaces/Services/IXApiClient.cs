using Rsl.Core.Models;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Client interface for X API operations.
/// </summary>
public interface IXApiClient
{
    Task<XTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken = default);
    Task<XTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<XUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<List<XFollowedAccountInfo>> GetFollowedAccountsAsync(string accessToken, string userId, CancellationToken cancellationToken = default);
    Task<List<XPostInfo>> GetRecentPostsAsync(string accessToken, string userId, DateTime? since, CancellationToken cancellationToken = default);
}

