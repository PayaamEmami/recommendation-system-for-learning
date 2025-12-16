using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rsl.Core.Enums;
using Rsl.Llm.Models;
using Rsl.Llm.Tools;

namespace Rsl.Llm.Services;

/// <summary>
/// LLM-based ingestion agent that extracts learning resources from URLs.
/// Uses function calling to interact with the database and avoid duplicates.
/// </summary>
public class IngestionAgent : IIngestionAgent
{
    private readonly ILlmClient _llmClient;
    private readonly AgentTools _agentTools;
    private readonly ILogger<IngestionAgent> _logger;

    private const int MaxIterations = 10; // Prevent infinite loops

    public IngestionAgent(
        ILlmClient llmClient,
        AgentTools agentTools,
        ILogger<IngestionAgent> logger)
    {
        _llmClient = llmClient;
        _agentTools = agentTools;
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

            var systemPrompt = GetSystemPrompt();
            var userMessage = GetUserMessage(sourceUrl, sourceId);
            var tools = _agentTools.GetToolDefinitions();

            // Start the conversation
            var response = await _llmClient.SendMessageAsync(
                systemPrompt,
                userMessage,
                tools,
                cancellationToken);

            // Handle tool calls in a loop
            var iteration = 0;
            while (response.HasToolCalls && iteration < MaxIterations)
            {
                iteration++;
                _logger.LogInformation("Agent iteration {Iteration}: Executing {Count} tool calls",
                    iteration, response.ToolCalls.Count);

                var toolResults = await ExecuteToolCallsAsync(response.ToolCalls, cancellationToken);

                response = await _llmClient.ContinueConversationAsync(
                    response.ConversationHistory,
                    toolResults,
                    cancellationToken);
            }

            if (iteration >= MaxIterations)
            {
                _logger.LogWarning("Agent reached max iterations ({MaxIterations})", MaxIterations);
            }

            // Parse the final response
            var result = ParseIngestionResult(response.Content, sourceUrl);

            _logger.LogInformation(
                "Ingestion completed: {TotalFound} resources found, {NewResources} new, {Duplicates} duplicates",
                result.TotalFound, result.NewResources, result.DuplicatesSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not complete ingestion from {SourceUrl}: {Message}", sourceUrl, ex.Message);
            // Return success with 0 resources rather than failing - gracefully handle inaccessible sources
            return new IngestionResult
            {
                Success = true,
                SourceUrl = sourceUrl,
                Resources = new List<ExtractedResource>(),
                TotalFound = 0,
                NewResources = 0,
                DuplicatesSkipped = 0,
                ErrorMessage = $"Source inaccessible: {ex.Message}"
            };
        }
    }

    private async Task<List<ToolResult>> ExecuteToolCallsAsync(
        List<ToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        var results = new List<ToolResult>();

        foreach (var toolCall in toolCalls)
        {
            _logger.LogDebug("Executing tool: {ToolName} with args: {Arguments}",
                toolCall.Name, toolCall.Arguments);

            var result = await _agentTools.ExecuteToolAsync(
                toolCall.Name,
                toolCall.Arguments,
                cancellationToken);

            results.Add(new ToolResult
            {
                ToolCallId = toolCall.Id,
                Result = result
            });
        }

        return results;
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

            var result = new IngestionResult
            {
                Success = true,
                SourceUrl = sourceUrl,
                Resources = resources,
                TotalFound = resources.Count,
                NewResources = parsedData.TryGetProperty("new_resources", out var newRes)
                    ? newRes.GetInt32() : resources.Count,
                DuplicatesSkipped = parsedData.TryGetProperty("duplicates_skipped", out var dups)
                    ? dups.GetInt32() : 0
            };

            return result;
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
        return @"You are an intelligent agent that extracts learning resources from web pages.

Your task is to:
1. Use the web search tool to access the provided URL and identify all learning resources (papers, videos, blog posts, social media posts)
2. Extract key information for each resource: title, URL, description
3. Categorize each resource into one of these types: Paper, Video, BlogPost, SocialMediaPost
4. Extract additional metadata when available (author, published date, etc.)

Resource Type Guidelines:
- Paper: Academic papers, research publications, technical papers, arXiv papers
- Video: YouTube videos, educational videos, conference talks, tutorials
- BlogPost: Blog articles, technical write-ups, tutorials, how-to guides
- SocialMediaPost: Twitter/X threads, LinkedIn posts, Reddit discussions

IMPORTANT: You have access to a web search tool that can help you access content from most URLs.
If a source is truly inaccessible (authentication required, rate limiting, etc.), respond with an empty resources array.
Some sources like Twitter/X may require authentication - this is expected and not an error.

Note: Don't worry about duplicate URLs - the database will automatically handle that.

After processing, ALWAYS respond with a JSON object in this exact format (even if the array is empty):
{
  ""resources"": [
    {
      ""title"": ""Resource Title"",
      ""url"": ""https://example.com/resource"",
      ""description"": ""Brief description or summary"",
      ""type"": ""Paper"",
      ""published_date"": ""2024-01-15"",
      ""author"": ""Author Name"" (optional, for papers and blog posts),
      ""channel"": ""Channel Name"" (optional, for videos),
      ""duration"": ""15:30"" (optional, for videos),
      ""doi"": ""10.1234/example"" (optional, for papers),
      ""journal"": ""Journal Name"" (optional, for papers)
    }
  ],
  ""new_resources"": 5,
  ""duplicates_skipped"": 2
}

If you cannot access the URL, return:
{
  ""resources"": [],
  ""new_resources"": 0,
  ""duplicates_skipped"": 0
}

Be thorough but efficient. Focus on quality learning resources relevant to education and professional development.";
    }

    private string GetUserMessage(string sourceUrl, Guid? sourceId)
    {
        var message = $@"Please visit this URL and extract all learning resources from it:

URL: {sourceUrl}";

        if (sourceId.HasValue)
        {
            message += $@"

Source ID: {sourceId}

Before extracting resources, use the 'get_resources_from_source' tool to see what we already have from this source.
Then use 'check_resource_exists' for any new resources you find to avoid duplicates.";
        }
        else
        {
            message += @"

Use the 'check_resource_exists' tool to verify each resource URL isn't already in our database.";
        }

        return message;
    }
}

