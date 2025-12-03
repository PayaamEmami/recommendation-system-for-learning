# Rsl.Llm Usage Examples

## Configuration

First, set up your OpenAI API key. Add to `appsettings.Development.local.json`:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here",
    "Model": "gpt-4o",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

## Example 1: Testing with Postman/curl

### Ingest from an Arbitrary URL (Test Mode)

```bash
curl -X POST https://localhost:5001/api/ingestion/ingest-url \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://arxiv.org/list/cs.AI/recent"
  }'
```

**Response:**
```json
{
  "success": true,
  "totalFound": 15,
  "newResources": 13,
  "duplicatesSkipped": 2,
  "resources": [
    {
      "title": "Advances in Neural Architecture Search",
      "url": "https://arxiv.org/abs/2401.12345",
      "description": "A comprehensive survey of recent advances in neural architecture search...",
      "type": "Paper",
      "publishedDate": "2024-01-15",
      "author": "Smith et al.",
      "channel": null
    },
    // ... more resources
  ]
}
```

### Ingest from a Configured Source

```bash
curl -X POST https://localhost:5001/api/ingestion/ingest-source/abc123... \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "success": true,
  "totalFound": 15,
  "newResources": 13,
  "duplicatesSkipped": 2,
  "savedCount": 13,
  "resources": [
    {
      "id": "def456...",
      "title": "Advances in Neural Architecture Search",
      "url": "https://arxiv.org/abs/2401.12345",
      "description": "...",
      "publishedDate": "2024-01-15",
      "type": "Paper",
      "createdAt": "2024-12-03T10:30:00Z",
      "updatedAt": "2024-12-03T10:30:00Z",
      "sourceInfo": {
        "id": "abc123...",
        "name": "ArXiv AI Papers",
        "url": "https://arxiv.org/list/cs.AI/recent"
      }
    }
  ]
}
```

## Example 2: Programmatic Usage in C#

### Direct Injection in a Service

```csharp
using Rsl.Llm.Services;
using Rsl.Llm.Models;

public class MyIngestionService
{
    private readonly IIngestionAgent _ingestionAgent;
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<MyIngestionService> _logger;

    public MyIngestionService(
        IIngestionAgent ingestionAgent,
        IResourceRepository resourceRepository,
        ILogger<MyIngestionService> logger)
    {
        _ingestionAgent = ingestionAgent;
        _resourceRepository = resourceRepository;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestAndLogAsync(string url)
    {
        _logger.LogInformation("Starting ingestion from: {Url}", url);

        var result = await _ingestionAgent.IngestFromUrlAsync(url);

        if (result.Success)
        {
            _logger.LogInformation(
                "Ingestion successful: {Total} found, {New} new, {Duplicates} duplicates",
                result.TotalFound, result.NewResources, result.DuplicatesSkipped);

            // Process each resource
            foreach (var resource in result.Resources)
            {
                _logger.LogDebug("- {Title} ({Type}): {Url}",
                    resource.Title, resource.Type, resource.Url);
            }
        }
        else
        {
            _logger.LogError("Ingestion failed: {Error}", result.ErrorMessage);
        }

        return result;
    }
}
```

### Background Job Example (for Rsl.Jobs)

```csharp
using Rsl.Llm.Services;
using Rsl.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ScheduledIngestionJob : BackgroundService
{
    private readonly IIngestionAgent _ingestionAgent;
    private readonly ISourceRepository _sourceRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<ScheduledIngestionJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public ScheduledIngestionJob(
        IIngestionAgent ingestionAgent,
        ISourceRepository sourceRepository,
        IResourceRepository resourceRepository,
        ILogger<ScheduledIngestionJob> logger)
    {
        _ingestionAgent = ingestionAgent;
        _sourceRepository = sourceRepository;
        _resourceRepository = resourceRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled ingestion run");

                var activeSources = await _sourceRepository.GetActiveSourcesAsync(stoppingToken);

                foreach (var source in activeSources)
                {
                    try
                    {
                        _logger.LogInformation("Ingesting from source: {SourceName}", source.Name);

                        var result = await _ingestionAgent.IngestFromUrlAsync(
                            source.Url,
                            source.Id,
                            stoppingToken);

                        if (result.Success)
                        {
                            // Save resources
                            foreach (var extracted in result.Resources)
                            {
                                var entity = CreateResourceEntity(extracted, source.Id);
                                await _resourceRepository.AddAsync(entity, stoppingToken);
                            }

                            // Update source metadata
                            source.LastFetchedAt = DateTime.UtcNow;
                            source.LastFetchError = null;
                            await _sourceRepository.UpdateAsync(source, stoppingToken);

                            _logger.LogInformation(
                                "Successfully ingested {Count} new resources from {SourceName}",
                                result.NewResources, source.Name);
                        }
                        else
                        {
                            source.LastFetchError = result.ErrorMessage;
                            await _sourceRepository.UpdateAsync(source, stoppingToken);

                            _logger.LogError("Ingestion failed for {SourceName}: {Error}",
                                source.Name, result.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error ingesting from source: {SourceName}", source.Name);
                    }

                    // Small delay between sources to avoid rate limiting
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                _logger.LogInformation("Completed ingestion run. Next run in {Hours} hours",
                    _interval.TotalHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled ingestion job");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private Resource CreateResourceEntity(ExtractedResource extracted, Guid sourceId)
    {
        // Implementation to create appropriate entity type based on ResourceType
        // Similar to the implementation in IngestionController
    }
}
```

## Example 3: Testing Different Source Types

### YouTube Channel

```json
{
  "url": "https://www.youtube.com/@3blue1brown/videos"
}
```

**Expected Output:**
```json
{
  "success": true,
  "totalFound": 20,
  "resources": [
    {
      "title": "The essence of calculus",
      "url": "https://www.youtube.com/watch?v=WUvTyaaNkzM",
      "description": "Introduction to calculus concepts with visual animations...",
      "type": "Video",
      "channel": "3Blue1Brown",
      "duration": "17:24",
      "thumbnailUrl": "https://i.ytimg.com/..."
    }
  ]
}
```

### Blog Homepage

```json
{
  "url": "https://martinfowler.com/"
}
```

**Expected Output:**
```json
{
  "success": true,
  "totalFound": 10,
  "resources": [
    {
      "title": "Refactoring Patterns for Large Codebases",
      "url": "https://martinfowler.com/articles/refactoring-patterns.html",
      "description": "This article explores common refactoring patterns...",
      "type": "BlogPost",
      "author": "Martin Fowler",
      "publishedDate": "2024-01-20"
    }
  ]
}
```

### ArXiv Recent Papers

```json
{
  "url": "https://arxiv.org/list/cs.LG/recent"
}
```

**Expected Output:**
```json
{
  "success": true,
  "totalFound": 50,
  "resources": [
    {
      "title": "Efficient Training of Large Language Models",
      "url": "https://arxiv.org/abs/2401.54321",
      "description": "We propose a novel training technique that reduces...",
      "type": "Paper",
      "author": "Johnson et al.",
      "publishedDate": "2024-01-25",
      "doi": "10.48550/arXiv.2401.54321"
    }
  ]
}
```

### Tech News Site

```json
{
  "url": "https://news.ycombinator.com/"
}
```

**Expected Output:**
```json
{
  "success": true,
  "totalFound": 30,
  "resources": [
    {
      "title": "New AI breakthrough in protein folding",
      "url": "https://example.com/ai-protein-folding",
      "description": "Researchers announce significant progress...",
      "type": "CurrentEvent",
      "publishedDate": "2024-12-03"
    }
  ]
}
```

## Example 4: Error Handling

### Invalid URL

```json
{
  "url": "not-a-valid-url"
}
```

**Response:**
```json
{
  "message": "Invalid URL provided."
}
```

### URL Not Accessible

```json
{
  "url": "https://example.com/404"
}
```

**Response:**
```json
{
  "success": false,
  "message": "Ingestion failed",
  "error": "Failed to access URL: 404 Not Found"
}
```

## Example 5: Advanced Configuration

### Using gpt-4o-mini for Cost Savings

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key",
    "Model": "gpt-4o-mini",
    "MaxTokens": 2048,
    "Temperature": 0.5
  }
}
```

### Using Azure OpenAI

```json
{
  "OpenAI": {
    "ApiKey": "your-azure-key",
    "Model": "gpt-4o",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "BaseUrl": "https://your-resource.openai.azure.com/openai/deployments/your-deployment"
  }
}
```

## Example 6: Monitoring and Logging

### Enable Detailed Logging

In `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Rsl.Llm": "Debug",
      "Rsl.Llm.Services.OpenAIClient": "Trace"
    }
  }
}
```

### Sample Log Output

```
[12:34:56 INF] Starting ingestion from URL: https://arxiv.org/list/cs.AI/recent
[12:34:56 INF] Sending request to OpenAI API with model gpt-4o
[12:34:58 DBG] Agent iteration 1: Executing 2 tool calls
[12:34:58 DBG] Executing tool: check_resource_exists with args: {"url":"https://arxiv.org/abs/2401.12345"}
[12:34:58 DBG] Executing tool: check_resource_exists with args: {"url":"https://arxiv.org/abs/2401.12346"}
[12:35:00 INF] Ingestion completed: 15 resources found, 13 new, 2 duplicates
```

## Best Practices

1. **Rate Limiting**: Add delays between ingestion calls to respect OpenAI rate limits
2. **Error Handling**: Always check `result.Success` before processing resources
3. **Cost Monitoring**: Track token usage and costs in production
4. **Caching**: Consider caching frequently accessed sources
5. **Testing**: Test with small sources first before processing large feeds
6. **Validation**: Validate extracted URLs before saving to database
7. **Logging**: Enable appropriate logging levels for debugging vs. production

## Troubleshooting

### Issue: LLM returns malformed JSON

**Solution**: The agent has retry logic and parsing fallbacks. Check logs for details.

### Issue: Too expensive / high token usage

**Solution**: Switch to `gpt-4o-mini`, reduce `MaxTokens`, or limit ingestion frequency.

### Issue: Duplicate resources being created

**Solution**: Ensure the agent is using the `check_resource_exists` tool. Check tool definitions.

### Issue: Wrong resource categorization

**Solution**: The LLM's categorization is usually accurate. You can refine the system prompt if needed.

---

Ready to start ingesting! 🚀

