using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using Crs.Core.Entities;
using Crs.Core.Interfaces;
using Crs.Core.Models;
using Crs.Llm.Services;
using Crs.Jobs.Validation;

namespace Crs.Jobs.Jobs;

/// <summary>
/// Background job that periodically ingests content from all active sources.
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
        var contentRepository = scope.ServiceProvider.GetRequiredService<IContentRepository>();
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

                        // Ingest content using LLM agent
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

                        // Save new content and generate embeddings
                        var newContent = new List<Content>();
                        int duplicateCount = 0;
                        int errorCount = 0;

                        _logger.LogInformation("Attempting to save {Count} extracted content from {SourceName}",
                            ingestionResult.Content.Count, source.Name);

                        foreach (var extractedContent in ingestionResult.Content)
                        {
                            try
                            {
                                _logger.LogInformation("Processing: {Title} (Type: {Type}, URL: {Url})",
                                    extractedContent.Title, extractedContent.Type, extractedContent.Url);

                                // Validate URL is not empty
                                if (string.IsNullOrWhiteSpace(extractedContent.Url))
                                {
                                    errorCount++;
                                    _logger.LogWarning("Skipping content with empty URL: {Title}", extractedContent.Title);
                                    continue;
                                }

                                if (!ContentUrlPolicy.IsLikelyContentUrl(extractedContent.Url, extractedContent.Type, source.Url))
                                {
                                    _logger.LogInformation("Skipping non-content URL: {Title} (URL: {Url})",
                                        extractedContent.Title,
                                        extractedContent.Url);
                                    continue;
                                }

                                // Check for duplicate URL before adding to context
                                if (await contentRepository.ExistsByUrlAsync(extractedContent.Url, perSourceCts.Token))
                                {
                                    duplicateCount++;
                                    _logger.LogInformation("Duplicate URL found: {Title} (URL: {Url})", extractedContent.Title, extractedContent.Url);
                                    continue;
                                }

                                // Create content entity (fallback to source category if LLM omitted type)
                                var content = CreateContentEntity(extractedContent, source.Id, source.Category);

                                _logger.LogInformation("Attempting to save: {Title} (Type: {Type}, URL: {Url})",
                                    content.Title, content.Type, content.Url);

                                await contentRepository.AddAsync(content, perSourceCts.Token);
                                newContent.Add(content);

                                totalIngested++;
                                _logger.LogInformation("Successfully saved: {Title} (Type: {Type})", content.Title, content.Type);
                            }
                            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                                                               ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true ||
                                                               ex.InnerException?.Message.Contains("UNIQUE", StringComparison.Ordinal) == true)
                            {
                                // Content URL already exists - this is a race condition edge case, just skip it
                                duplicateCount++;
                                _logger.LogInformation("Duplicate (race condition): {Title} (URL: {Url})", extractedContent.Title, extractedContent.Url);
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                _logger.LogError(ex, "Error saving: {Title} (Type: {Type}, URL: {Url}) - {Error}",
                                    extractedContent.Title, extractedContent.Type, extractedContent.Url, ex.Message);
                            }
                        }

                        // Generate embeddings and upsert to vector store
                        if (newContent.Any())
                        {
                            await EmbedAndIndexContentAsync(
                                newContent,
                                embeddingService,
                                vectorStore,
                                perSourceCts.Token);

                            totalEmbedded += newContent.Count;
                        }

                        stopwatch.Stop();
                        _logger.LogInformation(
                            "Completed {SourceName}: {Extracted} extracted, {New} saved, {Duplicates} duplicates, {Errors} errors in {ElapsedMs} ms",
                            source.Name,
                            ingestionResult.Content.Count,
                            newContent.Count,
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
                "Source ingestion job completed: {Ingested} content ingested, {Embedded} embedded",
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
    /// Generate embeddings and index content in vector store.
    /// </summary>
    private async Task EmbedAndIndexContentAsync(
        List<Content> content,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate embeddings for all content
            var texts = content
                .Select(r => $"{r.Title} {r.Description}".Trim())
                .ToList();

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

            // Create content documents
            var documents = content.Zip(embeddings, (content, embedding) => new ContentDocument
            {
                Id = content.Id,
                Title = content.Title,
                Description = content.Description,
                Url = content.Url,
                Type = content.Type,
                SourceId = content.SourceId,
                PublishedDate = content.CreatedAt, // Use CreatedAt as the published date for filtering
                CreatedAt = content.CreatedAt,
                UpdatedAt = content.UpdatedAt,
                Embedding = embedding
            }).ToList();

            // Upsert to vector store
            await vectorStore.UpsertDocumentsAsync(documents, cancellationToken);

            _logger.LogInformation("Embedded and indexed {Count} content", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error embedding and indexing content");
            throw;
        }
    }

    /// <summary>
    /// Create a content entity from extracted content data.
    /// Simplified version - in real app, would map to specific content types.
    /// </summary>
    private Content CreateContentEntity(Llm.Models.ExtractedContent extracted, Guid sourceId, Core.Enums.ContentType sourceCategory)
    {
        // Prefer extracted type; if missing/default, fall back to source category
        var contentType = extracted.Type != default ? extracted.Type : sourceCategory;

        // Map to appropriate content type based on extracted.Type
        // For now, using a simple factory approach
        return contentType switch
        {
            Core.Enums.ContentType.Paper => new Paper
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            Core.Enums.ContentType.Video => new Video
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            Core.Enums.ContentType.BlogPost => new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            _ => throw new ArgumentException($"Unknown content type: {extracted.Type}")
        };
    }
}
