using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing feeds and resources through the API.
/// </summary>
public class FeedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;
    private readonly ILogger<FeedService> _logger;

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

        // Add auth token if available
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
            var queryParams = new List<string>
            {
                "pageNumber=1",
                "pageSize=50" // Get first 50 resources
            };

            if (type.HasValue)
            {
                queryParams.Add($"type={type.Value}");
            }

            var queryString = string.Join("&", queryParams);

            using var httpClient = CreateHttpClient();
            var response = await httpClient.GetAsync($"/api/v1/resources?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResourceResponse>();
                return pagedResponse?.Items.Select(r => new ResourceItem
                {
                    Id = r.Id,
                    Title = r.Title,
                    Url = r.Url,
                    Type = r.Type,
                    Description = r.Description,
                    PublishedAt = r.PublishedDate ?? r.CreatedAt
                }).ToList() ?? new List<ResourceItem>();
            }

            _logger.LogWarning("Failed to fetch resources: {StatusCode}", response.StatusCode);
            return new List<ResourceItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching resources");
            return new List<ResourceItem>();
        }
    }
}

/// <summary>
/// Paged response from API.
/// </summary>
public class PagedResourceResponse
{
    public List<ResourceResponse> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Resource response from API.
/// </summary>
public class ResourceResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public ResourceType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Resource item for display in UI.
/// </summary>
public class ResourceItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string? Description { get; set; }
    public DateTime PublishedAt { get; set; }
}
