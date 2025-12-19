namespace Rsl.Core.Interfaces;

/// <summary>
/// Service for fetching content (HTML or RSS/XML) from URLs.
/// </summary>
public interface IContentFetcherService
{
    /// <summary>
    /// Fetches content from the specified URL.
    /// </summary>
    /// <param name="url">The URL to fetch content from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing content or error information</returns>
    Task<ContentFetchResult> FetchContentAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a content fetch operation.
/// </summary>
public class ContentFetchResult
{
    /// <summary>
    /// Whether the fetch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The fetched content (if successful).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Error message (if unsuccessful).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// HTTP status code received.
    /// </summary>
    public int StatusCode { get; set; }
}

