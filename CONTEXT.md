# RSL - AI Assistant Context

## Project Overview

**Purpose**: Personalized recommendation system that aggregates learning resources from user-defined sources using AI-powered recommendations.

**Tech Stack**:

- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Frontend**: Blazor WebAssembly (client-side, served via nginx)
- **Cloud**: Azure Container Apps, AI Search, SQL Database
- **AI**: OpenAI API (GPT-5-nano, text-embedding-3-small)

## Architecture

### Core Services

1. **Rsl.Api** - REST API with JWT authentication (Container App)
2. **Rsl.Web** - Blazor WebAssembly web UI (Container App, nginx static hosting)
3. **Rsl.Jobs** - Scheduled jobs (Container Apps Jobs):
   - **Ingestion Job**: Runs daily at midnight UTC
   - **Feed Generation Job**: Runs daily at 2 AM UTC
4. **Rsl.Core** - Domain entities, interfaces
5. **Rsl.Infrastructure** - Data access, Azure integrations, HTML fetching
6. **Rsl.Recommendation** - Hybrid engine (70% vector, 30% heuristics)
7. **Rsl.Llm** - Content ingestion (ChatGPT extracts from HTML)

### Azure Resources

- 2 Container Apps (API, Web)
- 2 Container Apps Jobs (Ingestion, Feed Generation)
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

### Registration

**Toggle user registration** in `infrastructure/bicep/main-container-apps.bicep`:

```
Registration__Enabled: 'true'  // Allow new user registrations
Registration__Enabled: 'false' // Block new registrations
```

**Update both API and Web apps** (two places in the file). Or via Azure CLI:

```bash
az containerapp update --name rsl-dev-api --resource-group rsl-dev-rg --set-env-vars "Registration__Enabled=true"
az containerapp update --name rsl-dev-web --resource-group rsl-dev-rg --set-env-vars "Registration__Enabled=true"
```

### Files

- `infrastructure/bicep/parameters.*.local.json` - NEVER commit (contains secrets)
- Push to `main` → GitHub Actions deploys automatically

## Ingestion Architecture

1. **HtmlFetcherService**: Fetches HTML, removes `<script>`/`<style>` tags
2. **IngestionAgent**: Sends HTML to ChatGPT Chat Completion API → extracts JSON
3. **SourceIngestionJob**: Maps to entities (Paper/Video/BlogPost) → saves to DB → generates embeddings → indexes in Azure AI Search

**Important**: Job exits early if no active sources (no OpenAI calls made).

## Job Scheduling

Jobs are implemented as **Azure Container Apps Jobs** with cron scheduling:

**Ingestion Job** (`rsl-dev-ingestion-job`):

- Schedule: Daily at midnight UTC (`0 0 * * *`)
- Timeout: 2 hours
- Command: `dotnet Rsl.Jobs.dll ingestion`

**Feed Generation Job** (`rsl-dev-feed-job`):

- Schedule: Daily at 2 AM UTC (`0 2 * * *`)
- Timeout: 1 hour
- Command: `dotnet Rsl.Jobs.dll feed`

**Benefits**:

- Containers only run during job execution (cost-efficient)
- No retries on failure (`replicaRetryLimit: 0`)
- Schedule visible in Azure Portal
- Can trigger manually via CLI or API
- Built-in job history and monitoring

**Manual Execution**:

```bash
# Trigger ingestion job manually
az containerapp job start --name rsl-dev-ingestion-job --resource-group rsl-dev-rg

# Trigger feed generation job manually
az containerapp job start --name rsl-dev-feed-job --resource-group rsl-dev-rg

# View job execution history
az containerapp job execution list --name rsl-dev-ingestion-job --resource-group rsl-dev-rg -o table
```

## Bulk Import

**API**: `POST /api/v1/sources/bulk-import`
**Format**:

```json
{
  "sources": [
    {
      "name": "...",
      "url": "...",
      "category": "Paper|Video|BlogPost",
      "description": "..."
    }
  ]
}
```

## Development

**Available Tools**: Azure CLI (`az`) and GitHub CLI (`gh`) are available for automation and deployment tasks.

### Infrastructure Scripts

Helper scripts in `infrastructure/scripts/`:

- **get-resource-names.sh** - Shows all Azure resource names (ACR, Key Vault, etc.)
- **deploy.sh** - Deploys Bicep templates (uses `parameters.*.local.json`)
- **build-and-push.sh** - Builds and pushes Docker images (requires ACR name, not "dev")
- **run-migrations.sh** - Runs EF Core migrations (requires Key Vault name with suffix)

See `infrastructure/scripts/README.md` for detailed usage.

```bash
# Get resource names
cd infrastructure/scripts && ./get-resource-names.sh dev

# Migrations
dotnet ef migrations add Name --project src/Rsl.Infrastructure --startup-project src/Rsl.Api
dotnet ef database update --project src/Rsl.Infrastructure --startup-project src/Rsl.Api

# Deploy infrastructure
cd infrastructure/scripts && ./deploy.sh dev rsl-dev-rg westus

# Logs (Container Apps)
az containerapp logs show --name rsl-dev-api --resource-group rsl-dev-rg --tail 100 --follow
az containerapp logs show --name rsl-dev-web --resource-group rsl-dev-rg --tail 100 --follow

# Logs (Container Apps Jobs - requires execution name)
az containerapp job logs show --name rsl-dev-ingestion-job --resource-group rsl-dev-rg --execution <execution-name>
az containerapp job logs show --name rsl-dev-feed-job --resource-group rsl-dev-rg --execution <execution-name>
```

## Key Decisions

- **Hybrid Recommendations**: 70% vector similarity, 30% heuristics
- **HTML-First Ingestion**: Fetch HTML ourselves, send to ChatGPT
- **Chat Completion API**: Standard API
- **Clean Architecture**: Core → Infrastructure → API/Web/Jobs
- **Repository Pattern**: All data access through interfaces

## Important Rules

- **NO standalone markdown files** (no `TROUBLESHOOTING.md`, `DEPLOYMENT_GUIDE.md`, etc.)
- **NEVER auto-commit** - only when user explicitly requests
- Update this file for architectural changes only
