using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;

namespace Rsl.Jobs.Jobs;

/// <summary>
/// One-time job to reindex all existing resources in the vector store.
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
    /// Reindex all resources in the vector store.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting reindex job");

        using var scope = _serviceProvider.CreateScope();
        var resourceRepository = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        try
        {
            // Get all resources from database
            var allResources = (await resourceRepository.GetAllAsync(cancellationToken)).ToList();

            if (!allResources.Any())
            {
                _logger.LogInformation("No resources found to reindex");
                return;
            }

            _logger.LogInformation("Found {Count} resources to reindex", allResources.Count);

            int totalReindexed = 0;
            var batches = allResources.Chunk(BatchSize).ToList();

            for (int i = 0; i < batches.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Reindex job cancelled");
                    break;
                }

                var batch = batches[i].ToList();
                _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({Count} resources)",
                    i + 1, batches.Count, batch.Count);

                try
                {
                    // Generate embeddings for the batch
                    var texts = batch
                        .Select(r => $"{r.Title} {r.Description}".Trim())
                        .ToList();

                    var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

                    // Create resource documents with correct publishedDate
                    var documents = batch.Zip(embeddings, (resource, embedding) => new ResourceDocument
                    {
                        Id = resource.Id,
                        Title = resource.Title,
                        Description = resource.Description,
                        Url = resource.Url,
                        Type = resource.Type,
                        SourceId = resource.SourceId,
                        PublishedDate = resource.CreatedAt, // Set publishedDate to CreatedAt
                        CreatedAt = resource.CreatedAt,
                        UpdatedAt = resource.UpdatedAt,
                        Embedding = embedding
                    }).ToList();

                    // Upsert to vector store
                    await vectorStore.UpsertDocumentsAsync(documents, cancellationToken);

                    totalReindexed += documents.Count;
                    _logger.LogInformation("Batch {BatchNumber} completed: {Count} resources reindexed",
                        i + 1, documents.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch {BatchNumber}", i + 1);
                    // Continue with next batch
                }
            }

            _logger.LogInformation("Reindex job completed: {Total} resources reindexed", totalReindexed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in reindex job");
            throw;
        }
    }
}
