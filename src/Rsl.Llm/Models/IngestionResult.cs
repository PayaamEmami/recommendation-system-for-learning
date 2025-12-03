namespace Rsl.Llm.Models;

/// <summary>
/// Represents the result of an LLM-based source ingestion operation.
/// </summary>
public class IngestionResult
{
    /// <summary>
    /// Whether the ingestion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The source URL that was ingested.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Resources extracted from the source.
    /// </summary>
    public List<ExtractedResource> Resources { get; set; } = new();

    /// <summary>
    /// Error message if ingestion failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of resources found.
    /// </summary>
    public int TotalFound { get; set; }

    /// <summary>
    /// Number of new resources (not already in database).
    /// </summary>
    public int NewResources { get; set; }

    /// <summary>
    /// Number of resources that were duplicates.
    /// </summary>
    public int DuplicatesSkipped { get; set; }

    /// <summary>
    /// Timestamp of when the ingestion occurred.
    /// </summary>
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}

