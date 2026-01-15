namespace Rsl.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for AWS OpenSearch Serverless.
/// </summary>
public class OpenSearchSettings
{
    public const string SectionName = "OpenSearch";

    /// <summary>
    /// OpenSearch Serverless collection endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Name of the index for resource vectors.
    /// </summary>
    public string IndexName { get; set; } = "rsl-resources";

    /// <summary>
    /// Embedding model dimensions (default for text-embedding-3-small).
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// AWS Region for OpenSearch Serverless.
    /// </summary>
    public string Region { get; set; } = "us-west-2";
}
