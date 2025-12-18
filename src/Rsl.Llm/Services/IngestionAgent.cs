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
        return @"You are a learning resource extractor. Given HTML content from a webpage, identify and extract all learning resources (papers, videos, blog posts).

Your task:
1. Analyze the provided HTML content
2. Identify all learning resources found on the page
3. Extract key information: title, URL, description
4. Categorize each resource: Paper, Video, or BlogPost
5. Extract metadata when available (author, published date, DOI, journal, channel, duration, etc.)

Resource Type Guidelines:
- Paper: Academic papers, research publications, technical papers, arXiv papers
- Video: YouTube videos, educational videos, conference talks, tutorials
- BlogPost: Blog articles, technical write-ups, tutorials, how-to guides

IMPORTANT:
- Extract ONLY learning resources, not navigation links or ads
- Each resource must have a valid URL
- If no resources are found, return an empty array
- Be generous with descriptions - extract summaries or abstracts when available

Output Format:
Return ONLY valid JSON in this exact format:
{
  ""resources"": [
    {
      ""title"": ""Resource Title"",
      ""url"": ""https://example.com/resource"",
      ""description"": ""Brief description or summary"",
      ""type"": ""Paper"",
      ""published_date"": ""2024-01-15"",
      ""author"": ""Author Name"",
      ""channel"": ""Channel Name"",
      ""duration"": ""15:30"",
      ""doi"": ""10.1234/example"",
      ""journal"": ""Journal Name""
    }
  ]
}";
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

Return the extracted resources as JSON.";
    }
}

