namespace Rsl.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for embedding generation.
/// </summary>
public class EmbeddingSettings
{
    public const string SectionName = "Embedding";

    /// <summary>
    /// Azure OpenAI endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the deployed embedding model.
    /// </summary>
    public string DeploymentName { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Embedding dimensions (must match the model).
    /// </summary>
    public int Dimensions { get; set; } = 1536;

    /// <summary>
    /// Maximum number of texts to embed in a single batch.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;
}
