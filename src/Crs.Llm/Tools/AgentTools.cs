using System.Text.Json;
using Crs.Core.Interfaces;

namespace Crs.Llm.Tools;

/// <summary>
/// Provides tools that the LLM agent can use during ingestion.
/// These tools give the agent access to database queries and other system functionality.
/// </summary>
public class AgentTools
{
    private readonly IContentRepository _contentRepository;
    private readonly ISourceRepository _sourceRepository;

    public AgentTools(
        IContentRepository contentRepository,
        ISourceRepository sourceRepository)
    {
        _contentRepository = contentRepository;
        _sourceRepository = sourceRepository;
    }

    /// <summary>
    /// Gets the tool definitions in the format expected by OpenAI's function calling.
    /// </summary>
    public List<object> GetToolDefinitions()
    {
        return new List<object>
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "check_content_exists",
                    description = "Check if a content URL already exists in the database to avoid duplicates.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            url = new
                            {
                                type = "string",
                                description = "The URL of the content to check"
                            }
                        },
                        required = new[] { "url" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_content_from_source",
                    description = "Get all content that has already been ingested from a specific source URL.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            sourceId = new
                            {
                                type = "string",
                                description = "The ID of the source to get content from"
                            }
                        },
                        required = new[] { "sourceId" }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Executes a tool based on the tool name and arguments provided by the LLM.
    /// </summary>
    public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);

            return toolName switch
            {
                "check_content_exists" => await CheckContentExistsAsync(
                    arguments!["url"].GetString()!, cancellationToken),

                "get_content_from_source" => await GetContentFromSourceAsync(
                    Guid.Parse(arguments!["sourceId"].GetString()!), cancellationToken),

                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> CheckContentExistsAsync(string url, CancellationToken cancellationToken)
    {
        var exists = await _contentRepository.ExistsByUrlAsync(url, cancellationToken);
        return JsonSerializer.Serialize(new { exists, url });
    }

    private async Task<string> GetContentFromSourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);

        if (source == null)
        {
            return JsonSerializer.Serialize(new { error = "Source not found" });
        }

        var content = source.Content.Select(r => new
        {
            r.Id,
            r.Title,
            r.Url,
            Type = r.Type.ToString()
        });

        return JsonSerializer.Serialize(new { sourceId, count = content.Count(), content });
    }
}
