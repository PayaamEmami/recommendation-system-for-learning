using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing feeds and resources that integrates with the RSL API.
/// </summary>
public class FeedService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly ILogger<FeedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public FeedService(
        HttpClient httpClient,
        AuthService authService,
        ILogger<FeedService> logger)
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

    public async Task<List<ResourceItem>> GetFeedAsync(ResourceType? type = null)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot fetch feed");
                return new List<ResourceItem>();
            }

            SetAuthHeader();

            // Get today's recommendations from the API
            var response = await _httpClient.GetAsync("/api/v1/recommendations");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch recommendations: {StatusCode}", response.StatusCode);
                return new List<ResourceItem>();
            }

            var feedRecommendations = await response.Content.ReadFromJsonAsync<List<FeedRecommendationsResponse>>(JsonOptions);

            if (feedRecommendations == null || !feedRecommendations.Any())
            {
                _logger.LogInformation("No recommendations available");
                return new List<ResourceItem>();
            }

            // Flatten all recommendations into a single list
            var resources = new List<ResourceItem>();

            foreach (var feed in feedRecommendations)
            {
                if (type.HasValue && feed.FeedType != type.Value)
                    continue;

                foreach (var rec in feed.Recommendations)
                {
                    resources.Add(new ResourceItem
                    {
                        Id = rec.Resource.Id,
                        Title = rec.Resource.Title,
                        Url = rec.Resource.Url,
                        Type = rec.Resource.Type,
                        Description = rec.Resource.Description,
                        PublishedAt = rec.Resource.PublishedDate ?? rec.Resource.CreatedAt
                    });
                }
            }

            return resources.OrderByDescending(r => r.PublishedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching feed");
            return new List<ResourceItem>();
        }
    }

    public async Task<List<VoteItem>> GetUserVotesAsync()
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return new List<VoteItem>();
            }

            SetAuthHeader();
            var response = await _httpClient.GetAsync("/api/v1/users/me/votes");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch user votes: {StatusCode}", response.StatusCode);
                return new List<VoteItem>();
            }

            var votes = await response.Content.ReadFromJsonAsync<List<VoteItem>>(JsonOptions);
            return votes ?? new List<VoteItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user votes");
            return new List<VoteItem>();
        }
    }

    public async Task<VoteItem?> VoteAsync(Guid resourceId, VoteType voteType)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return null;
            }

            SetAuthHeader();
            var request = new { voteType = voteType };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/resources/{resourceId}/vote", request, JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to vote on resource {ResourceId}: {StatusCode}", resourceId, response.StatusCode);
                return null;
            }

            var vote = await response.Content.ReadFromJsonAsync<VoteItem>(JsonOptions);
            _logger.LogInformation("Successfully voted {VoteType} on resource {ResourceId}", voteType, resourceId);
            return vote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on resource {ResourceId}", resourceId);
            return null;
        }
    }

    public async Task<bool> RemoveVoteAsync(Guid resourceId)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                return false;
            }

            SetAuthHeader();
            var response = await _httpClient.DeleteAsync($"/api/v1/resources/{resourceId}/vote");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to remove vote from resource {ResourceId}: {StatusCode}", resourceId, response.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully removed vote from resource {ResourceId}", resourceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing vote from resource {ResourceId}", resourceId);
            return false;
        }
    }
}

public class ResourceItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string? Description { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class VoteItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public VoteType VoteType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// API Response DTOs
public class FeedRecommendationsResponse
{
    public ResourceType FeedType { get; set; }
    public DateOnly Date { get; set; }
    public List<RecommendationItemResponse> Recommendations { get; set; } = new();
}

public class RecommendationItemResponse
{
    public Guid Id { get; set; }
    public ResourceItemResponse Resource { get; set; } = null!;
    public int Position { get; set; }
    public double Score { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class ResourceItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public ResourceType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
