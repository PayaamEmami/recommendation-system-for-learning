namespace Crs.Llm.Models;

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
    /// Content extracted from the source.
    /// </summary>
    public List<ExtractedContent> Content { get; set; } = new();

    /// <summary>
    /// Error message if ingestion failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of content found.
    /// </summary>
    public int TotalFound { get; set; }

    /// <summary>
    /// Number of new content (not already in database).
    /// </summary>
    public int NewContent { get; set; }

    /// <summary>
    /// Number of content that were duplicates.
    /// </summary>
    public int DuplicatesSkipped { get; set; }

    /// <summary>
    /// Timestamp of when the ingestion occurred.
    /// </summary>
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}

