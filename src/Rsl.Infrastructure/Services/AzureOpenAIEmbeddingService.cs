using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Configuration;

namespace Rsl.Infrastructure.Services;

/// <summary>
/// Azure OpenAI-based embedding service.
/// </summary>
public class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly AzureOpenAIClient _client;
    private readonly EmbeddingSettings _settings;
    private readonly ILogger<AzureOpenAIEmbeddingService> _logger;

    public AzureOpenAIEmbeddingService(
        IOptions<EmbeddingSettings> settings,
        ILogger<AzureOpenAIEmbeddingService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _client = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new AzureKeyCredential(_settings.ApiKey));
    }

    public int Dimensions => _settings.Dimensions;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty text provided for embedding generation");
            return new float[Dimensions];
        }

        try
        {
            var embeddingClient = _client.GetEmbeddingClient(_settings.DeploymentName);

            var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            var embedding = response.Value.ToFloats().ToArray();

            _logger.LogDebug("Generated embedding for text of length {Length}", text.Length);
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<IList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        if (!textList.Any())
        {
            return new List<float[]>();
        }

        try
        {
            var results = new List<float[]>();
            var embeddingClient = _client.GetEmbeddingClient(_settings.DeploymentName);

            // Process in batches to avoid API limits
            var batches = textList
                .Select((text, index) => new { text, index })
                .GroupBy(x => x.index / _settings.MaxBatchSize)
                .Select(g => g.Select(x => x.text).ToList());

            foreach (var batch in batches)
            {
                var response = await embeddingClient.GenerateEmbeddingsAsync(batch, cancellationToken: cancellationToken);

                foreach (var item in response.Value)
                {
                    results.Add(item.ToFloats().ToArray());
                }

                _logger.LogDebug("Generated {Count} embeddings in batch", batch.Count);
            }

            _logger.LogInformation("Generated {Count} embeddings total", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for batch");
            throw;
        }
    }
}
