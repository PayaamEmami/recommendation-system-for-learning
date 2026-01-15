using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Rsl.Web.Services;

/// <summary>
/// Service for connecting X accounts and fetching X feed data.
/// </summary>
public class XFeedService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly ILogger<XFeedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public XFeedService(HttpClient httpClient, AuthService authService, ILogger<XFeedService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(_authService.CurrentState.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authService.CurrentState.AccessToken);
        }
    }

    public async Task<string?> GetConnectUrlAsync()
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return null;
            }

            SetAuthHeader();
            var response = await _httpClient.GetAsync("/api/v1/x/connect-url");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch X connect URL: {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<XConnectUrlResponse>(JsonOptions);
            return payload?.AuthorizationUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching X connect URL");
            return null;
        }
    }

    public async Task<bool> HandleCallbackAsync(string code, string state)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return false;
            }

            SetAuthHeader();
            var request = new XCallbackRequest
            {
                Code = code,
                State = state
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/x/callback", request, JsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling X callback");
            return false;
        }
    }

    public async Task<List<XFollowedAccountItem>> GetFollowedAccountsAsync(bool refresh = false)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return new List<XFollowedAccountItem>();
            }

            SetAuthHeader();
            var response = await _httpClient.GetAsync($"/api/v1/x/followed-accounts?refresh={refresh.ToString().ToLowerInvariant()}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch followed X accounts: {StatusCode}", response.StatusCode);
                return new List<XFollowedAccountItem>();
            }

            var payload = await response.Content.ReadFromJsonAsync<List<XFollowedAccountResponse>>(JsonOptions);
            if (payload == null)
            {
                return new List<XFollowedAccountItem>();
            }

            return payload.Select(p => new XFollowedAccountItem
            {
                Id = p.Id,
                XUserId = p.XUserId,
                Handle = p.Handle,
                DisplayName = p.DisplayName,
                ProfileImageUrl = p.ProfileImageUrl,
                IsSelected = p.IsSelected
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching followed X accounts");
            return new List<XFollowedAccountItem>();
        }
    }

    public async Task<List<XFollowedAccountItem>> UpdateSelectedAccountsAsync(List<Guid> followedAccountIds)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return new List<XFollowedAccountItem>();
            }

            SetAuthHeader();
            var request = new XSelectedAccountsRequest
            {
                FollowedAccountIds = followedAccountIds
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/x/selected-accounts", request, JsonOptions);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update selected X accounts: {StatusCode}", response.StatusCode);
                return new List<XFollowedAccountItem>();
            }

            var payload = await response.Content.ReadFromJsonAsync<List<XFollowedAccountResponse>>(JsonOptions);
            if (payload == null)
            {
                return new List<XFollowedAccountItem>();
            }

            return payload.Select(p => new XFollowedAccountItem
            {
                Id = p.Id,
                XUserId = p.XUserId,
                Handle = p.Handle,
                DisplayName = p.DisplayName,
                ProfileImageUrl = p.ProfileImageUrl,
                IsSelected = p.IsSelected
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating selected X accounts");
            return new List<XFollowedAccountItem>();
        }
    }

    public async Task<List<XPostItem>> GetPostsAsync(int limit = 30)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return new List<XPostItem>();
            }

            SetAuthHeader();
            var response = await _httpClient.GetAsync($"/api/v1/x/posts?limit={limit}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch X posts: {StatusCode}", response.StatusCode);
                return new List<XPostItem>();
            }

            var payload = await response.Content.ReadFromJsonAsync<List<XPostResponse>>(JsonOptions);
            if (payload == null)
            {
                return new List<XPostItem>();
            }

            return payload.Select(p => new XPostItem
            {
                Id = p.Id,
                PostId = p.PostId,
                Text = p.Text,
                Url = p.Url,
                PostCreatedAt = p.PostCreatedAt,
                AuthorHandle = p.AuthorHandle,
                AuthorName = p.AuthorName,
                AuthorProfileImageUrl = p.AuthorProfileImageUrl,
                MediaJson = p.MediaJson,
                LikeCount = p.LikeCount,
                ReplyCount = p.ReplyCount,
                RepostCount = p.RepostCount,
                QuoteCount = p.QuoteCount
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching X posts");
            return new List<XPostItem>();
        }
    }
}

public class XConnectUrlResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
}

public class XCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class XSelectedAccountsRequest
{
    public List<Guid> FollowedAccountIds { get; set; } = new();
}

public class XFollowedAccountResponse
{
    public Guid Id { get; set; }
    public string XUserId { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsSelected { get; set; }
}

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

public class XFollowedAccountItem
{
    public Guid Id { get; set; }
    public string XUserId { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsSelected { get; set; }
}

public class XPostItem
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
