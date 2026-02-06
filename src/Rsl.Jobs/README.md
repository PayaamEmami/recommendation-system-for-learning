# Rsl.Jobs

Background worker service for RSL. Handles scheduled data ingestion and feed generation tasks.

## Purpose

Jobs runs as a scheduled container task that orchestrates periodic tasks for content aggregation and recommendation generation. It's a headless .NET Worker Service with no UI or HTTP endpoints.

## Scheduled Tasks

### Source Ingestion Job

- **Schedule:** Runs every 24 hours
- **Purpose:** Pulls new content from all active user-configured sources
- **Steps:**
  1. Fetch active sources from database
  2. For each source, use LLM agent to extract content
  3. Generate embeddings for new resources
  4. Index resources in OpenSearch vector store
  5. Save new resources to database

### Daily Feed Generation Job

- **Schedule:** Runs daily at 2:00 AM
- **Purpose:** Pre-generates personalized recommendation feeds for all users
- **Steps:**
  1. For each user and content type (Papers, Videos, Blogs, etc.):
  2. Build user interest profile from voting history
  3. Run hybrid recommendation engine (vector similarity + heuristics)
  4. Apply diversity and deduplication filters
  5. Save top N recommendations to database for fast retrieval

## Dependencies

- **Rsl.Core:** Domain models and interfaces
- **Rsl.Infrastructure:** Database access, OpenAI, OpenSearch
- **Rsl.Recommendation:** Hybrid recommendation engine
- **Rsl.Llm:** LLM-based content ingestion agent

## Configuration

Jobs requires the same configuration as the API (database connection, OpenAI, OpenSearch) plus job scheduling settings (cron expressions).

See `appsettings.json.example` for required configuration values.

## Local prerequisites (Windows)

- **Docker Desktop + WSL2** running (for local OpenSearch).
- **OpenSearch container** running (see `docker-compose.yml`):
  - `docker compose up -d opensearch`
- **Environment variables**:
  - `OpenAI__ApiKey` (used for both embeddings + LLM)
  - DB connection string reachable from your machine
  - `OpenSearch__Mode=Local` and `OpenSearch__Endpoint=http://localhost:9200`

## Running jobs locally

From the repo root:

```bash
# Source ingestion
dotnet run --project src/Rsl.Jobs -- ingestion

# Daily feed generation
dotnet run --project src/Rsl.Jobs -- feed

# X post ingestion
dotnet run --project src/Rsl.Jobs -- x-ingestion
```

## Reindexing embeddings

Rebuild vector embeddings and reindex all resources:

```bash
dotnet run --project src/Rsl.Jobs -- reindex
```

## Deployment

Jobs is deployed as a scheduled container task:

- **Local:** `dotnet run` or Docker container
- **AWS:** ECS Fargate task triggered by EventBridge Scheduler

The service is triggered on a schedule via AWS EventBridge.
