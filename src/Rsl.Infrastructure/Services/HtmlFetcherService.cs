using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Rsl.Core.Interfaces;

namespace Rsl.Infrastructure.Services;

/// <summary>
/// Implementation of IHtmlFetcherService that fetches and minimally cleans HTML content.
/// </summary>
public class HtmlFetcherService : IHtmlFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HtmlFetcherService> _logger;

    private const int TimeoutSeconds = 30;
    private const int MaxRedirects = 5;

    public HtmlFetcherService(
        HttpClient httpClient,
        ILogger<HtmlFetcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; RSL-Bot/1.0; +https://github.com/payaam/recommendation-system-for-learning)");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    }

    public async Task<HtmlFetchResult> FetchHtmlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching HTML from URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch HTML from {Url}: {StatusCode}",
                    url, response.StatusCode);

                return new HtmlFetchResult
                {
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                };
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Minimal cleaning: Remove script and style tags to reduce token usage
            var cleanedHtml = CleanHtml(html);

            _logger.LogInformation("Successfully fetched HTML from {Url} ({Length} characters after cleaning)",
                url, cleanedHtml.Length);

            return new HtmlFetchResult
            {
                Success = true,
                Html = cleanedHtml,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching HTML from {Url}", url);
            return new HtmlFetchResult
            {
                Success = false,
                ErrorMessage = $"HTTP error: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching HTML from {Url}", url);
            return new HtmlFetchResult
            {
                Success = false,
                ErrorMessage = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching HTML from {Url}", url);
            return new HtmlFetchResult
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

