namespace Rsl.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for embedding generation.
/// </summary>
public class EmbeddingSettings
{
    public const string SectionName = "Embedding";

    /// <summary>
    /// Whether to use Azure OpenAI instead of direct OpenAI.
    /// </summary>
    public bool UseAzure { get; set; } = false;

    /// <summary>
    /// Azure OpenAI endpoint URL (only used when UseAzure is true).
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// OpenAI API key (for both Azure and direct OpenAI).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the deployed embedding model (Azure) or model name (direct OpenAI).
    /// For Azure: deployment name (e.g., "text-embedding-3-small")
    /// For OpenAI: model name (e.g., "text-embedding-3-small")
    /// </summary>
    public string DeploymentName { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Model name for direct OpenAI (e.g., "text-embedding-3-small").
    /// </summary>
    public string ModelName { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Embedding dimensions (must match the model).
    /// </summary>
    public int Dimensions { get; set; } = 1536;

    /// <summary>
    /// Maximum number of texts to embed in a single batch.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;
}
