using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rsl.Core.Interfaces;
using Microsoft.Extensions.Options;
using Rsl.Core.Models;
using Rsl.Infrastructure.Configuration;

namespace Rsl.Infrastructure.Services;

/// <summary>
/// HTTP client for X API operations.
/// </summary>
public class XApiClient : IXApiClient
{
    private readonly HttpClient _httpClient;
    private readonly XApiSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public XApiClient(HttpClient httpClient, IOptions<XApiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<XTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        AddClientAuthHeader(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"X API /2/users/me failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        var token = await response.Content.ReadFromJsonAsync<XTokenApiResponse>(JsonOptions, cancellationToken);
        return token == null
            ? new XTokenResponse()
            : new XTokenResponse
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                ExpiresIn = token.ExpiresIn,
                Scope = token.Scope,
                TokenType = token.TokenType
            };
    }

    public async Task<XTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["client_id"] = _settings.ClientId
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        AddClientAuthHeader(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<XTokenApiResponse>(JsonOptions, cancellationToken);
        return token == null
            ? new XTokenResponse()
            : new XTokenResponse
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                ExpiresIn = token.ExpiresIn,
                Scope = token.Scope,
                TokenType = token.TokenType
            };
    }

    public async Task<XUserProfile> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/2/users/me?user.fields=profile_image_url");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<XUserResponse>(JsonOptions, cancellationToken);
        if (payload?.Data == null)
        {
            return new XUserProfile();
        }

        return new XUserProfile
        {
            XUserId = payload.Data.Id,
            Handle = payload.Data.Username,
            DisplayName = payload.Data.Name,
            ProfileImageUrl = payload.Data.ProfileImageUrl
        };
    }

    public async Task<List<XFollowedAccountInfo>> GetFollowedAccountsAsync(string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        var results = new List<XFollowedAccountInfo>();
        string? nextToken = null;

        do
        {
            var url = $"{_settings.BaseUrl}/2/users/{userId}/following?max_results=1000&user.fields=profile_image_url";
            if (!string.IsNullOrEmpty(nextToken))
            {
                url += $"&pagination_token={Uri.EscapeDataString(nextToken)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<XFollowResponse>(JsonOptions, cancellationToken);
            if (payload?.Data != null)
            {
                results.AddRange(payload.Data.Select(item => new XFollowedAccountInfo
                {
                    XUserId = item.Id,
                    Handle = item.Username,
                    DisplayName = item.Name,
                    ProfileImageUrl = item.ProfileImageUrl
                }));
            }

            nextToken = payload?.Meta?.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        return results;
    }

    public async Task<List<XPostInfo>> GetRecentPostsAsync(string accessToken, string userId, DateTime? since, CancellationToken cancellationToken = default)
    {
        var posts = new List<XPostInfo>();
        string? nextToken = null;

        do
        {
            var url = $"{_settings.BaseUrl}/2/users/{userId}/tweets"
                + "?max_results=100"
                + "&tweet.fields=created_at,public_metrics,attachments"
                + "&expansions=attachments.media_keys,author_id"
                + "&media.fields=preview_image_url,url,type"
                + "&user.fields=profile_image_url";

            if (since.HasValue)
            {
                url += $"&start_time={Uri.EscapeDataString(since.Value.ToUniversalTime().ToString("O"))}";
            }

            if (!string.IsNullOrEmpty(nextToken))
            {
                url += $"&pagination_token={Uri.EscapeDataString(nextToken)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<XPostResponse>(JsonOptions, cancellationToken);
            if (payload?.Data != null)
            {
                var mediaByKey = payload.Includes?.Media?.ToDictionary(m => m.MediaKey, StringComparer.Ordinal) ?? new Dictionary<string, XMedia>();
                var usersById = payload.Includes?.Users?.ToDictionary(u => u.Id, StringComparer.Ordinal) ?? new Dictionary<string, XUser>();

                foreach (var item in payload.Data)
                {
                    var author = usersById.TryGetValue(item.AuthorId ?? string.Empty, out var user)
                        ? new XUserProfile
                        {
                            XUserId = user.Id,
                            Handle = user.Username,
                            DisplayName = user.Name,
                            ProfileImageUrl = user.ProfileImageUrl
                        }
                        : new XUserProfile { XUserId = item.AuthorId ?? string.Empty };

                    var media = new List<XMediaInfo>();
                    if (item.Attachments?.MediaKeys != null)
                    {
                        foreach (var key in item.Attachments.MediaKeys)
                        {
                            if (mediaByKey.TryGetValue(key, out var mediaItem))
                            {
                                media.Add(new XMediaInfo
                                {
                                    Type = mediaItem.Type ?? string.Empty,
                                    Url = mediaItem.Url,
                                    PreviewImageUrl = mediaItem.PreviewImageUrl
                                });
                            }
                        }
                    }

                    posts.Add(new XPostInfo
                    {
                        PostId = item.Id,
                        Text = item.Text ?? string.Empty,
                        Url = $"https://x.com/{author.Handle}/status/{item.Id}",
                        CreatedAt = item.CreatedAt ?? DateTime.UtcNow,
                        Author = author,
                        LikeCount = item.PublicMetrics?.LikeCount ?? 0,
                        ReplyCount = item.PublicMetrics?.ReplyCount ?? 0,
                        RepostCount = item.PublicMetrics?.RepostCount ?? 0,
                        QuoteCount = item.PublicMetrics?.QuoteCount ?? 0,
                        Media = media
                    });
                }
            }

            nextToken = payload?.Meta?.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        return posts;
    }

    private void AddClientAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_settings.ClientSecret))
        {
            var credentials = $"{_settings.ClientId}:{_settings.ClientSecret}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }
    }

    private class XTokenApiResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }

    private class XUserResponse
    {
        public XUser? Data { get; set; }
    }

    private class XFollowResponse
    {
        public List<XUser>? Data { get; set; }
        public XMeta? Meta { get; set; }
    }

    private class XPostResponse
    {
        public List<XPost>? Data { get; set; }
        public XPostIncludes? Includes { get; set; }
        public XMeta? Meta { get; set; }
    }

    private class XPostIncludes
    {
        public List<XMedia>? Media { get; set; }
        public List<XUser>? Users { get; set; }
    }

    private class XMeta
    {
        [JsonPropertyName("next_token")]
        public string? NextToken { get; set; }
    }

    private class XUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Name { get; set; }
        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }
    }

    private class XPost
    {
        public string Id { get; set; } = string.Empty;
        public string? Text { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("author_id")]
        public string? AuthorId { get; set; }
        [JsonPropertyName("public_metrics")]
        public XPublicMetrics? PublicMetrics { get; set; }
        public XAttachments? Attachments { get; set; }
    }

    private class XPublicMetrics
    {
        [JsonPropertyName("like_count")]
        public int LikeCount { get; set; }
        [JsonPropertyName("reply_count")]
        public int ReplyCount { get; set; }
        [JsonPropertyName("retweet_count")]
        public int RepostCount { get; set; }
        [JsonPropertyName("quote_count")]
        public int QuoteCount { get; set; }
    }

    private class XAttachments
    {
        [JsonPropertyName("media_keys")]
        public List<string>? MediaKeys { get; set; }
    }

    private class XMedia
    {
        [JsonPropertyName("media_key")]
        public string MediaKey { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Url { get; set; }
        [JsonPropertyName("preview_image_url")]
        public string? PreviewImageUrl { get; set; }
    }
}
