using Crs.Core.Models;

namespace Crs.Core.Interfaces;

/// <summary>
/// Vector store for semantic similarity search on content.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Initialize or ensure the vector index exists.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update a single content document in the vector index.
    /// </summary>
    /// <param name="document">Content document with embedding</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertDocumentAsync(ContentDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update multiple content documents in the vector index.
    /// </summary>
    /// <param name="documents">Collection of content documents with embeddings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertDocumentsAsync(IEnumerable<ContentDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a content document from the vector index.
    /// </summary>
    /// <param name="contentId">Content ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteDocumentAsync(Guid contentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar content using vector similarity.
    /// </summary>
    /// <param name="request">Search request with query vector and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of search results with similarity scores</returns>
    Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the total count of documents in the vector index.
    /// </summary>
    Task<long> GetDocumentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all content IDs currently stored in the vector index.
    /// Useful for reconciliation when the vector index is treated as a rebuildable cache.
    /// </summary>
    Task<HashSet<Guid>> GetAllDocumentIdsAsync(CancellationToken cancellationToken = default);
}
