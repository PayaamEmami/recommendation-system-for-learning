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
            // Some OpenAI models (e.g., gpt-5-nano) only accept the default temperature (1).
            var temperature = _settings.Temperature;
            if (!_settings.UseAzure &&
                string.Equals(_settings.Model, "gpt-5-nano", StringComparison.OrdinalIgnoreCase))
            {
                temperature = 1;
            }

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = _settings.Model,
                ["messages"] = messages,
                ["temperature"] = temperature
            };

            // OpenAI chat API (non-Azure) uses max_completion_tokens; Azure still expects max_tokens
            if (_settings.UseAzure)
            {
                requestBody["max_tokens"] = _settings.MaxTokens;
            }
            else
            {
                requestBody["max_completion_tokens"] = _settings.MaxTokens;
            }

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

}

