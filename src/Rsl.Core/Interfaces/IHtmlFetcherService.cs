namespace Rsl.Core.Interfaces;

/// <summary>
/// Service for fetching HTML content from URLs.
/// </summary>
public interface IHtmlFetcherService
{
    /// <summary>
    /// Fetches HTML content from the specified URL.
    /// </summary>
    /// <param name="url">The URL to fetch HTML from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing HTML content or error information</returns>
    Task<HtmlFetchResult> FetchHtmlAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an HTML fetch operation.
/// </summary>
public class HtmlFetchResult
{
    /// <summary>
    /// Whether the fetch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The fetched HTML content (if successful).
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// Error message (if unsuccessful).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// HTTP status code received.
    /// </summary>
    public int StatusCode { get; set; }
}

