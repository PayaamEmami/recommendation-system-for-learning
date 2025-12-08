# Rsl.Jobs

Background worker service for RSL. Handles scheduled data ingestion and feed generation tasks.

## Purpose

Jobs runs as a continuous background process that orchestrates periodic tasks for content aggregation and recommendation generation. It's a headless .NET Worker Service with no UI or HTTP endpoints.

## Scheduled Tasks

### Source Ingestion Job
- **Schedule:** Runs every 24 hours
- **Purpose:** Pulls new content from all active user-configured sources
- **Steps:**
  1. Fetch active sources from database
  2. For each source, use LLM agent to extract content
  3. Generate embeddings for new resources
  4. Index resources in Azure AI Search vector store
  5. Save new resources to database

### Daily Feed Generation Job
- **Schedule:** Runs daily at 2:00 AM
- **Purpose:** Pre-generates personalized recommendation feeds for all users
- **Steps:**
  1. For each user and content type (Papers, Videos, Blog Posts, etc.):
  2. Build user interest profile from voting history
  3. Run hybrid recommendation engine (vector similarity + heuristics)
  4. Apply diversity and deduplication filters
  5. Save top N recommendations to database for fast retrieval

## Dependencies

- **Rsl.Core:** Domain models and interfaces
- **Rsl.Infrastructure:** Database access, Azure OpenAI, Azure AI Search
- **Rsl.Recommendation:** Hybrid recommendation engine
- **Rsl.Llm:** LLM-based content ingestion agent

## Configuration

Jobs requires the same configuration as the API (database connection, Azure OpenAI, Azure AI Search) plus job scheduling settings (cron expressions).

See `appsettings.json.example` for required configuration values.

## Deployment

Jobs is deployed as a long-running process:
- **Local:** `dotnet run` or Docker container
- **Azure:** Azure App Service (Linux container, always-on enabled)

The service runs continuously and uses Quartz.NET or similar for scheduling.
