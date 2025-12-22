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

            // Log token usage and finish reason
            _logger.LogInformation(
                "OpenAI response: {CompletionTokens} completion tokens, finish_reason: {FinishReason}",
                response.CompletionTokens, response.FinishReason);

            // Step 3: Parse the response
            var result = ParseIngestionResult(response, sourceUrl);

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


    private IngestionResult ParseIngestionResult(LlmResponse response, string sourceUrl)
    {
        var llmResponse = response.Content;

        try
        {
            // Log last 200 chars for debugging truncation
            if (llmResponse.Length > 200)
            {
                var lastChars = llmResponse.Substring(llmResponse.Length - 200);
                _logger.LogDebug("Last 200 chars of response: {LastChars}", lastChars);
            }

            // Try to extract JSON from the response
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1)
            {
                _logger.LogWarning(
                    "No JSON found in LLM response for {SourceUrl}. FinishReason: {FinishReason}, Response preview: {Response}",
                    sourceUrl, response.FinishReason, llmResponse?.Substring(0, Math.Min(200, llmResponse?.Length ?? 0)));

                // Return success with 0 resources rather than failing - this is expected for some sources
                return new IngestionResult
                {
                    Success = true,
                    SourceUrl = sourceUrl,
                    Resources = new List<ExtractedResource>(),
                    TotalFound = 0,
                    NewResources = 0,
                    DuplicatesSkipped = 0,
                    ErrorMessage = $"No JSON found (finish_reason: {response.FinishReason})"
                };
            }

            var jsonString = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

            // Validate JSON is complete (basic check)
            var openBraces = jsonString.Count(c => c == '{');
            var closeBraces = jsonString.Count(c => c == '}');
            var openBrackets = jsonString.Count(c => c == '[');
            var closeBrackets = jsonString.Count(c => c == ']');

            if (openBraces != closeBraces || openBrackets != closeBrackets)
            {
                _logger.LogWarning(
                    "Malformed JSON detected for {SourceUrl}: braces {OpenBrace}/{CloseBrace}, brackets {OpenBracket}/{CloseBracket}",
                    sourceUrl, openBraces, closeBraces, openBrackets, closeBrackets);

                return new IngestionResult
                {
                    Success = true,
                    SourceUrl = sourceUrl,
                    Resources = new List<ExtractedResource>(),
                    TotalFound = 0,
                    NewResources = 0,
                    DuplicatesSkipped = 0,
                    ErrorMessage = "Malformed JSON (likely truncated)"
                };
            }

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
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "JSON parse error for {SourceUrl}. FinishReason: {FinishReason}, Last 100 chars: {LastChars}",
                sourceUrl,
                response.FinishReason,
                llmResponse.Length > 100 ? llmResponse.Substring(llmResponse.Length - 100) : llmResponse);

            return new IngestionResult
            {
                Success = true,
                SourceUrl = sourceUrl,
                Resources = new List<ExtractedResource>(),
                TotalFound = 0,
                NewResources = 0,
                DuplicatesSkipped = 0,
                ErrorMessage = $"JSON parse error: {ex.Message}"
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
        return @"You are a learning resource extraction assistant. You MUST respond with ONLY valid JSON - no other text.

Extract learning resources from the provided HTML/RSS/XML content. If nothing can be extracted, return { ""resources"": [] }.

OUTPUT SCHEMA (strict):
{
  ""resources"": [
    {
      ""title"": string,
      ""url"": string (absolute URL),
      ""description"": string,
      ""type"": ""Paper"" | ""Video"" | ""BlogPost""
    }
  ]
}

CRITICAL CONSTRAINTS:
1. Extract up to 20 most important/recent items
2. DESCRIPTIONS: 200 characters maximum, concise and factual
3. URLS: Must be absolute (not relative)
4. DEDUPLICATE: Same URL = skip duplicate
5. VALID JSON: Response must be parseable JSON, properly closed braces/brackets

EXTRACTION RULES:
- Only extract explicitly present resources (no invention)
- Each item MUST have: non-empty title, absolute URL, description
- Description priority: use abstract/summary if concise, otherwise metadata (authors, venue, date)
- De-duplicate by URL (keep first occurrence)
- Select the most valuable/recent items

TYPE GUIDANCE:
- Paper: academic/research papers, preprints (arXiv, DOI pages)
- Video: individual video watch pages
- BlogPost: articles, posts, tutorials

EXCLUDE:
- Source/feed/channel itself (only extract individual items)
- Navigation, ads, indexes, search pages, login pages
- URLs matching the source URL provided
- RSS/XML: extract <item>/<entry> only, NOT feed metadata
- YouTube: extract watch URLs only, NOT channel URLs

FAILURE MODE: If extraction fails or no valid items found, return { ""resources"": [] }";
    }

    private string GetUserMessage(string sourceUrl, string htmlContent)
    {
        // Truncate content if it's too long to avoid token limits
        const int maxHtmlLength = 50000; // ~12.5k tokens
        var truncatedHtml = htmlContent.Length > maxHtmlLength
            ? htmlContent.Substring(0, maxHtmlLength) + "\n\n[...Content truncated for length...]"
            : htmlContent;

        return $@"Extract learning resources from this content.

Source URL: {sourceUrl}

Content:
{truncatedHtml}

REQUIREMENTS:
- Maximum 20 resources
- Each description: 200 characters maximum
- URLs must be absolute
- De-duplicate by URL
- Return ONLY valid JSON

Respond with JSON only (no markdown, no explanation):";
    }
}

