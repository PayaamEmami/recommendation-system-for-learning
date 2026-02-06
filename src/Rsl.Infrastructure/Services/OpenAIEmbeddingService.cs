using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Configuration;

namespace Rsl.Infrastructure.Services;

/// <summary>
/// Direct OpenAI API-based embedding service.
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingSettings _settings;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(
        HttpClient httpClient,
        IOptions<EmbeddingSettings> settings,
        IConfiguration configuration,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient for OpenAI API
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");

        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key is missing for embeddings. Set OpenAI__ApiKey.");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
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
            var requestBody = new
            {
                model = _settings.ModelName,
                input = text,
                dimensions = _settings.Dimensions
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("embeddings", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"OpenAI API request failed: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var embeddingArray = result.GetProperty("data")[0].GetProperty("embedding");

            var embedding = new float[Dimensions];
            int i = 0;
            foreach (var value in embeddingArray.EnumerateArray())
            {
                embedding[i++] = (float)value.GetDouble();
            }

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

            // Process in batches to avoid API limits
            var batches = textList
                .Select((text, index) => new { text, index })
                .GroupBy(x => x.index / _settings.MaxBatchSize)
                .Select(g => g.Select(x => x.text).ToList());

            foreach (var batch in batches)
            {
                var requestBody = new
                {
                    model = _settings.ModelName,
                    input = batch,
                    dimensions = _settings.Dimensions
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("embeddings", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"OpenAI API request failed: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                foreach (var item in result.GetProperty("data").EnumerateArray())
                {
                    var embeddingArray = item.GetProperty("embedding");
                    var embedding = new float[Dimensions];
                    int i = 0;
                    foreach (var value in embeddingArray.EnumerateArray())
                    {
                        embedding[i++] = (float)value.GetDouble();
                    }
                    results.Add(embedding);
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
