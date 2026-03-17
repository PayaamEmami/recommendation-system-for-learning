using Crs.Core.Enums;

namespace Crs.Core.Models;

/// <summary>
/// Request for vector similarity search.
/// </summary>
public class VectorSearchRequest
{
    /// <summary>
    /// Query vector to search for similar items.
    /// </summary>
    public required float[] QueryVector { get; set; }

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int TopK { get; set; } = 10;

    /// <summary>
    /// Filter by content type (optional).
    /// </summary>
    public ContentType? ContentType { get; set; }

    /// <summary>
    /// Filter by source IDs (optional).
    /// </summary>
    public HashSet<Guid>? SourceIds { get; set; }

    /// <summary>
    /// Filter to content published after this date (optional).
    /// </summary>
    public DateTime? PublishedAfter { get; set; }

    /// <summary>
    /// Filter to content published before this date (optional).
    /// </summary>
    public DateTime? PublishedBefore { get; set; }

    /// <summary>
    /// Exclude these content IDs from results (optional).
    /// </summary>
    public HashSet<Guid>? ExcludeContentIds { get; set; }

    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0, optional).
    /// </summary>
    public double? MinimumScore { get; set; }
}

