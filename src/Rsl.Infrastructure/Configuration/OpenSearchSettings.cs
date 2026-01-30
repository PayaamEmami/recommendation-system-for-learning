namespace Rsl.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for OpenSearch.
/// </summary>
public class OpenSearchSettings
{
    public const string SectionName = "OpenSearch";

    /// <summary>
    /// Determines which OpenSearch mode to use.
    /// </summary>
    public OpenSearchMode Mode { get; set; } = OpenSearchMode.Local;

    /// <summary>
    /// OpenSearch endpoint URL.
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
    /// AWS Region for OpenSearch (used when Mode is Aws).
    /// </summary>
    public string Region { get; set; } = "us-west-2";
}

public enum OpenSearchMode
{
    Local,
    Aws
}
