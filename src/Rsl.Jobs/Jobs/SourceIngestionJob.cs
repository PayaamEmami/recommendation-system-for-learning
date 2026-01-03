using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using Rsl.Core.Entities;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Llm.Services;

namespace Rsl.Jobs.Jobs;

/// <summary>
/// Background job that periodically ingests resources from all active sources.
/// </summary>
public class SourceIngestionJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SourceIngestionJob> _logger;
    private const int BatchSize = 5;
    private static readonly TimeSpan PerSourceTimeout = TimeSpan.FromSeconds(120);

    public SourceIngestionJob(
        IServiceProvider serviceProvider,
        ILogger<SourceIngestionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Execute the source ingestion job for all active sources.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting source ingestion job");

        using var scope = _serviceProvider.CreateScope();
        var sourceRepository = scope.ServiceProvider.GetRequiredService<ISourceRepository>();
        var resourceRepository = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
        var ingestionAgent = scope.ServiceProvider.GetRequiredService<IIngestionAgent>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        try
        {
            // Get all active sources
            var sourcesList = (await sourceRepository.GetActiveSourcesAsync(cancellationToken)).ToList();

            if (!sourcesList.Any())
            {
                _logger.LogInformation("No active sources to ingest");
                return;
            }

            _logger.LogInformation("Found {Count} active sources to process (batch size {BatchSize})", sourcesList.Count, BatchSize);

            int totalIngested = 0;
            int totalEmbedded = 0;

            var batches = sourcesList.Chunk(BatchSize).ToList();
            int batchNumber = 0;

            foreach (var batch in batches)
            {
                batchNumber++;
                _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} with {BatchCount} sources", batchNumber, batches.Count, batch.Length);

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Ingestion job cancelled");
                    break;
                }

                foreach (var source in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Ingestion job cancelled during batch");
                        break;
                    }

                    using var perSourceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    perSourceCts.CancelAfter(PerSourceTimeout);

                    try
                    {
                        _logger.LogInformation("Ingesting from source: {SourceName} ({SourceUrl})", source.Name, source.Url);
                        var stopwatch = Stopwatch.StartNew();

                        // Ingest resources using LLM agent
                        var ingestionResult = await ingestionAgent.IngestFromUrlAsync(
                            source.Url,
                            source.Id,
                            perSourceCts.Token);

                        if (!ingestionResult.Success)
                        {
                            _logger.LogWarning(
                                "Failed to ingest from source {SourceId}: {Error}",
                                source.Id,
                                ingestionResult.ErrorMessage);
                            continue;
                        }

                        // Save new resources and generate embeddings
                        var newResources = new List<Resource>();
                        int duplicateCount = 0;
                        int errorCount = 0;

                        _logger.LogInformation("Attempting to save {Count} extracted resources from {SourceName}",
                            ingestionResult.Resources.Count, source.Name);

                        foreach (var extractedResource in ingestionResult.Resources)
                        {
                            try
                            {
                                _logger.LogInformation("Processing: {Title} (Type: {Type}, URL: {Url})",
                                    extractedResource.Title, extractedResource.Type, extractedResource.Url);

                                // Validate URL is not empty
                                if (string.IsNullOrWhiteSpace(extractedResource.Url))
                                {
                                    errorCount++;
                                    _logger.LogWarning("Skipping resource with empty URL: {Title}", extractedResource.Title);
                                    continue;
                                }

                                // Check for duplicate URL before adding to context
                                if (await resourceRepository.ExistsByUrlAsync(extractedResource.Url, perSourceCts.Token))
                                {
                                    duplicateCount++;
                                    _logger.LogInformation("Duplicate URL found: {Title} (URL: {Url})", extractedResource.Title, extractedResource.Url);
                                    continue;
                                }

                                // Create resource entity (fallback to source category if LLM omitted type)
                                var resource = CreateResourceEntity(extractedResource, source.Id, source.Category);

                                _logger.LogInformation("Attempting to save: {Title} (Type: {Type}, URL: {Url})",
                                    resource.Title, resource.Type, resource.Url);

                                await resourceRepository.AddAsync(resource, perSourceCts.Token);
                                newResources.Add(resource);

                                totalIngested++;
                                _logger.LogInformation("Successfully saved: {Title} (Type: {Type})", resource.Title, resource.Type);
                            }
                            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                                                               ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true ||
                                                               ex.InnerException?.Message.Contains("UNIQUE", StringComparison.Ordinal) == true)
                            {
                                // Resource URL already exists - this is a race condition edge case, just skip it
                                duplicateCount++;
                                _logger.LogInformation("Duplicate (race condition): {Title} (URL: {Url})", extractedResource.Title, extractedResource.Url);
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                _logger.LogError(ex, "Error saving: {Title} (Type: {Type}, URL: {Url}) - {Error}",
                                    extractedResource.Title, extractedResource.Type, extractedResource.Url, ex.Message);
                            }
                        }

                        // Generate embeddings and upsert to vector store
                        if (newResources.Any())
                        {
                            await EmbedAndIndexResourcesAsync(
                                newResources,
                                embeddingService,
                                vectorStore,
                                perSourceCts.Token);

                            totalEmbedded += newResources.Count;
                        }

                        stopwatch.Stop();
                        _logger.LogInformation(
                            "Completed {SourceName}: {Extracted} extracted, {New} saved, {Duplicates} duplicates, {Errors} errors in {ElapsedMs} ms",
                            source.Name,
                            ingestionResult.Resources.Count,
                            newResources.Count,
                            duplicateCount,
                            errorCount,
                            stopwatch.ElapsedMilliseconds);
                    }
                    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Ingestion timed out for source {SourceName} after {TimeoutSeconds}s", source.Name, PerSourceTimeout.TotalSeconds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing source {SourceId}", source.Id);
                    }
                }
            }

            _logger.LogInformation(
                "Source ingestion job completed: {Ingested} resources ingested, {Embedded} embedded",
                totalIngested,
                totalEmbedded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in source ingestion job");
            // Swallow to avoid tight retry loops; individual source errors are already handled.
            // The worker will log and retry on its normal schedule.
        }
    }

    /// <summary>
    /// Generate embeddings and index resources in vector store.
    /// </summary>
    private async Task EmbedAndIndexResourcesAsync(
        List<Resource> resources,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate embeddings for all resources
            var texts = resources
                .Select(r => $"{r.Title} {r.Description}".Trim())
                .ToList();

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

            // Create resource documents
            var documents = resources.Zip(embeddings, (resource, embedding) => new ResourceDocument
            {
                Id = resource.Id,
                Title = resource.Title,
                Description = resource.Description,
                Url = resource.Url,
                Type = resource.Type,
                SourceId = resource.SourceId,
                PublishedDate = resource.CreatedAt, // Use CreatedAt as the published date for filtering
                CreatedAt = resource.CreatedAt,
                UpdatedAt = resource.UpdatedAt,
                Embedding = embedding
            }).ToList();

            // Upsert to vector store
            await vectorStore.UpsertDocumentsAsync(documents, cancellationToken);

            _logger.LogInformation("Embedded and indexed {Count} resources", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error embedding and indexing resources");
            throw;
        }
    }

    /// <summary>
    /// Create a resource entity from extracted resource data.
    /// Simplified version - in real app, would map to specific resource types.
    /// </summary>
    private Resource CreateResourceEntity(Llm.Models.ExtractedResource extracted, Guid sourceId, Core.Enums.ResourceType sourceCategory)
    {
        // Prefer extracted type; if missing/default, fall back to source category
        var resourceType = extracted.Type != default ? extracted.Type : sourceCategory;

        // Map to appropriate resource type based on extracted.Type
        // For now, using a simple factory approach
        return resourceType switch
        {
            Core.Enums.ResourceType.Paper => new Paper
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            Core.Enums.ResourceType.Video => new Video
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            Core.Enums.ResourceType.BlogPost => new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            _ => throw new ArgumentException($"Unknown resource type: {extracted.Type}")
        };
    }
}
