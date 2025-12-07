using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Core.Models;
using Rsl.Infrastructure.Configuration;

namespace Rsl.Infrastructure.VectorStore;

/// <summary>
/// Azure AI Search implementation of vector store.
/// </summary>
public class AzureAISearchVectorStore : IVectorStore
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly AzureAISearchSettings _settings;
    private readonly ILogger<AzureAISearchVectorStore> _logger;

    public AzureAISearchVectorStore(
        IOptions<AzureAISearchSettings> settings,
        ILogger<AzureAISearchVectorStore> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var credential = new AzureKeyCredential(_settings.ApiKey);
        _indexClient = new SearchIndexClient(new Uri(_settings.Endpoint), credential);
        _searchClient = _indexClient.GetSearchClient(_settings.IndexName);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Azure AI Search index: {IndexName}", _settings.IndexName);

            var index = new SearchIndex(_settings.IndexName)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SearchableField("title") { IsFilterable = false, IsSortable = false },
                    new SearchableField("description") { IsFilterable = false, IsSortable = false },
                    new SimpleField("url", SearchFieldDataType.String) { IsFilterable = false },
                    new SimpleField("type", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("sourceId", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("publishedDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SimpleField("createdAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SimpleField("updatedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = _settings.EmbeddingDimensions,
                        VectorSearchProfileName = "default-vector-profile"
                    }
                },
                VectorSearch = new VectorSearch
                {
                    Profiles =
                    {
                        new VectorSearchProfile("default-vector-profile", "default-algorithm")
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration("default-algorithm")
                    }
                }
            };

            await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
            _logger.LogInformation("Successfully initialized index: {IndexName}", _settings.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Azure AI Search index");
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
            var batch = IndexDocumentsBatch.Upload(searchDocuments);

            var result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Upserted {Count} documents to index, {Succeeded} succeeded",
                documentsList.Count,
                result.Value.Results.Count(r => r.Succeeded));

            var failures = result.Value.Results.Where(r => !r.Succeeded).ToList();
            if (failures.Any())
            {
                foreach (var failure in failures)
                {
                    _logger.LogWarning(
                        "Failed to index document {Key}: {Error}",
                        failure.Key,
                        failure.ErrorMessage);
                }
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
            var batch = IndexDocumentsBatch.Delete("id", new[] { resourceId.ToString() });
            await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted document {ResourceId} from index", resourceId);
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
            var vectorQuery = new VectorizedQuery(request.QueryVector)
            {
                KNearestNeighborsCount = request.TopK,
                Fields = { "embedding" }
            };

            var searchOptions = new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { vectorQuery }
                },
                Size = request.TopK
            };

            // Build filter expression
            var filters = new List<string>();

            if (request.ResourceType.HasValue)
            {
                filters.Add($"type eq '{request.ResourceType.Value}'");
            }

            if (request.SourceIds != null && request.SourceIds.Any())
            {
                var sourceFilters = string.Join(" or ",
                    request.SourceIds.Select(id => $"sourceId eq '{id}'"));
                filters.Add($"({sourceFilters})");
            }

            if (request.PublishedAfter.HasValue)
            {
                var dateStr = request.PublishedAfter.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                filters.Add($"publishedDate ge {dateStr}");
            }

            if (request.PublishedBefore.HasValue)
            {
                var dateStr = request.PublishedBefore.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                filters.Add($"publishedDate le {dateStr}");
            }

            if (request.ExcludeResourceIds != null && request.ExcludeResourceIds.Any())
            {
                var excludeFilters = string.Join(" and ",
                    request.ExcludeResourceIds.Select(id => $"id ne '{id}'"));
                filters.Add($"({excludeFilters})");
            }

            if (filters.Any())
            {
                searchOptions.Filter = string.Join(" and ", filters);
            }

            var response = await _searchClient.SearchAsync<SearchDocument>(
                null,
                searchOptions,
                cancellationToken);

            var results = new List<VectorSearchResult>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var score = result.Score ?? 0.0;

                // Apply minimum score filter if specified
                if (request.MinimumScore.HasValue && score < request.MinimumScore.Value)
                {
                    continue;
                }

                var resourceId = Guid.Parse(result.Document["id"].ToString()!);

                results.Add(new VectorSearchResult
                {
                    ResourceId = resourceId,
                    SimilarityScore = score
                });
            }

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
            var response = await _searchClient.SearchAsync<SearchDocument>(
                "*",
                new SearchOptions { Size = 0, IncludeTotalCount = true },
                cancellationToken);

            return response.Value.TotalCount ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count");
            throw;
        }
    }

    private static SearchDocument ToSearchDocument(ResourceDocument document)
    {
        return new SearchDocument
        {
            ["id"] = document.Id.ToString(),
            ["title"] = document.Title,
            ["description"] = document.Description ?? string.Empty,
            ["url"] = document.Url,
            ["type"] = document.Type.ToString(),
            ["sourceId"] = document.SourceId?.ToString() ?? string.Empty,
            ["publishedDate"] = document.PublishedDate,
            ["createdAt"] = document.CreatedAt,
            ["updatedAt"] = document.UpdatedAt,
            ["embedding"] = document.Embedding
        };
    }
}
