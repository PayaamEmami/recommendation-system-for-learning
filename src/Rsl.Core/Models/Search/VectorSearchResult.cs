using Rsl.Core.Entities;

namespace Rsl.Core.Models;

/// <summary>
/// Result from a vector similarity search.
/// </summary>
public class VectorSearchResult
{
    /// <summary>
    /// Resource ID.
    /// </summary>
    public required Guid ResourceId { get; set; }

    /// <summary>
    /// Similarity score (typically 0.0 to 1.0, where 1.0 is most similar).
    /// </summary>
    public required double SimilarityScore { get; set; }

    /// <summary>
    /// Optional reference to the full resource entity (may be null if only IDs are returned).
    /// </summary>
    public Resource? Resource { get; set; }
}

