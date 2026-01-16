using Rsl.Core.Entities;

namespace Rsl.Api.Services;

/// <summary>
/// Service for connecting and managing X accounts.
/// </summary>
public interface IXAccountService
{
    Task<string> CreateConnectUrlAsync(Guid userId, string? redirectUri = null, CancellationToken cancellationToken = default);
    Task HandleCallbackAsync(Guid userId, string code, string state, CancellationToken cancellationToken = default);
    Task<List<XFollowedAccount>> GetFollowedAccountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<XSelectedAccount>> GetSelectedAccountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<XFollowedAccount>> RefreshFollowedAccountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<XSelectedAccount>> UpdateSelectedAccountsAsync(Guid userId, List<Guid> followedAccountIds, CancellationToken cancellationToken = default);
    Task<List<XPost>> GetPostsAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
}
