using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Llm.Models;

namespace Rsl.Llm.Services;

/// <summary>
/// LLM-based ingestion agent that extracts learning resources from URLs.
/// Fetches HTML content and uses ChatGPT to extract structured resource data.
/// </summary>
public class IngestionAgent : IIngestionAgent
{
    private readonly ILlmClient _llmClient;
    private readonly IContentFetcherService _contentFetcher;
    private readonly ILogger<IngestionAgent> _logger;

    public IngestionAgent(
        ILlmClient llmClient,
        IContentFetcherService contentFetcher,
        ILogger<IngestionAgent> logger)
    {
        _llmClient = llmClient;
        _contentFetcher = contentFetcher;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestFromUrlAsync(
        string sourceUrl,
        Guid? sourceId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ingestion from URL: {SourceUrl}", sourceUrl);

            // Step 1: Fetch content (HTML or RSS/XML)
            var contentResult = await _contentFetcher.FetchContentAsync(sourceUrl, cancellationToken);

            if (!contentResult.Success || string.IsNullOrWhiteSpace(contentResult.Content))
            {
                _logger.LogWarning("Failed to fetch content from {SourceUrl}: {Error}",
                    sourceUrl, contentResult.ErrorMessage);

                return new IngestionResult
                {
                    Success = true,
                    SourceUrl = sourceUrl,
                    Resources = new List<ExtractedResource>(),
                    TotalFound = 0,
                    NewResources = 0,
                    DuplicatesSkipped = 0,
                    ErrorMessage = contentResult.ErrorMessage ?? "Failed to fetch content"
                };
            }

            // Step 2: Send content to ChatGPT for resource extraction
            var systemPrompt = GetSystemPrompt();
            var userMessage = GetUserMessage(sourceUrl, contentResult.Content);

            var response = await _llmClient.SendMessageAsync(
                systemPrompt,
                userMessage,
                tools: null,
                cancellationToken);

            // Step 3: Parse the response
            var result = ParseIngestionResult(response.Content, sourceUrl);

            _logger.LogInformation(
                "Ingestion completed from {SourceUrl}: {TotalFound} resources found",
                sourceUrl, result.TotalFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ingestion from {SourceUrl}", sourceUrl);
            return new IngestionResult
            {
                Success = true,
                SourceUrl = sourceUrl,
                Resources = new List<ExtractedResource>(),
                TotalFound = 0,
                NewResources = 0,
                DuplicatesSkipped = 0,
                ErrorMessage = $"Ingestion error: {ex.Message}"
            };
        }
    }


    private IngestionResult ParseIngestionResult(string llmResponse, string sourceUrl)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1)
            {
                _logger.LogWarning("No JSON found in LLM response for {SourceUrl}. Response: {Response}",
                    sourceUrl, llmResponse?.Substring(0, Math.Min(200, llmResponse?.Length ?? 0)));

                // Return success with 0 resources rather than failing - this is expected for some sources
                return new IngestionResult
                {
                    Success = true,
                    SourceUrl = sourceUrl,
                    Resources = new List<ExtractedResource>(),
                    TotalFound = 0,
                    NewResources = 0,
                    DuplicatesSkipped = 0,
                    ErrorMessage = "Source inaccessible or no resources found"
                };
            }

            var jsonString = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var parsedData = JsonSerializer.Deserialize<JsonElement>(jsonString);

            var resources = new List<ExtractedResource>();

            if (parsedData.TryGetProperty("resources", out var resourcesArray))
            {
                foreach (var item in resourcesArray.EnumerateArray())
                {
                    try
                    {
                        var resource = ParseExtractedResource(item);
                        if (resource != null)
                        {
                            resources.Add(resource);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse resource from JSON");
                    }
                }
            }

            return new IngestionResult
            {
                Success = true,
                SourceUrl = sourceUrl,
                Resources = resources,
                TotalFound = resources.Count,
                NewResources = resources.Count,
                DuplicatesSkipped = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing ingestion result from {SourceUrl}", sourceUrl);
            // Return success with 0 resources rather than failing
            return new IngestionResult
            {
                Success = true,
                SourceUrl = sourceUrl,
                Resources = new List<ExtractedResource>(),
                TotalFound = 0,
                NewResources = 0,
                DuplicatesSkipped = 0,
                ErrorMessage = $"Could not parse response: {ex.Message}"
            };
        }
    }

    private ExtractedResource? ParseExtractedResource(JsonElement json)
    {
        if (!json.TryGetProperty("title", out var title) ||
            !json.TryGetProperty("url", out var url))
        {
            return null;
        }

        var resource = new ExtractedResource
        {
            Title = title.GetString() ?? string.Empty,
            Url = url.GetString() ?? string.Empty,
            Description = json.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? string.Empty : string.Empty
        };

        // Parse resource type
        if (json.TryGetProperty("type", out var type))
        {
            if (Enum.TryParse<ResourceType>(type.GetString(), true, out var resourceType))
            {
                resource.Type = resourceType;
            }
        }

        // Parse optional fields
        if (json.TryGetProperty("published_date", out var pubDate))
        {
            if (DateTime.TryParse(pubDate.GetString(), out var date))
            {
                resource.PublishedDate = date;
            }
        }

        if (json.TryGetProperty("author", out var author))
            resource.Author = author.GetString();

        if (json.TryGetProperty("channel", out var channel))
            resource.Channel = channel.GetString();

        if (json.TryGetProperty("duration", out var duration))
            resource.Duration = duration.GetString();

        if (json.TryGetProperty("thumbnail_url", out var thumbnail))
            resource.ThumbnailUrl = thumbnail.GetString();

        if (json.TryGetProperty("doi", out var doi))
            resource.DOI = doi.GetString();

        if (json.TryGetProperty("journal", out var journal))
            resource.Journal = journal.GetString();

        return resource;
    }

    private string GetSystemPrompt()
    {
        return @"You are a learning resource extraction assistant. You must respond ONLY with valid JSON.

Extract learning resources from the provided HTML/RSS/XML content. If nothing can be confidently extracted, return { ""resources"": [] }.

Output schema (required fields only; optional fields allowed only when present in the content):
{
  ""resources"": [
    {
      ""title"": string,
      ""url"": string,
      ""description"": string,
      ""type"": ""Paper"" | ""Video"" | ""BlogPost""
    }
  ]
}

Extraction rules:
- Only include resources that are explicitly present in the provided content. Do not invent or infer facts.
- Each item must have a non-empty title and a URL. Skip items that are missing either.
- URLs must be absolute. Resolve relative URLs using the source page URL as the base (and respect any HTML <base href> if present).
- Descriptions must be non-empty:
  - Prefer an abstract/summary/snippet when present.
  - Otherwise, write a short factual description using only visible metadata (e.g., authors, venue/journal, date, subjects/tags, comments). Do not fabricate missing details.
- Choose the most useful, distinct items and de-duplicate near-identical entries (e.g., same URL).
- Limit to at most 20 items to avoid truncation.

Type guidance:
- Paper: academic/research papers or preprints (e.g., arXiv entries, DOI/journal/conference pages).
- Video: individual videos (watch pages or clearly identified video items).
- BlogPost: articles/posts/tutorials.

CRITICAL exclusions:
- NEVER extract the source/feed/channel itself as a resource. Only extract individual content items (videos, articles, papers).
- For RSS/XML feeds: extract only the individual <item> or <entry> elements. Do NOT extract the feed metadata, channel information, or feed URL itself.
- For YouTube RSS feeds: extract only individual video watch URLs (e.g., youtube.com/watch?v=...). Do NOT extract channel URLs, feed URLs, or channel metadata.
- Exclude navigation links, ads, generic category/tag indexes, search pages without specific items, login/about/profile pages, or playlists without individual video entries.
- If the URL being extracted matches or is nearly identical to the source URL provided in the user message, skip it.";
    }

    private string GetUserMessage(string sourceUrl, string htmlContent)
    {
        // Truncate content if it's too long to avoid token limits
        const int maxHtmlLength = 50000; // ~12.5k tokens (rough estimate)
        var truncatedHtml = htmlContent.Length > maxHtmlLength
            ? htmlContent.Substring(0, maxHtmlLength) + "\n\n[...Content truncated for length...]"
            : htmlContent;

        return $@"Extract all learning resources from this content (HTML page or RSS/XML feed).

Source URL: {sourceUrl}

Content:
{truncatedHtml}

Return the extracted resources as JSON. Respond with JSON only.";
    }
}

