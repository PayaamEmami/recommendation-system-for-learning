namespace Rsl.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for embedding generation using OpenAI.
/// </summary>
public class EmbeddingSettings
{
    public const string SectionName = "Embedding";

    /// <summary>
    /// OpenAI API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name for OpenAI embeddings (e.g., "text-embedding-3-small").
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
