namespace Rsl.Llm.Services;

/// <summary>
/// Interface for LLM client operations.
/// Abstracts the underlying LLM provider (OpenAI, Claude, etc.) for flexibility.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Sends a message to the LLM with function/tool calling support.
    /// </summary>
    /// <param name="systemPrompt">System instructions for the LLM</param>
    /// <param name="userMessage">The user's message/request</param>
    /// <param name="tools">Optional list of tools the LLM can call</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The LLM's response</returns>
    Task<LlmResponse> SendMessageAsync(
        string systemPrompt,
        string userMessage,
        List<object>? tools = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message using the Responses API with web search enabled.
    /// This is specifically for URL ingestion where GPT needs to browse the web.
    /// </summary>
    /// <param name="systemPrompt">System instructions for the LLM</param>
    /// <param name="userMessage">The user's message/request</param>
    /// <param name="customTools">Optional list of custom tools (web_search is always included)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The LLM's response</returns>
    Task<LlmResponse> SendMessageWithWebSearchAsync(
        string systemPrompt,
        string userMessage,
        List<object>? customTools = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues a conversation by sending tool results back to the LLM.
    /// </summary>
    /// <param name="conversationHistory">Previous messages in the conversation</param>
    /// <param name="toolResults">Results from tool executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The LLM's response</returns>
    Task<LlmResponse> ContinueConversationAsync(
        List<object> conversationHistory,
        List<ToolResult> toolResults,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a response from the LLM.
/// </summary>
public class LlmResponse
{
    /// <summary>
    /// The text content of the response.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Tool/function calls requested by the LLM.
    /// </summary>
    public List<ToolCall> ToolCalls { get; set; } = new();

    /// <summary>
    /// Whether the LLM requested tool calls.
    /// </summary>
    public bool HasToolCalls => ToolCalls.Any();

    /// <summary>
    /// Whether this is the final response (no more tool calls needed).
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Full conversation history for continuing the conversation.
    /// </summary>
    public List<object> ConversationHistory { get; set; } = new();
}

/// <summary>
/// Represents a tool/function call requested by the LLM.
/// </summary>
public class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of executing a tool.
/// </summary>
public class ToolResult
{
    public string ToolCallId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}

