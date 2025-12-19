using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Rsl.Core.Interfaces;

namespace Rsl.Infrastructure.Services;

/// <summary>
/// Implementation of IContentFetcherService that fetches and minimally cleans HTML content and RSS/XML feeds.
/// </summary>
public class ContentFetcherService : IContentFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentFetcherService> _logger;

    private const int TimeoutSeconds = 30;
    private const int MaxRedirects = 5;

    public ContentFetcherService(
        HttpClient httpClient,
        ILogger<ContentFetcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; RSL-Bot/1.0; +https://github.com/payaam/recommendation-system-for-learning)");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml,application/rss+xml,application/atom+xml;q=0.9,*/*;q=0.8");
    }

    public async Task<ContentFetchResult> FetchContentAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching content from URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch content from {Url}: {StatusCode}",
                    url, response.StatusCode);

                return new ContentFetchResult
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                };
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Minimal cleaning: Remove script and style tags to reduce token usage
            var cleanedHtml = CleanHtml(html);

            _logger.LogInformation("Successfully fetched content from {Url} ({Length} characters after cleaning)",
                url, cleanedHtml.Length);

            return new ContentFetchResult
            {
                Success = true,
                Content = cleanedHtml,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching content from {Url}", url);
            return new ContentFetchResult
            {
                Success = false,
                ErrorMessage = $"HTTP error: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching content from {Url}", url);
            return new ContentFetchResult
            {
                Success = false,
                ErrorMessage = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching content from {Url}", url);
            return new ContentFetchResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Performs minimal HTML cleaning by removing script and style tags.
    /// This reduces token usage without losing content structure for ChatGPT.
    /// </summary>
    private string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove script tags and their content
        html = Regex.Replace(html, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
            "", RegexOptions.IgnoreCase);

        // Remove style tags and their content
        html = Regex.Replace(html, @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>",
            "", RegexOptions.IgnoreCase);

        return html;
    }
}

