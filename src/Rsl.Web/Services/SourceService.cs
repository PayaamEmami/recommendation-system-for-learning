using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing user sources that integrates with the RSL API.
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

        // Add authentication token if available
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
            if (!_authService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot fetch sources");
                return new List<SourceItem>();
            }

            using var httpClient = CreateHttpClient();
            var response = await httpClient.GetAsync("/api/v1/sources");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch sources: {StatusCode}", response.StatusCode);
                return new List<SourceItem>();
            }

            var sources = await response.Content.ReadFromJsonAsync<List<SourceResponse>>();

            if (sources == null)
            {
                return new List<SourceItem>();
            }

            return sources.Select(s => new SourceItem
            {
                Id = s.Id,
                UserId = s.UserId,
                Name = s.Name,
                Url = s.Url,
                Category = s.Category,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                LastFetchedAt = s.LastFetchedAt,
                ResourceCount = s.ResourceCount
            }).OrderBy(s => s.Name).ToList();
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
            if (!_authService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot add source");
                return false;
            }

            var request = new CreateSourceRequest
            {
                Name = name,
                Url = url,
                Category = category,
                Description = description
            };

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PostAsJsonAsync("/api/v1/sources", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to add source: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Successfully added source: {Name}", name);
            return true;
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
            if (!_authService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot delete source");
                return false;
            }

            using var httpClient = CreateHttpClient();
            var response = await httpClient.DeleteAsync($"/api/v1/sources/{sourceId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete source: {StatusCode}", response.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully deleted source: {SourceId}", sourceId);
            return true;
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
            if (!_authService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot toggle source");
                return false;
            }

            // First, get the current source to determine its state
            using var httpClient = CreateHttpClient();
            var getResponse = await httpClient.GetAsync($"/api/v1/sources/{sourceId}");

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch source: {StatusCode}", getResponse.StatusCode);
                return false;
            }

            var source = await getResponse.Content.ReadFromJsonAsync<SourceResponse>();
            if (source == null)
            {
                return false;
            }

            // Update the source with toggled IsActive state
            var updateRequest = new UpdateSourceRequest
            {
                Name = source.Name,
                Url = source.Url,
                Category = source.Category,
                Description = source.Description,
                IsActive = !source.IsActive
            };

            var updateResponse = await httpClient.PutAsJsonAsync($"/api/v1/sources/{sourceId}", updateRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update source: {StatusCode}", updateResponse.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully toggled source: {SourceId}", sourceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling source");
            return false;
        }
    }

    public async Task<bool> UpdateSourceAsync(Guid sourceId, string name, string url, ResourceType category, string? description)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                _logger.LogWarning("User not authenticated, cannot update source");
                return false;
            }

            // First get the current source to preserve its IsActive state
            using var httpClient = CreateHttpClient();
            var getResponse = await httpClient.GetAsync($"/api/v1/sources/{sourceId}");

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch source for update: {StatusCode}", getResponse.StatusCode);
                return false;
            }

            var source = await getResponse.Content.ReadFromJsonAsync<SourceResponse>();
            if (source == null)
            {
                return false;
            }

            var updateRequest = new UpdateSourceRequest
            {
                Name = name,
                Url = url,
                Category = category,
                Description = description,
                IsActive = source.IsActive // Preserve existing active state
            };

            var response = await httpClient.PutAsJsonAsync($"/api/v1/sources/{sourceId}", updateRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update source: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Successfully updated source: {SourceId}", sourceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating source");
            return false;
        }
    }

    public async Task<BulkImportResultModel> BulkImportSourcesAsync(string json)
    {
        try
        {
            if (!_authService.CurrentState.IsAuthenticated)
            {
                throw new InvalidOperationException("User not authenticated");
            }

            using var httpClient = CreateHttpClient();
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/api/v1/sources/bulk-import", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to bulk import sources: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to import sources: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<BulkImportResultResponse>();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to parse bulk import response");
            }

            return new BulkImportResultModel
            {
                Imported = result.Imported,
                Failed = result.Failed,
                Errors = result.Errors.Select(e => new BulkImportErrorModel
                {
                    Url = e.Url,
                    Error = e.Error
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk import");
            throw;
        }
    }
}

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
    public DateTime? LastFetchedAt { get; set; }
    public int ResourceCount { get; set; }
}

// API Request/Response DTOs
public class CreateSourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Category { get; set; }
    public string? Description { get; set; }
}

public class UpdateSourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SourceResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ResourceType Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastFetchedAt { get; set; }
    public string? LastFetchError { get; set; }
    public int ResourceCount { get; set; }
}

public class BulkImportResultResponse
{
    public int Imported { get; set; }
    public int Failed { get; set; }
    public List<BulkImportErrorResponse> Errors { get; set; } = new();
}

public class BulkImportErrorResponse
{
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class BulkImportResultModel
{
    public int Imported { get; set; }
    public int Failed { get; set; }
    public List<BulkImportErrorModel> Errors { get; set; } = new();
}

public class BulkImportErrorModel
{
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
