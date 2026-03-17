using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Crs.Core.Interfaces;
using Crs.Core.Models;

namespace Crs.Jobs.Jobs;

/// <summary>
/// One-time job to reindex all existing content in the vector store.
/// Use this to fix missing publishedDate or other index schema changes.
/// </summary>
public class ReindexJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReindexJob> _logger;
    private const int BatchSize = 50;

    public ReindexJob(
        IServiceProvider serviceProvider,
        ILogger<ReindexJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Reindex all content in the vector store.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting reindex job");

        using var scope = _serviceProvider.CreateScope();
        var contentRepository = scope.ServiceProvider.GetRequiredService<IContentRepository>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        try
        {
            // Get all content from database
            var allContent = (await contentRepository.GetAllAsync(cancellationToken)).ToList();

            if (!allContent.Any())
            {
                _logger.LogInformation("No content found to reindex");
                return;
            }

            _logger.LogInformation("Found {Count} content to reindex", allContent.Count);

            int totalReindexed = 0;
            var batches = allContent.Chunk(BatchSize).ToList();

            for (int i = 0; i < batches.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Reindex job cancelled");
                    break;
                }

                var batch = batches[i].ToList();
                _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({Count} content)",
                    i + 1, batches.Count, batch.Count);

                try
                {
                    // Generate embeddings for the batch
                    var texts = batch
                        .Select(r => $"{r.Title} {r.Description}".Trim())
                        .ToList();

                    var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

                    // Create content documents with correct publishedDate
                    var documents = batch.Zip(embeddings, (content, embedding) => new ContentDocument
                    {
                        Id = content.Id,
                        Title = content.Title,
                        Description = content.Description,
                        Url = content.Url,
                        Type = content.Type,
                        SourceId = content.SourceId,
                        PublishedDate = content.CreatedAt, // Set publishedDate to CreatedAt
                        CreatedAt = content.CreatedAt,
                        UpdatedAt = content.UpdatedAt,
                        Embedding = embedding
                    }).ToList();

                    // Upsert to vector store
                    await vectorStore.UpsertDocumentsAsync(documents, cancellationToken);

                    totalReindexed += documents.Count;
                    _logger.LogInformation("Batch {BatchNumber} completed: {Count} content reindexed",
                        i + 1, documents.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch {BatchNumber}", i + 1);
                    // Continue with next batch
                }
            }

            _logger.LogInformation("Reindex job completed: {Total} content reindexed", totalReindexed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in reindex job");
            throw;
        }
    }
}
