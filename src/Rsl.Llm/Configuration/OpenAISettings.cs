namespace Rsl.Llm.Configuration;

/// <summary>
/// Configuration settings for OpenAI API integration.
/// </summary>
public class OpenAISettings
{
    /// <summary>
    /// OpenAI API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The model to use (e.g., "gpt-4o", "gpt-4o-mini").
    /// </summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Maximum tokens per request to control costs.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for response generation (0-2). Lower = more focused/deterministic.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Base URL for the OpenAI API (allows switching to Azure OpenAI or other providers).
    /// </summary>
    public string? BaseUrl { get; set; }
}

