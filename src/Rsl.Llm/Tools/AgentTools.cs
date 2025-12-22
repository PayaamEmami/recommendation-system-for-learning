using System.Text.Json;
using Rsl.Core.Interfaces;

namespace Rsl.Llm.Tools;

/// <summary>
/// Provides tools that the LLM agent can use during ingestion.
/// These tools give the agent access to database queries and other system functionality.
/// </summary>
public class AgentTools
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ISourceRepository _sourceRepository;

    public AgentTools(
        IResourceRepository resourceRepository,
        ISourceRepository sourceRepository)
    {
        _resourceRepository = resourceRepository;
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
                    name = "check_resource_exists",
                    description = "Check if a resource URL already exists in the database to avoid duplicates.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            url = new
                            {
                                type = "string",
                                description = "The URL of the resource to check"
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
                    name = "get_resources_from_source",
                    description = "Get all resources that have already been ingested from a specific source URL.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            sourceId = new
                            {
                                type = "string",
                                description = "The ID of the source to get resources from"
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
                "check_resource_exists" => await CheckResourceExistsAsync(
                    arguments!["url"].GetString()!, cancellationToken),

                "get_resources_from_source" => await GetResourcesFromSourceAsync(
                    Guid.Parse(arguments!["sourceId"].GetString()!), cancellationToken),

                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> CheckResourceExistsAsync(string url, CancellationToken cancellationToken)
    {
        var exists = await _resourceRepository.ExistsByUrlAsync(url, cancellationToken);
        return JsonSerializer.Serialize(new { exists, url });
    }

    private async Task<string> GetResourcesFromSourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);

        if (source == null)
        {
            return JsonSerializer.Serialize(new { error = "Source not found" });
        }

        var resources = source.Resources.Select(r => new
        {
            r.Id,
            r.Title,
            r.Url,
            Type = r.Type.ToString()
        });

        return JsonSerializer.Serialize(new { sourceId, count = resources.Count(), resources });
    }
}

