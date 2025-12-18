# RSL - AI Assistant Context

## Project Overview

**Purpose**: Personalized recommendation system that aggregates learning resources from user-defined sources using AI-powered recommendations.

**Tech Stack**:
- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Frontend**: Blazor Server
- **Cloud**: Azure Container Apps, AI Search, SQL Database
- **AI**: OpenAI API (GPT-5-nano, text-embedding-3-small)

## Architecture

### Core Services

1. **Rsl.Api** - REST API with JWT authentication
2. **Rsl.Web** - Blazor Server web UI
3. **Rsl.Jobs** - Background worker (runs every 24h, must have `minReplicas: 1`)
4. **Rsl.Core** - Domain entities, interfaces
5. **Rsl.Infrastructure** - Data access, Azure integrations, HTML fetching
6. **Rsl.Recommendation** - Hybrid engine (70% vector, 30% heuristics)
7. **Rsl.Llm** - Content ingestion (ChatGPT extracts from HTML)

### Azure Resources

- 3 Container Apps (API, Web, Jobs)
- Azure AI Search (vector database)
- Azure SQL Database
- Azure Container Registry
- Azure Key Vault (secrets)
- OpenAI API (direct, not Azure OpenAI)

**Default Region**: `westus`
**Resource Types**: Paper, Video, BlogPost

## Critical Configuration

### Environment Variables

**CRITICAL**: .NET uses `__` (double underscore) for hierarchical config.

```
# Correct
AzureAISearch__Endpoint
ConnectionStrings__DefaultConnection
Embedding__Endpoint
ApiBaseUrl

# Wrong
AZURE_SEARCH_ENDPOINT
SQL_CONNECTION_STRING
```

**Mapping**: `appsettings.json` section `"AzureAISearch": { "Endpoint": "..." }` → env var `AzureAISearch__Endpoint`

### Files

- `infrastructure/bicep/parameters.*.local.json` - NEVER commit (contains secrets)
- Push to `main` → GitHub Actions deploys automatically

## Ingestion Architecture

1. **HtmlFetcherService**: Fetches HTML, removes `<script>`/`<style>` tags
2. **IngestionAgent**: Sends HTML to ChatGPT Chat Completion API → extracts JSON
3. **SourceIngestionJob**: Maps to entities (Paper/Video/BlogPost) → saves to DB → generates embeddings → indexes in Azure AI Search

**Important**: Job exits early if no active sources (no OpenAI calls made).

## Bulk Import

**API**: `POST /api/v1/sources/bulk-import`
**Format**:
```json
{
  "sources": [
    {"name": "...", "url": "...", "category": "Paper|Video|BlogPost", "description": "..."}
  ]
}
```

## Development

```bash
# Migrations
dotnet ef migrations add Name --project src/Rsl.Infrastructure --startup-project src/Rsl.Api
dotnet ef database update --project src/Rsl.Infrastructure --startup-project src/Rsl.Api

# Deploy infrastructure
cd infrastructure/scripts && ./deploy.sh dev rsl-dev-rg westus

# Logs
az containerapp logs show --name rsl-dev-jobs --resource-group rsl-dev-rg --tail 100 --follow
```

## Key Decisions

- **Hybrid Recommendations**: 70% vector similarity, 30% heuristics
- **HTML-First Ingestion**: Fetch HTML ourselves, send to ChatGPT (not GPT web search)
- **Chat Completion API**: Standard API (not Responses API)
- **Clean Architecture**: Core → Infrastructure → API/Web/Jobs
- **Repository Pattern**: All data access through interfaces

## Troubleshooting

**Issue**: Recommendations not generating
**Fix**: Check `minReplicas: 1` on Jobs service, verify env vars use `__`, check logs

**Issue**: Vector store errors (`Invalid URI`)
**Fix**: Use `AzureAISearch__Endpoint` not `AZURE_SEARCH_ENDPOINT`

## Important Rules

- **NO standalone markdown files** (no `TROUBLESHOOTING.md`, `DEPLOYMENT_GUIDE.md`, etc.)
- **NEVER auto-commit** - only when user explicitly requests
- Update this file for architectural changes only
