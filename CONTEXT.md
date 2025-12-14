# RSL - AI Assistant Context

This document provides essential context for AI coding assistants working on the Recommendation System for Learning (RSL) project.

## Project Overview

**Purpose**: Personalized recommendation system that aggregates and suggests relevant learning resources from user-defined sources using AI-powered recommendations.

**Tech Stack**:
- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Frontend**: Blazor Server
- **Cloud**: Azure (Container Apps, AI Search, OpenAI, SQL Database)
- **AI/ML**: Azure OpenAI (GPT-4, text-embedding-3-small), Vector embeddings, Semantic search
- **DevOps**: Docker, GitHub Actions, Azure Bicep (IaC)

## Architecture Overview

### Core Services

1. **Rsl.Api** - REST API with JWT authentication
2. **Rsl.Web** - Blazor Server interactive web UI
3. **Rsl.Jobs** - Background worker service for scheduled tasks
4. **Rsl.Core** - Domain models, entities, interfaces
5. **Rsl.Infrastructure** - Data access, Azure integrations
6. **Rsl.Recommendation** - Hybrid recommendation engine (70% vector similarity, 30% heuristics)
7. **Rsl.Llm** - LLM-based content ingestion agent

### Background Jobs

**Rsl.Jobs** runs as a continuous worker service (NOT a web API):
- **Source Ingestion Job**: Runs every 24 hours (`0 0 * * *`)
- **Daily Feed Generation Job**: Runs daily at 2:00 AM UTC (`0 2 * * *`)
- **Critical**: Must have `minReplicas: 1` in Container Apps to stay running

## Azure Deployment

### Infrastructure

**Hosted on Azure Container Apps** with the following resources:
- **3 Container Apps**: API, Web, Jobs (all in same environment)
- **Azure AI Search**: Vector database for semantic similarity
- **Azure OpenAI**: GPT-4 and text-embedding-3-small
- **Azure SQL Database**: Application data
- **Azure Container Registry**: Docker images
- **Azure Key Vault**: Secrets management
- **Application Insights**: Monitoring

### Environment Configuration

**CRITICAL**: .NET uses hierarchical configuration with double underscore (`__`) as separator.

Environment variables must follow this pattern:
```
AzureAISearch__Endpoint           ✅ Correct
AzureAISearch__ApiKey             ✅ Correct
ConnectionStrings__DefaultConnection ✅ Correct
Embedding__Endpoint               ✅ Correct

AZURE_SEARCH_ENDPOINT             ❌ Wrong
SQL_CONNECTION_STRING             ❌ Wrong
```

**Configuration mapping**:
- `appsettings.json` section `"AzureAISearch": { "Endpoint": "..." }`
- Environment variable: `AzureAISearch__Endpoint`
- .NET reads as: `settings.Value.Endpoint`

### Bicep Configuration Files

- `infrastructure/bicep/main-container-apps.bicep` - Main infrastructure template
- `infrastructure/bicep/parameters.dev.json` - Dev environment parameters (public)
- `infrastructure/bicep/parameters.dev.local.json` - Local dev parameters (gitignored, contains secrets)
- `infrastructure/bicep/parameters.prod.json` - Production parameters

**Note**: Never commit `parameters.*.local.json` files - they contain secrets!

### CI/CD Pipeline

GitHub Actions workflow (`.github/workflows/azure-deploy.yml`):
- Triggers on push to `main` branch
- Builds solution, runs tests
- Builds Docker images for API, Web, Jobs
- Pushes images to Azure Container Registry
- Updates Container Apps with new images
- **Does NOT** deploy infrastructure (use Bicep deployment for that)

### Scaling Configuration

- **API**: `minReplicas: 0`, `maxReplicas: 3` (scales to zero when idle)
- **Web**: `minReplicas: 0`, `maxReplicas: 3` (scales to zero when idle)
- **Jobs**: `minReplicas: 1`, `maxReplicas: 1` (always running - critical!)

## Development Workflow

### Local Development

```bash
# Run with Docker Compose
docker-compose up

# Run individual projects
dotnet run --project src/Rsl.Api
dotnet run --project src/Rsl.Web
dotnet run --project src/Rsl.Jobs
```

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project src/Rsl.Infrastructure --startup-project src/Rsl.Api

# Update database
dotnet ef database update --project src/Rsl.Infrastructure --startup-project src/Rsl.Api
```

### Testing

```bash
dotnet test
```

## Common Development Tasks

### Deploying Infrastructure Changes

```bash
cd infrastructure/scripts
./deploy.sh dev rsl-dev-rg westus
```

Or manually:
```bash
az deployment group create \
  --resource-group rsl-dev-rg \
  --template-file infrastructure/bicep/main-container-apps.bicep \
  --parameters infrastructure/bicep/parameters.dev.local.json
```

### Deploying Code Changes

Push to `main` branch - GitHub Actions handles the rest.

### Checking Logs

```bash
# Jobs service (most important for debugging recommendations)
az containerapp logs show --name rsl-dev-jobs --resource-group rsl-dev-rg --tail 100 --follow

# API service
az containerapp logs show --name rsl-dev-api --resource-group rsl-dev-rg --tail 50

# Web service
az containerapp logs show --name rsl-dev-web --resource-group rsl-dev-rg --tail 50
```

### Manual Job Trigger

If recommendations aren't generating, check:
1. Jobs service has `minReplicas: 1`
2. Environment variables use `__` separator
3. Check logs for startup errors
4. Verify Azure AI Search and OpenAI endpoints are accessible

## Security Best Practices

- **Never commit secrets**: Use Key Vault references in Bicep
- **Environment variables**: Sensitive values use `@Microsoft.KeyVault(SecretUri=...)`
- **JWT tokens**: Short-lived with refresh token support
- **API rate limiting**: Configured per endpoint
- **CORS**: Restricted to specific origins

## Code Conventions

### Project Structure

- **Clean Architecture**: Core → Infrastructure → API/Web/Jobs
- **Repository Pattern**: All data access through repositories
- **Dependency Injection**: Constructor injection throughout
- **SOLID Principles**: Interface-based design

### Naming Conventions

- **Entities**: PascalCase (e.g., `User`, `Source`, `Resource`)
- **Interfaces**: `I` prefix (e.g., `IUserRepository`)
- **Services**: `Service` suffix (e.g., `AuthService`)
- **DTOs**: Request/Response suffix (e.g., `LoginRequest`, `UserResponse`)

## Documentation Guidelines

### ⚠️ IMPORTANT: No Extra Markdown Files

**DO NOT create** standalone markdown files for documentation, summaries, or guides (e.g., `TROUBLESHOOTING.md`, `DEPLOYMENT_GUIDE.md`, `FIX_SUMMARY.md`).

**Why?**
- Creates repository clutter
- Becomes outdated quickly
- User doesn't want them

**Instead:**
- Update this CONTEXT.md for architectural changes
- Add inline code comments for complex logic
- Update README.md only if user explicitly requests it

**Exception:** README.md is the primary project documentation and should be maintained.

## Troubleshooting Common Issues

### Recommendations Not Generating

**Root causes**:
1. Jobs service scaled to zero (`minReplicas: 0`)
2. Invalid environment variable names (flat instead of hierarchical)
3. Azure AI Search initialization failure
4. Missing or expired Azure OpenAI keys

**How to fix**:
1. Check Jobs service scaling: `az containerapp show --name rsl-dev-jobs --resource-group rsl-dev-rg --query "properties.template.scale"`
2. Verify environment variables use `__` separator
3. Check logs for startup errors
4. Verify Key Vault secrets are accessible

### Vector Store Initialization Errors

If you see `System.UriFormatException: Invalid URI`:
- Environment variables are using wrong naming convention
- Should be `AzureAISearch__Endpoint` not `AZURE_SEARCH_ENDPOINT`
- Redeploy infrastructure with corrected Bicep template

### Database Connection Issues

- Verify `ConnectionStrings__DefaultConnection` is set (with `__`)
- Check Key Vault has `SqlConnectionString` secret
- Ensure Container App has Key Vault reference permissions

### Azure Authentication Errors

- Container Apps need managed identity enabled
- Key Vault access policies must include Container Apps identity
- Check IAM roles on Key Vault

## Important Architecture Decisions

### Why Hybrid Recommendations?

Combines strengths of both approaches:
- **Vector similarity (70%)**: Semantic understanding of content
- **Heuristics (30%)**: Recency, source preferences, user feedback

### Why Azure Container Apps?

- Serverless container hosting (no VM management)
- Auto-scaling with scale-to-zero
- Built-in load balancing and ingress
- Native integration with Azure services
- Cost-effective for variable workloads

### Why Background Worker for Jobs?

- Scheduled tasks need consistent execution
- Recommendation generation is resource-intensive
- Decouples heavy processing from API/Web
- Can run independently with different scaling rules

## Resource Naming Convention

All Azure resources follow: `{appName}-{environment}-{resourceType}[-{uniqueSuffix}]`

Examples:
- `rsl-dev-api` - API Container App
- `rsl-dev-jobs` - Jobs Container App
- `rsl-dev-sql-abc123` - SQL Server
- `rsl-dev-kv-abc123` - Key Vault

## Performance Considerations

- **Vector Search**: Cached embeddings in Azure AI Search
- **Database**: Entity Framework with proper indexing
- **API**: Rate limiting to prevent abuse
- **Background Jobs**: Runs during off-peak hours (2 AM UTC)
- **Scaling**: Auto-scales based on HTTP requests (API/Web only)

