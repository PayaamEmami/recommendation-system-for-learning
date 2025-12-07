using Rsl.Core.Enums;

namespace Rsl.Core.Models;

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
    /// Filter by resource type (optional).
    /// </summary>
    public ResourceType? ResourceType { get; set; }

    /// <summary>
    /// Filter by source IDs (optional).
    /// </summary>
    public HashSet<Guid>? SourceIds { get; set; }

    /// <summary>
    /// Filter to resources published after this date (optional).
    /// </summary>
    public DateTime? PublishedAfter { get; set; }

    /// <summary>
    /// Filter to resources published before this date (optional).
    /// </summary>
    public DateTime? PublishedBefore { get; set; }

    /// <summary>
    /// Exclude these resource IDs from results (optional).
    /// </summary>
    public HashSet<Guid>? ExcludeResourceIds { get; set; }

    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0, optional).
    /// </summary>
    public double? MinimumScore { get; set; }
}
