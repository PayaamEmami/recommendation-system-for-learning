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
    private readonly IHtmlFetcherService _htmlFetcher;
    private readonly ILogger<IngestionAgent> _logger;

    public IngestionAgent(
        ILlmClient llmClient,
        IHtmlFetcherService htmlFetcher,
        ILogger<IngestionAgent> logger)
    {
        _llmClient = llmClient;
        _htmlFetcher = htmlFetcher;
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

            // Step 1: Fetch HTML content
            var htmlResult = await _htmlFetcher.FetchHtmlAsync(sourceUrl, cancellationToken);

            if (!htmlResult.Success || string.IsNullOrWhiteSpace(htmlResult.Html))
            {
                _logger.LogWarning("Failed to fetch HTML from {SourceUrl}: {Error}",
                    sourceUrl, htmlResult.ErrorMessage);

                return new IngestionResult
                {
                    Success = true,
                    SourceUrl = sourceUrl,
                    Resources = new List<ExtractedResource>(),
                    TotalFound = 0,
                    NewResources = 0,
                    DuplicatesSkipped = 0,
                    ErrorMessage = htmlResult.ErrorMessage ?? "Failed to fetch HTML"
                };
            }

            // Step 2: Send HTML to ChatGPT for resource extraction
            var systemPrompt = GetSystemPrompt();
            var userMessage = GetUserMessage(sourceUrl, htmlResult.Html);

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
        return @"You are a precise learning-resource extractor.

Rules:
- Output JSON only, no prose or markdown.
- Use this schema: { ""resources"": [ { ""title"": string, ""url"": string, ""description"": string, ""type"": ""Paper""|""Video""|""BlogPost"", ""published_date"": string (ISO, optional), ""author"": string (optional), ""channel"": string (optional), ""duration"": string (optional), ""doi"": string (optional), ""journal"": string (optional), ""thumbnail_url"": string (optional) } ] }
- Always return the top results you can confidently extract (up to 20). If none, return { ""resources"": [] }.
- Each resource must have: title (trimmed), absolute URL, non-empty description (summaries or abstracts are preferred).
- Be generous but factual: pull summaries/abstracts/snippets that are present in the HTML; do not invent facts.
- Classify:
  - Paper: academic/research/technical papers, arXiv/DOI/journal indicators.
  - Video: YouTube or other video entries, video watch links, channel videos, durations, thumbnails.
  - BlogPost: blog articles, tutorials, how-tos, technical write-ups.
- Do NOT return navigation, ads, categories, playlists without specific videos, or profile/about pages.
- For YouTube/channel pages: extract individual videos (title + video URL + channel + duration/thumbnail if visible).
- Normalize:
  - Make URLs absolute using the page base if needed.
  - Dates in ISO 8601 (YYYY-MM-DD) when present.
  - Duration as HH:MM:SS or MM:SS when present.
- If data is missing (e.g., author, doi), omit the field rather than guessing.";
    }

    private string GetUserMessage(string sourceUrl, string htmlContent)
    {
        // Truncate HTML if it's too long to avoid token limits
        const int maxHtmlLength = 50000; // ~12.5k tokens (rough estimate)
        var truncatedHtml = htmlContent.Length > maxHtmlLength
            ? htmlContent.Substring(0, maxHtmlLength) + "\n\n[...HTML truncated for length...]"
            : htmlContent;

        return $@"Extract all learning resources from this HTML content.

Source URL: {sourceUrl}

HTML Content:
{truncatedHtml}

Return the extracted resources as JSON. Respond with JSON only.";
    }
}

