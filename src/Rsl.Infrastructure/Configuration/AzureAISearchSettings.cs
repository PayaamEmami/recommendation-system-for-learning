namespace Rsl.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Azure AI Search.
/// </summary>
public class AzureAISearchSettings
{
    public const string SectionName = "AzureAISearch";

    /// <summary>
    /// Azure AI Search service endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Search API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the index for resource vectors.
    /// </summary>
    public string IndexName { get; set; } = "rsl-resources";

    /// <summary>
    /// Embedding model dimensions (default for text-embedding-3-small).
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;
}
