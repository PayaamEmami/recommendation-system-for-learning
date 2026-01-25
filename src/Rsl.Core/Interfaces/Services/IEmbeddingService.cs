namespace Rsl.Core.Interfaces;

/// <summary>
/// Service for generating embeddings from text.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for the given text.
    /// </summary>
    /// <param name="text">Input text to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector (float array)</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple texts in a batch.
    /// </summary>
    /// <param name="texts">Collection of texts to embed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of embedding vectors in the same order as input</returns>
    Task<IList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the dimensionality of the embeddings produced by this service.
    /// </summary>
    int Dimensions { get; }
}

