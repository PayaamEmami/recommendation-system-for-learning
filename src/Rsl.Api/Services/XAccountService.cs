using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Configuration;

namespace Rsl.Api.Services;

/// <summary>
/// Service for handling X account connections and feed data.
/// </summary>
public class XAccountService : IXAccountService
{
    private static readonly ConcurrentDictionary<string, XAuthState> AuthStates = new();

    private readonly IXConnectionRepository _connectionRepository;
    private readonly IXFollowedAccountRepository _followedAccountRepository;
    private readonly IXSelectedAccountRepository _selectedAccountRepository;
    private readonly IXPostRepository _postRepository;
    private readonly IXApiClient _xApiClient;
    private readonly XApiSettings _settings;
    private readonly IDataProtector _protector;
    private readonly ILogger<XAccountService> _logger;

    public XAccountService(
        IXConnectionRepository connectionRepository,
        IXFollowedAccountRepository followedAccountRepository,
        IXSelectedAccountRepository selectedAccountRepository,
        IXPostRepository postRepository,
        IXApiClient xApiClient,
        IOptions<XApiSettings> settings,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<XAccountService> logger)
    {
        _connectionRepository = connectionRepository;
        _followedAccountRepository = followedAccountRepository;
        _selectedAccountRepository = selectedAccountRepository;
        _postRepository = postRepository;
        _xApiClient = xApiClient;
        _settings = settings.Value;
        _protector = dataProtectionProvider.CreateProtector("Rsl.X.Tokens");
        _logger = logger;
    }

    public Task<string> CreateConnectUrlAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        CleanupExpiredStates();

        var state = CreateBase64Url(32);
        var codeVerifier = CreateBase64Url(64);
        var codeChallenge = CreateCodeChallenge(codeVerifier);

        AuthStates[state] = new XAuthState
        {
            UserId = userId,
            CodeVerifier = codeVerifier,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["scope"] = _settings.Scopes,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var url = $"{_settings.AuthorizationUrl}?{ToQueryString(query)}";
        return Task.FromResult(url);
    }

    public async Task HandleCallbackAsync(Guid userId, string code, string state, CancellationToken cancellationToken = default)
    {
        CleanupExpiredStates();

        if (!AuthStates.TryRemove(state, out var authState) || authState.UserId != userId)
        {
            throw new InvalidOperationException("Invalid or expired X authorization state");
        }

        var token = await _xApiClient.ExchangeCodeAsync(code, authState.CodeVerifier, _settings.RedirectUri, cancellationToken);
        var profile = await _xApiClient.GetCurrentUserAsync(token.AccessToken, cancellationToken);

        var connection = new XConnection
        {
            UserId = userId,
            XUserId = profile.XUserId,
            Handle = profile.Handle,
            DisplayName = profile.DisplayName,
            AccessTokenEncrypted = Protect(token.AccessToken),
            RefreshTokenEncrypted = Protect(token.RefreshToken),
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn),
            Scopes = token.Scope
        };

        await _connectionRepository.UpsertAsync(connection, cancellationToken);
        _logger.LogInformation("Connected X account {Handle} for user {UserId}", profile.Handle, userId);
        await RefreshFollowedAccountsAsync(userId, cancellationToken);
    }

    public Task<List<XFollowedAccount>> GetFollowedAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _followedAccountRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public Task<List<XSelectedAccount>> GetSelectedAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _selectedAccountRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<XFollowedAccount>> RefreshFollowedAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await GetValidConnectionAsync(userId, cancellationToken);
        var accounts = await _xApiClient.GetFollowedAccountsAsync(connection.AccessToken, connection.XUserId, cancellationToken);

        _logger.LogInformation("Fetched {Count} followed X accounts for user {UserId}", accounts.Count, userId);

        var entities = accounts.Select(account => new XFollowedAccount
        {
            UserId = userId,
            XUserId = account.XUserId,
            Handle = account.Handle,
            DisplayName = account.DisplayName,
            ProfileImageUrl = account.ProfileImageUrl,
            FollowedAt = DateTime.UtcNow
        }).ToList();

        await _followedAccountRepository.ReplaceForUserAsync(userId, entities, cancellationToken);
        return await _followedAccountRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<XSelectedAccount>> UpdateSelectedAccountsAsync(Guid userId, List<Guid> followedAccountIds, CancellationToken cancellationToken = default)
    {
        var selected = followedAccountIds.Select(id => new XSelectedAccount
        {
            XFollowedAccountId = id,
            SelectedAt = DateTime.UtcNow
        }).ToList();

        await _selectedAccountRepository.ReplaceForUserAsync(userId, selected, cancellationToken);
        return await _selectedAccountRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public Task<List<XPost>> GetPostsAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        return _postRepository.GetLatestByUserIdAsync(userId, limit, cancellationToken);
    }

    private async Task<ValidConnection> GetValidConnectionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (connection == null)
        {
            throw new InvalidOperationException("X account is not connected");
        }

        var accessToken = Unprotect(connection.AccessTokenEncrypted);
        var refreshToken = Unprotect(connection.RefreshTokenEncrypted);

        if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(1))
        {
            var refreshed = await _xApiClient.RefreshTokenAsync(refreshToken, cancellationToken);
            accessToken = refreshed.AccessToken;
            refreshToken = refreshed.RefreshToken;

            connection.AccessTokenEncrypted = Protect(accessToken);
            connection.RefreshTokenEncrypted = Protect(refreshToken);
            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
            connection.Scopes = refreshed.Scope;

            await _connectionRepository.UpsertAsync(connection, cancellationToken);
        }

        return new ValidConnection
        {
            XUserId = connection.XUserId,
            AccessToken = accessToken
        };
    }

    private string Protect(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : _protector.Protect(value);
    }

    private string Unprotect(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : _protector.Unprotect(value);
    }

    private static string CreateBase64Url(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }

    private static string ToQueryString(Dictionary<string, string> parameters)
    {
        return string.Join("&", parameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }

    private static void CleanupExpiredStates()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in AuthStates)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                AuthStates.TryRemove(entry.Key, out _);
            }
        }
    }

    private class XAuthState
    {
        public Guid UserId { get; set; }
        public string CodeVerifier { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class ValidConnection
    {
        public string XUserId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }
}
