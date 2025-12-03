using Rsl.Llm.Models;

namespace Rsl.Llm.Services;

/// <summary>
/// Interface for the LLM-based ingestion agent.
/// </summary>
public interface IIngestionAgent
{
    /// <summary>
    /// Ingests learning resources from a source URL using an LLM agent.
    /// The agent will browse the URL, extract resources, and check for duplicates.
    /// </summary>
    /// <param name="sourceUrl">The URL to ingest resources from</param>
    /// <param name="sourceId">Optional source ID to associate resources with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing extracted resources and ingestion metadata</returns>
    Task<IngestionResult> IngestFromUrlAsync(
        string sourceUrl,
        Guid? sourceId = null,
        CancellationToken cancellationToken = default);
}

