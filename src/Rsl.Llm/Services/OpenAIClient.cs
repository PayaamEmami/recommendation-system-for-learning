using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rsl.Llm.Configuration;

namespace Rsl.Llm.Services;

/// <summary>
/// Implementation of ILlmClient using OpenAI's API with function calling.
/// </summary>
public class OpenAIClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIClient> _logger;

    public OpenAIClient(
        HttpClient httpClient,
        IOptions<OpenAISettings> settings,
        ILogger<OpenAIClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient based on provider
        if (_settings.UseAzure)
        {
            // Azure OpenAI configuration
            if (string.IsNullOrEmpty(_settings.AzureEndpoint))
                throw new InvalidOperationException("AzureEndpoint is required when UseAzure is true");
            if (string.IsNullOrEmpty(_settings.AzureDeployment))
                throw new InvalidOperationException("AzureDeployment is required when UseAzure is true");

            var endpoint = _settings.AzureEndpoint.TrimEnd('/');
            _httpClient.BaseAddress = new Uri(endpoint);
            _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
        }
        else
        {
            // Standard OpenAI configuration
            var baseUrl = _settings.BaseUrl ?? "https://api.openai.com/v1/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task<LlmResponse> SendMessageAsync(
        string systemPrompt,
        string userMessage,
        List<object>? tools = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        return await CallApiAsync(messages, tools, cancellationToken);
    }

    /// <summary>
    /// Sends a message using the Responses API with web search enabled.
    /// This is specifically for URL ingestion where GPT needs to browse the web.
    /// </summary>
    public async Task<LlmResponse> SendMessageWithWebSearchAsync(
        string systemPrompt,
        string userMessage,
        List<object>? customTools = null,
        CancellationToken cancellationToken = default)
    {
        return await CallResponsesApiAsync(systemPrompt, userMessage, customTools, cancellationToken);
    }

    public async Task<LlmResponse> ContinueConversationAsync(
        List<object> conversationHistory,
        List<ToolResult> toolResults,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<object>(conversationHistory);

        // Add tool results to conversation
        foreach (var toolResult in toolResults)
        {
            messages.Add(new
            {
                role = "tool",
                tool_call_id = toolResult.ToolCallId,
                content = toolResult.Result
            });
        }

        return await CallApiAsync(messages, null, cancellationToken);
    }

    private async Task<LlmResponse> CallApiAsync(
        List<object> messages,
        List<object>? tools,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestBody = new Dictionary<string, object>
            {
                ["model"] = _settings.Model,
                ["messages"] = messages,
                ["temperature"] = _settings.Temperature,
                ["max_tokens"] = _settings.MaxTokens
            };

            if (tools != null && tools.Any())
            {
                requestBody["tools"] = tools;
                requestBody["tool_choice"] = "auto";
            }

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to OpenAI API with model {Model}", _settings.Model);

            // Build the correct endpoint URL
            string endpoint;
            if (_settings.UseAzure)
            {
                endpoint = $"/openai/deployments/{_settings.AzureDeployment}/chat/completions?api-version={_settings.AzureApiVersion}";
            }
            else
            {
                endpoint = "chat/completions";
            }

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"OpenAI API request failed: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var choice = result.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            var llmResponse = new LlmResponse
            {
                ConversationHistory = new List<object>(messages)
            };

            // Check if there are tool calls
            if (message.TryGetProperty("tool_calls", out var toolCalls))
            {
                foreach (var toolCall in toolCalls.EnumerateArray())
                {
                    var function = toolCall.GetProperty("function");
                    llmResponse.ToolCalls.Add(new ToolCall
                    {
                        Id = toolCall.GetProperty("id").GetString()!,
                        Name = function.GetProperty("name").GetString()!,
                        Arguments = function.GetProperty("arguments").GetString()!
                    });
                }

                // Add assistant's message with tool calls to history
                llmResponse.ConversationHistory.Add(JsonSerializer.Deserialize<object>(
                    message.GetRawText())!);

                llmResponse.IsComplete = false;
            }
            else
            {
                // Regular text response
                llmResponse.Content = message.GetProperty("content").GetString() ?? string.Empty;
                llmResponse.IsComplete = true;
            }

            return llmResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }

    /// <summary>
    /// Calls the OpenAI Responses API with web search enabled.
    /// This API has a different format than Chat Completions.
    /// </summary>
    private async Task<LlmResponse> CallResponsesApiAsync(
        string systemPrompt,
        string userMessage,
        List<object>? customTools,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build Responses API request body
            var requestBody = new Dictionary<string, object>
            {
                ["model"] = _settings.Model,
                ["input"] = new List<object>
                {
                    new
                    {
                        role = "user",
                        content = new List<object>
                        {
                            new
                            {
                                type = "input_text",
                                text = $"{systemPrompt}\n\n{userMessage}"
                            }
                        }
                    }
                },
                ["temperature"] = _settings.Temperature,
                ["max_tokens"] = _settings.MaxTokens
            };

            // Add tools: web_search + any custom tools
            var tools = new List<object>
            {
                new { type = "web_search" }
            };

            if (customTools != null && customTools.Any())
            {
                tools.AddRange(customTools);
            }

            requestBody["tools"] = tools;

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to OpenAI Responses API with model {Model} and web_search", _settings.Model);

            // Responses API endpoint
            var endpoint = "responses";
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI Responses API request failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"OpenAI Responses API request failed: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Responses API has different structure
            var output = result.GetProperty("output");
            var outputContent = output.GetProperty("content");

            var llmResponse = new LlmResponse
            {
                ConversationHistory = new List<object>(),
                IsComplete = true
            };

            // Extract text content from the response
            if (outputContent.ValueKind == JsonValueKind.Array)
            {
                var textParts = new List<string>();
                foreach (var item in outputContent.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) &&
                        type.GetString() == "output_text" &&
                        item.TryGetProperty("text", out var text))
                    {
                        textParts.Add(text.GetString()!);
                    }
                }
                llmResponse.Content = string.Join("\n", textParts);
            }
            else if (outputContent.TryGetProperty("text", out var textProp))
            {
                llmResponse.Content = textProp.GetString() ?? string.Empty;
            }

            _logger.LogInformation("Received response from OpenAI Responses API with {Length} characters",
                llmResponse.Content.Length);

            return llmResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI Responses API");
            throw;
        }
    }
}

