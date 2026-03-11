# CRS - AI Assistant Context

## Project Overview

**Purpose**: Personalized recommendation system that aggregates learning resources from user-defined sources using AI-powered recommendations.

**Tech Stack**:

- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Frontend**: Blazor WebAssembly (S3 + CloudFront)
- **Cloud**: AWS (App Runner, ECS, RDS, OpenSearch, S3)
- **AI**: OpenAI API (GPT-5-nano, text-embedding-3-small)

## Architecture

### Core Services

1. **Crs.Api** - REST API with JWT authentication (App Runner)
2. **Crs.Web** - Blazor WebAssembly web UI (S3 + CloudFront)
3. **Crs.Jobs** - Scheduled jobs (ECS Fargate + EventBridge):
   - **Ingestion Job**: Runs daily at midnight UTC
   - **Feed Generation Job**: Runs daily at 2 AM UTC
4. **Crs.Core** - Domain entities, interfaces
5. **Crs.Infrastructure** - Data access, AWS integrations, HTML fetching
6. **Crs.Recommendation** - Hybrid engine (70% vector, 30% heuristics)
7. **Crs.Llm** - Content ingestion (ChatGPT extracts from HTML)

### AWS Resources

All resources prefixed with `crs-` for clear separation:

- 1 App Runner service (API) - `crs-api`
- 1 S3 bucket + CloudFront (Web) - `crs-web-*`
- 1 ECS Cluster with 2 scheduled tasks - `crs-cluster`
- AWS OpenSearch Serverless (vector database) - `crs-search`
- RDS PostgreSQL - `crs-db`
- ECR repositories - `crs-api`, `crs-jobs`
- EventBridge Scheduler - `crs-cloudfront-invalidation` (daily CloudFront cache invalidation)
- Secrets Manager - `crs-secrets/*`
- CloudWatch logs - `/crs/*`
- OpenAI API (direct, not AWS Bedrock)

**Default Region**: `us-west-2`
**Resource Types**: Paper, Video, BlogPost

## Critical Configuration

### Environment Variables

**CRITICAL**: .NET uses `__` (double underscore) for hierarchical config.

```
# Correct
OpenSearch__Endpoint
ConnectionStrings__DefaultConnection
OpenAI__ApiKey
ApiBaseUrl

# Wrong
AWS_OPENSEARCH_ENDPOINT
SQL_CONNECTION_STRING
```

**Mapping**: `appsettings.json` section `"OpenSearch": { "Endpoint": "..." }` → env var `OpenSearch__Endpoint`

### Registration

**Toggle user registration**:

1. **API** - Update App Runner environment variables:

```bash
# Get current service ARN
SERVICE_ARN=$(aws apprunner list-services --query "ServiceSummaryList[?ServiceName=='crs-api'].ServiceArn" --output text --region us-west-2)

# Update registration setting (requires service update)
```

2. **Web** - Update in `src/Crs.Web/wwwroot/appsettings.json` (requires redeploy):

```json
"Registration": { "Enabled": true }
```

### Files

- `infrastructure/aws/secrets.env` - NEVER commit (contains secrets)
- Push to `main` → GitHub Actions deploys automatically

## Ingestion Architecture

1. **HtmlFetcherService**: Fetches HTML, removes `<script>`/`<style>` tags
2. **IngestionAgent**: Sends HTML to ChatGPT Chat Completion API → extracts JSON
3. **SourceIngestionJob**: Maps to entities (Paper/Video/BlogPost) → saves to DB → generates embeddings → indexes in OpenSearch

**Important**: Job exits early if no active sources (no OpenAI calls made).

## Job Scheduling

Jobs are implemented as **ECS Fargate tasks** with EventBridge cron scheduling:

**Ingestion Job** (`crs-ingestion-task`):

- Schedule: Daily at midnight UTC (`cron(0 0 * * ? *)`)
- Timeout: 2 hours
- Command: `dotnet Crs.Jobs.dll ingestion`

**Feed Generation Job** (`crs-feed-task`):

- Schedule: Daily at 2 AM UTC (`cron(0 2 * * ? *)`)
- Timeout: 1 hour
- Command: `dotnet Crs.Jobs.dll feed`

**X Ingestion Job** (`crs-x-ingestion-task`):

- Schedule: Daily at 1 AM UTC (`cron(0 1 * * ? *)`)
- Timeout: 1 hour
- Command: `dotnet Crs.Jobs.dll x-ingestion`
- Notes: Ingests recent posts for selected X accounts.

**Benefits**:

- Containers only run during job execution (cost-efficient)
- No retries on failure
- Schedule visible in AWS Console
- Can trigger manually via CLI

**Manual Execution**:

```bash
# Trigger ingestion job manually
aws ecs run-task \
  --cluster crs-cluster \
  --task-definition crs-ingestion-task \
  --launch-type FARGATE \
  --network-configuration 'awsvpcConfiguration={subnets=[SUBNET_ID],securityGroups=[SG_ID],assignPublicIp=ENABLED}' \
  --region us-west-2

# View task logs
aws logs tail /crs/ingestion --follow --region us-west-2
```

### Local Scheduling (Windows Task Scheduler)

Jobs run locally via Windows Task Scheduler, using `run-job.ps1` as a wrapper script that automatically starts Docker Desktop and the OpenSearch container if they aren't running.

- **CRS - Ingestion**: Daily at 11:00 AM Pacific
- **CRS - X Ingestion**: Daily at 11:30 AM Pacific
- **CRS - Feed Generation**: Daily at 12:00 PM Pacific
- **CloudFront Cache Invalidation**: Daily at 1:00 PM Pacific (EventBridge Scheduler, `crs-cloudfront-invalidation`)

Tasks run hidden (no terminal window). Manage via PowerShell:

```powershell
# View task status
Get-ScheduledTask -TaskName "CRS*" | Get-ScheduledTaskInfo

# Disable/enable a task
Disable-ScheduledTask -TaskName "CRS - Ingestion"
Enable-ScheduledTask -TaskName "CRS - Ingestion"

# Remove a task
Unregister-ScheduledTask -TaskName "CRS - Ingestion" -Confirm:$false
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

**Available Tools**: AWS CLI (`aws`) and GitHub CLI (`gh`) are available for automation and deployment tasks.

### Infrastructure Scripts

Helper scripts in `infrastructure/aws/`:

- **deploy.sh** - Deploys all AWS infrastructure
- **build-and-push.sh** - Builds and pushes Docker images to ECR
- **deploy-web.sh** - Builds and deploys Blazor to S3

See `infrastructure/aws/README.md` for detailed usage.

```bash
# Deploy infrastructure
cd infrastructure/aws && ./deploy.sh

# Build and push Docker images
./build-and-push.sh

# Deploy web
./deploy-web.sh

# Migrations (local development)
dotnet ef migrations add Name --project src/Crs.Infrastructure --startup-project src/Crs.Api
dotnet ef database update --project src/Crs.Infrastructure --startup-project src/Crs.Api

# View API logs
aws logs tail /crs/api --follow --region us-west-2
```

## Key Decisions

- **Hybrid Recommendations**: 70% vector similarity, 30% heuristics
- **HTML-First Ingestion**: Fetch HTML ourselves, send to ChatGPT
- **Chat Completion API**: Standard API
- **Clean Architecture**: Core → Infrastructure → API/Web/Jobs
- **Repository Pattern**: All data access through interfaces
- **PostgreSQL**: Using PostgreSQL instead of SQL Server (cost-effective)
- **OpenSearch Serverless**: AWS-native vector search

## Important Rules

- **NO standalone markdown files** (no `TROUBLESHOOTING.md`, `DEPLOYMENT_GUIDE.md`, etc.)
- **NEVER auto-commit** - only when user explicitly requests
- Update this file for architectural changes only
