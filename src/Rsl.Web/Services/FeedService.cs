using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing feeds and resources that integrates with the RSL API.
/// </summary>
public class FeedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;
    private readonly ILogger<FeedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public FeedService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        AuthService authService,
        ILogger<FeedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _authService = authService;
        _logger = logger;
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
        if (!string.IsNullOrEmpty(apiBaseUrl))
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }

        // Add authentication token if available
        if (!string.IsNullOrEmpty(_authService.CurrentState.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authService.CurrentState.AccessToken);
        }

        return client;
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

            using var httpClient = CreateHttpClient();

            // Get today's recommendations from the API
            var response = await httpClient.GetAsync("/api/v1/recommendations");

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
