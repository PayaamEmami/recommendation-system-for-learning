# Rsl.Llm

LLM-powered ingestion agent for the Recommendation System for Learning (RSL).

## Overview

This project provides an intelligent, LLM-based agent that can automatically extract learning resources from any URL without requiring custom parsers for different sources.

## Features

- **Flexible URL Ingestion**: Provide any URL (YouTube channel, blog, arXiv listing, etc.) and the agent extracts resources
- **Automatic Categorization**: LLM categorizes resources into Papers, Videos, BlogPosts, CurrentEvents, or SocialMediaPosts
- **Duplicate Detection**: Agent uses tools to query the database and avoid inserting duplicate resources
- **Rich Metadata Extraction**: Extracts titles, descriptions, URLs, and type-specific metadata (authors, duration, DOI, etc.)
- **Provider Agnostic**: Abstracted `ILlmClient` allows switching between OpenAI, Azure OpenAI, or other providers

## Architecture

### Components

1. **Configuration**
   - `OpenAISettings`: Configuration for OpenAI API (key, model, temperature, etc.)

2. **Models**
   - `ExtractedResource`: Represents a learning resource extracted by the agent
   - `IngestionResult`: Result object containing extracted resources and metadata

3. **Services**
   - `ILlmClient` / `OpenAIClient`: Handles communication with OpenAI API with function calling
   - `IIngestionAgent` / `IngestionAgent`: Main orchestrator that coordinates the ingestion process

4. **Tools**
   - `AgentTools`: Provides database query tools that the LLM can call during ingestion
     - `check_resource_exists`: Check if a URL is already in the database
     - `get_resources_from_source`: Get all resources from a specific source

## Usage

### Configuration

Add OpenAI settings to your `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-4o",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

### Registration

In your `Program.cs` or startup:

```csharp
builder.Services.AddLlmServices(builder.Configuration);
```

### Using the Ingestion Agent

```csharp
public class MyService
{
    private readonly IIngestionAgent _ingestionAgent;

    public MyService(IIngestionAgent ingestionAgent)
    {
        _ingestionAgent = ingestionAgent;
    }

    public async Task IngestFromSource(string url, Guid? sourceId = null)
    {
        var result = await _ingestionAgent.IngestFromUrlAsync(url, sourceId);

        if (result.Success)
        {
            Console.WriteLine($"Found {result.TotalFound} resources");
            Console.WriteLine($"New: {result.NewResources}, Duplicates: {result.DuplicatesSkipped}");

            foreach (var resource in result.Resources)
            {
                Console.WriteLine($"- {resource.Title} ({resource.Type})");
                Console.WriteLine($"  {resource.Url}");
                Console.WriteLine($"  {resource.Description}");
            }
        }
        else
        {
            Console.WriteLine($"Ingestion failed: {result.ErrorMessage}");
        }
    }
}
```

## How It Works

1. **User provides a URL**: Can be any learning resource listing page (YouTube channel, blog homepage, arXiv category, etc.)

2. **Agent receives instructions**: The system prompt instructs the LLM to:
   - Browse the URL
   - Extract learning resources
   - Categorize them appropriately
   - Use tools to check for duplicates

3. **LLM browses and extracts**: Using its web browsing capability, the LLM visits the URL and identifies resources

4. **Tool calling loop**: The LLM can call provided tools to:
   - Check if specific URLs already exist
   - Get existing resources from a source

5. **Result parsing**: The agent parses the LLM's JSON response and returns structured `ExtractedResource` objects

6. **Caller persists resources**: The calling code (typically in Rsl.Jobs) creates appropriate entity objects and saves to database

## Resource Types

The agent categorizes resources into these types:

- **Paper**: Academic papers, research publications (extracts DOI, authors, journal)
- **Video**: Educational videos (extracts channel, duration, thumbnail)
- **BlogPost**: Blog articles, tutorials (extracts author, blog name)
- **CurrentEvent**: News articles, announcements
- **SocialMediaPost**: Twitter/X threads, LinkedIn posts, Reddit discussions

## Error Handling

- Malformed LLM responses are caught and logged
- Failed URL fetches return error results
- Maximum iteration limit prevents infinite loops
- All errors are logged for debugging

## Cost Control

- `MaxTokens` setting limits per-request costs
- Agent uses focused prompts to minimize token usage
- Tool calls are efficient and targeted
- Consider using `gpt-4o-mini` for cost savings during development

## Future Enhancements

- Support for Claude, Azure OpenAI, and other providers
- Batch processing of multiple URLs
- Caching of already-processed pages
- Configurable extraction strategies per source type
- Automatic retry with backoff for rate limits

