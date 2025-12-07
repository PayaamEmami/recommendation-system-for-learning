using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            var sourcesList = await sourceRepository.GetActiveSourcesAsync(cancellationToken);

            if (!sourcesList.Any())
            {
                _logger.LogInformation("No active sources to ingest");
                return;
            }

            _logger.LogInformation("Found {Count} active sources to process", sourcesList.Count);

            int totalIngested = 0;
            int totalEmbedded = 0;

            foreach (var source in sourcesList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Ingestion job cancelled");
                    break;
                }

                try
                {
                    _logger.LogInformation("Ingesting from source: {SourceName} ({SourceUrl})", source.Name, source.Url);

                    // Ingest resources using LLM agent
                    var ingestionResult = await ingestionAgent.IngestFromUrlAsync(
                        source.Url,
                        source.Id,
                        cancellationToken);

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

                    foreach (var extractedResource in ingestionResult.Resources)
                    {
                        try
                        {
                            // Check if resource already exists
                            var exists = await resourceRepository.ExistsByUrlAsync(
                                extractedResource.Url,
                                cancellationToken);

                            if (exists)
                            {
                                _logger.LogDebug("Resource already exists: {Url}", extractedResource.Url);
                                continue;
                            }

                            // Create resource entity (simplified - in real app, would map to specific types)
                            var resource = CreateResourceEntity(extractedResource, source.Id);
                            await resourceRepository.AddAsync(resource, cancellationToken);
                            newResources.Add(resource);

                            totalIngested++;
                            _logger.LogInformation("Saved new resource: {Title}", resource.Title);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving resource: {Title}", extractedResource.Title);
                        }
                    }

                    // Generate embeddings and upsert to vector store
                    if (newResources.Any())
                    {
                        await EmbedAndIndexResourcesAsync(
                            newResources,
                            embeddingService,
                            vectorStore,
                            cancellationToken);

                        totalEmbedded += newResources.Count;
                    }

                    _logger.LogInformation(
                        "Completed ingestion for source {SourceName}: {New} new resources",
                        source.Name,
                        newResources.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing source {SourceId}", source.Id);
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
            throw;
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
                PublishedDate = resource.PublishedDate,
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
    private Resource CreateResourceEntity(Llm.Models.ExtractedResource extracted, Guid sourceId)
    {
        // Map to appropriate resource type based on extracted.Type
        // For now, using a simple factory approach
        return extracted.Type switch
        {
            Core.Enums.ResourceType.Paper => new Paper
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                PublishedDate = extracted.PublishedDate,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Authors = string.IsNullOrEmpty(extracted.Author)
                    ? new List<string>()
                    : new List<string> { extracted.Author },
                DOI = extracted.DOI,
                Journal = extracted.Journal
            },
            Core.Enums.ResourceType.Video => new Video
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                PublishedDate = extracted.PublishedDate,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Channel = extracted.Channel,
                Duration = !string.IsNullOrEmpty(extracted.Duration) && TimeSpan.TryParse(extracted.Duration, out var duration)
                    ? duration
                    : null,
                ThumbnailUrl = extracted.ThumbnailUrl
            },
            Core.Enums.ResourceType.BlogPost => new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                PublishedDate = extracted.PublishedDate,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Author = extracted.Author
            },
            Core.Enums.ResourceType.CurrentEvent => new CurrentEvent
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                PublishedDate = extracted.PublishedDate,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            Core.Enums.ResourceType.SocialMediaPost => new SocialMediaPost
            {
                Id = Guid.NewGuid(),
                Title = extracted.Title,
                Description = extracted.Description,
                Url = extracted.Url,
                PublishedDate = extracted.PublishedDate,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Username = extracted.Author
            },
            _ => throw new ArgumentException($"Unknown resource type: {extracted.Type}")
        };
    }
}
