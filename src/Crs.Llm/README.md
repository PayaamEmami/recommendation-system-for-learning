# Crs.Llm

LLM-powered ingestion agent for the Content Recommendation System (CRS).

## Overview

This project provides an intelligent, LLM-based agent that can automatically extract learning content from any URL without requiring custom parsers for different sources.

## Features

- **Flexible URL Ingestion**: Provide any URL (YouTube channel, blog, arXiv listing, etc.) and the agent extracts content
- **Automatic Categorization**: LLM categorizes content into Papers, Videos, or BlogPosts
- **Rich Metadata Extraction**: Extracts titles, descriptions, URLs, and type-specific metadata (authors, duration, DOI, etc.)
- **Provider Agnostic**: Abstracted `ILlmClient` allows switching between OpenAI or other providers

## Architecture

### Components

1. **Configuration**

   - `OpenAISettings`: Configuration for OpenAI API (key, model, temperature, etc.)

2. **Models**

   - `ExtractedContent`: Represents a learning content extracted by the agent
   - `IngestionResult`: Result object containing extracted content and metadata

3. **Services**
   - `ILlmClient` / `OpenAIClient`: Handles communication with OpenAI Chat Completion API
   - `IIngestionAgent` / `IngestionAgent`: Main orchestrator that coordinates the ingestion process

## Usage

### Configuration

Add OpenAI settings to your `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-5-nano",
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
            Console.WriteLine($"Found {result.TotalFound} content");
            Console.WriteLine($"New: {result.NewContent}, Duplicates: {result.DuplicatesSkipped}");

            foreach (var content in result.Content)
            {
                Console.WriteLine($"- {content.Title} ({content.Type})");
                Console.WriteLine($"  {content.Url}");
                Console.WriteLine($"  {content.Description}");
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

1. **HTML Fetching**: The system fetches HTML content from the URL using `HtmlFetcherService`

2. **Minimal Cleaning**: Removes `<script>` and `<style>` tags to reduce token usage

3. **LLM Extraction**: The cleaned HTML is sent to ChatGPT with instructions to:

   - Identify all learning content in the HTML
   - Extract title, URL, description, and metadata
   - Categorize content as Paper, Video, or BlogPost

4. **JSON Response**: ChatGPT returns structured JSON with extracted content

5. **Result parsing**: The agent parses the JSON response and returns structured `ExtractedContent` objects

6. **Caller persists content**: The calling code (typically in Crs.Jobs) creates appropriate entity objects and saves to database

## Content Types

The agent categorizes content into these types:

- **Paper**: Academic papers, research publications (extracts DOI, authors, journal)
- **Video**: Educational videos (extracts channel, duration, thumbnail)
- **BlogPost**: Blog articles, tutorials (extracts author, blog name)

## Error Handling

- Malformed LLM responses are caught and logged
- Failed URL fetches return error results
- Maximum iteration limit prevents infinite loops
- All errors are logged for debugging

## Cost Control

- HTML pre-fetching reduces token usage compared to web browsing
- `MaxTokens` setting limits per-request costs
- Agent uses focused prompts to minimize token usage
- GPT-5-nano provides excellent performance at 50x lower cost than GPT-4o
