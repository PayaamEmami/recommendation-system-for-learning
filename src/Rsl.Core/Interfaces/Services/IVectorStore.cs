using Rsl.Core.Models;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Vector store for semantic similarity search on resources.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Initialize or ensure the vector index exists.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update a single resource document in the vector index.
    /// </summary>
    /// <param name="document">Resource document with embedding</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertDocumentAsync(ResourceDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update multiple resource documents in the vector index.
    /// </summary>
    /// <param name="documents">Collection of resource documents with embeddings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertDocumentsAsync(IEnumerable<ResourceDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a resource document from the vector index.
    /// </summary>
    /// <param name="resourceId">Resource ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteDocumentAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar resources using vector similarity.
    /// </summary>
    /// <param name="request">Search request with query vector and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of search results with similarity scores</returns>
    Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the total count of documents in the vector index.
    /// </summary>
    Task<long> GetDocumentCountAsync(CancellationToken cancellationToken = default);
}

