using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Infrastructure.Configuration;

namespace Rsl.Infrastructure.VectorStore;

/// <summary>
/// AWS OpenSearch Serverless implementation of vector store.
/// </summary>
public class OpenSearchVectorStore : IVectorStore
{
    private readonly OpenSearchClient _client;
    private readonly OpenSearchSettings _settings;
    private readonly ILogger<OpenSearchVectorStore> _logger;

    public OpenSearchVectorStore(
        IOptions<OpenSearchSettings> settings,
        ILogger<OpenSearchVectorStore> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Configure OpenSearch client
        var endpoint = new Uri(_settings.Endpoint);
        var pool = new SingleNodeConnectionPool(endpoint);
        var connectionSettings = new ConnectionSettings(pool)
            .DefaultIndex(_settings.IndexName)
            .RequestTimeout(TimeSpan.FromMinutes(2))
            .DisableDirectStreaming(); // Helps with debugging

        _client = new OpenSearchClient(connectionSettings);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing OpenSearch index: {IndexName}", _settings.IndexName);

            var indexExists = await _client.Indices.ExistsAsync(_settings.IndexName, ct: cancellationToken);

            if (!indexExists.Exists)
            {
                var createIndexResponse = await _client.Indices.CreateAsync(_settings.IndexName, c => c
                    .Settings(s => s
                        .Setting("index.knn", true)
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                    )
                    .Map<ResourceSearchDocument>(m => m
                        .Properties(p => p
                            .Keyword(k => k.Name(n => n.Id))
                            .Text(t => t.Name(n => n.Title))
                            .Text(t => t.Name(n => n.Description))
                            .Keyword(k => k.Name(n => n.Url))
                            .Keyword(k => k.Name(n => n.Type))
                            .Keyword(k => k.Name(n => n.SourceId))
                            .Date(d => d.Name(n => n.PublishedDate))
                            .Date(d => d.Name(n => n.CreatedAt))
                            .Date(d => d.Name(n => n.UpdatedAt))
                            .KnnVector(knn => knn
                                .Name(n => n.Embedding)
                                .Dimension(_settings.EmbeddingDimensions)
                                .Method(m => m
                                    .Name("hnsw")
                                    .SpaceType("cosinesimil")
                                    .Engine("nmslib")
                                    .Parameters(p => p
                                        .Parameter("ef_construction", 512)
                                        .Parameter("m", 16)
                                    )
                                )
                            )
                        )
                    ),
                    cancellationToken
                );

                if (!createIndexResponse.IsValid)
                {
                    _logger.LogError("Failed to create index: {Error}", createIndexResponse.DebugInformation);
                    throw new Exception($"Failed to create OpenSearch index: {createIndexResponse.DebugInformation}");
                }

                _logger.LogInformation("Successfully created index: {IndexName}", _settings.IndexName);
            }
            else
            {
                _logger.LogInformation("Index already exists: {IndexName}", _settings.IndexName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing OpenSearch index");
            throw;
        }
    }

    public async Task UpsertDocumentAsync(ResourceDocument document, CancellationToken cancellationToken = default)
    {
        await UpsertDocumentsAsync(new[] { document }, cancellationToken);
    }

    public async Task UpsertDocumentsAsync(
        IEnumerable<ResourceDocument> documents,
        CancellationToken cancellationToken = default)
    {
        var documentsList = documents.ToList();
        if (!documentsList.Any())
        {
            return;
        }

        try
        {
            var searchDocuments = documentsList.Select(ToSearchDocument).ToList();

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(_settings.IndexName)
                .IndexMany(searchDocuments, (descriptor, doc) => descriptor.Id(doc.Id)),
                cancellationToken
            );

            if (!bulkResponse.IsValid)
            {
                _logger.LogError("Bulk upsert failed: {Error}", bulkResponse.DebugInformation);
            }

            var successCount = bulkResponse.Items.Count(i => i.IsValid);
            var failureCount = bulkResponse.Items.Count(i => !i.IsValid);

            _logger.LogInformation(
                "Upserted {Count} documents to index, {Succeeded} succeeded, {Failed} failed",
                documentsList.Count,
                successCount,
                failureCount);

            foreach (var item in bulkResponse.Items.Where(i => !i.IsValid))
            {
                _logger.LogWarning(
                    "Failed to index document {Id}: {Error}",
                    item.Id,
                    item.Error?.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting documents to vector store");
            throw;
        }
    }

    public async Task DeleteDocumentAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<ResourceSearchDocument>(
                resourceId.ToString(),
                d => d.Index(_settings.IndexName),
                cancellationToken
            );

            if (response.IsValid)
            {
                _logger.LogInformation("Deleted document {ResourceId} from index", resourceId);
            }
            else
            {
                _logger.LogWarning("Failed to delete document {ResourceId}: {Error}",
                    resourceId, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {ResourceId} from vector store", resourceId);
            throw;
        }
    }

    public async Task<List<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build filter queries
            var mustQueries = new List<Func<QueryContainerDescriptor<ResourceSearchDocument>, QueryContainer>>();
            var mustNotQueries = new List<Func<QueryContainerDescriptor<ResourceSearchDocument>, QueryContainer>>();

            if (request.ResourceType.HasValue)
            {
                mustQueries.Add(q => q.Term(t => t.Field(f => f.Type).Value(request.ResourceType.Value.ToString())));
            }

            if (request.SourceIds != null && request.SourceIds.Any())
            {
                mustQueries.Add(q => q.Terms(t => t.Field(f => f.SourceId).Terms(request.SourceIds.Select(id => id.ToString()))));
            }

            if (request.PublishedAfter.HasValue)
            {
                mustQueries.Add(q => q.DateRange(dr => dr.Field(f => f.PublishedDate).GreaterThanOrEquals(request.PublishedAfter.Value)));
            }

            if (request.PublishedBefore.HasValue)
            {
                mustQueries.Add(q => q.DateRange(dr => dr.Field(f => f.PublishedDate).LessThanOrEquals(request.PublishedBefore.Value)));
            }

            if (request.ExcludeResourceIds != null && request.ExcludeResourceIds.Any())
            {
                mustNotQueries.Add(q => q.Ids(ids => ids.Values(request.ExcludeResourceIds.Select(id => id.ToString()))));
            }

            // Build the k-NN query with filters
            var searchResponse = await _client.SearchAsync<ResourceSearchDocument>(s => s
                .Index(_settings.IndexName)
                .Size(request.TopK)
                .Query(q => q
                    .Bool(b =>
                    {
                        var boolQuery = b
                            .Must(mustQueries.ToArray())
                            .MustNot(mustNotQueries.ToArray());

                        // Add k-NN query
                        boolQuery.Filter(f => f
                            .Knn(knn => knn
                                .Field(field => field.Embedding)
                                .Vector(request.QueryVector)
                                .K(request.TopK)
                            )
                        );

                        return boolQuery;
                    })
                ),
                cancellationToken
            );

            if (!searchResponse.IsValid)
            {
                _logger.LogError("Vector search failed: {Error}", searchResponse.DebugInformation);
                return new List<VectorSearchResult>();
            }

            var results = searchResponse.Hits
                .Where(hit => !request.MinimumScore.HasValue || hit.Score >= request.MinimumScore.Value)
                .Select(hit => new VectorSearchResult
                {
                    ResourceId = Guid.Parse(hit.Source.Id),
                    SimilarityScore = hit.Score ?? 0.0
                })
                .ToList();

            _logger.LogInformation(
                "Vector search returned {Count} results (requested {TopK})",
                results.Count,
                request.TopK);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector search");
            throw;
        }
    }

    public async Task<long> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.CountAsync<ResourceSearchDocument>(c => c
                .Index(_settings.IndexName),
                cancellationToken
            );

            return response.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count");
            throw;
        }
    }

    private static ResourceSearchDocument ToSearchDocument(ResourceDocument document)
    {
        return new ResourceSearchDocument
        {
            Id = document.Id.ToString(),
            Title = document.Title,
            Description = document.Description ?? string.Empty,
            Url = document.Url,
            Type = document.Type.ToString(),
            SourceId = document.SourceId?.ToString() ?? string.Empty,
            PublishedDate = document.PublishedDate ?? document.CreatedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            Embedding = document.Embedding
        };
    }
}

/// <summary>
/// Internal document type for OpenSearch indexing.
/// </summary>
internal class ResourceSearchDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
