using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing user sources through the API.
/// </summary>
public class SourceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;
    private readonly ILogger<SourceService> _logger;

    public SourceService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        AuthService authService,
        ILogger<SourceService> logger)
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

    public async Task<List<SourceItem>> GetUserSourcesAsync()
    {
        try
        {
            using var httpClient = CreateHttpClient();
            var response = await httpClient.GetAsync("/api/sources");

            if (response.IsSuccessStatusCode)
            {
                var sources = await response.Content.ReadFromJsonAsync<List<SourceResponse>>();
                return sources?.Select(s => new SourceItem
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Name = s.Name,
                    Url = s.Url,
                    Category = s.Category,
                    Description = s.Description,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    LastFetchedAt = s.LastFetchedAt,
                    ResourceCount = s.ResourceCount
                }).ToList() ?? new List<SourceItem>();
            }

            _logger.LogWarning("Failed to fetch sources: {StatusCode}", response.StatusCode);
            return new List<SourceItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sources");
            return new List<SourceItem>();
        }
    }

    public async Task<bool> AddSourceAsync(string name, string url, ResourceType category, string? description)
    {
        try
        {
            var request = new
            {
                name = name,
                url = url,
                category = category,
                description = description,
                isActive = true
            };

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PostAsJsonAsync("/api/sources", request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to add source: {StatusCode} - {Content}", response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding source");
            return false;
        }
    }

    public async Task<bool> DeleteSourceAsync(Guid sourceId)
    {
        try
        {
            using var httpClient = CreateHttpClient();
            var response = await httpClient.DeleteAsync($"/api/sources/{sourceId}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Failed to delete source: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting source");
            return false;
        }
    }

    public async Task<bool> ToggleSourceActiveAsync(Guid sourceId)
    {
        try
        {
            // First get the current source
            using var httpClient = CreateHttpClient();
            var getResponse = await httpClient.GetAsync($"/api/sources/{sourceId}");

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch source for toggle: {StatusCode}", getResponse.StatusCode);
                return false;
            }

            var source = await getResponse.Content.ReadFromJsonAsync<SourceResponse>();
            if (source == null)
                return false;

            // Update with toggled active state
            var request = new
            {
                isActive = !source.IsActive
            };

            var updateResponse = await httpClient.PutAsJsonAsync($"/api/sources/{sourceId}", request);

            if (updateResponse.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Failed to toggle source: {StatusCode}", updateResponse.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling source");
            return false;
        }
    }

    public async Task<bool> UpdateSourceAsync(Guid sourceId, string? name, string? url, ResourceType? category, string? description)
    {
        try
        {
            var request = new
            {
                name = name,
                url = url,
                category = category,
                description = description
            };

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PutAsJsonAsync($"/api/sources/{sourceId}", request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to update source: {StatusCode} - {Content}", response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating source");
            return false;
        }
    }
}

/// <summary>
/// Source response from API.
/// </summary>
public class SourceResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastFetchedAt { get; set; }
    public string? LastFetchError { get; set; }
    public int ResourceCount { get; set; }
}

/// <summary>
/// Source item for display in UI.
/// </summary>
public class SourceItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastFetchedAt { get; set; }
    public int ResourceCount { get; set; }
}
