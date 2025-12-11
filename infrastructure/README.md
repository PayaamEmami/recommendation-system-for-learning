# RSL Infrastructure

Azure deployment infrastructure for RSL using Infrastructure as Code (IaC).

## Purpose

This directory contains all deployment automation for running RSL in Microsoft Azure, including:
- Azure Bicep templates for infrastructure provisioning
- Deployment scripts using Azure CLI
- CI/CD pipeline configurations

## Structure

```
infrastructure/
├── bicep/
│   ├── main-container-apps.bicep   # Main infrastructure template (Container Apps)
│   ├── parameters.*.json           # Environment-specific parameters
│   └── modules/                    # Modular Bicep templates
│       ├── container-app.bicep
│       ├── container-apps-environment.bicep
│       ├── container-registry.bicep
│       ├── sql-server.bicep
│       ├── key-vault.bicep
│       ├── log-analytics.bicep
│       └── application-insights.bicep
└── scripts/
    ├── deploy.sh               # Deploy infrastructure
    ├── build-and-push.sh       # Build and push Docker images
    ├── setup-secrets.sh        # Configure Azure Key Vault
    └── run-migrations.sh       # Run database migrations
```

## Azure Resources Deployed

- **Azure Container Apps** (3 instances: API, Web, Jobs)
- **Container Apps Environment** - Hosting environment for containers
- **Azure Container Registry** - Docker image storage
- **Azure SQL Database** - Application database
- **Azure Key Vault** - Secret management
- **Log Analytics Workspace** - Container logs and monitoring
- **Application Insights** - Application telemetry and monitoring
- **Azure OpenAI** (Free tier) - AI/ML services for embeddings
- **Azure AI Search** (Free tier) - Vector database for semantic search

## Prerequisites

1. Azure account with active subscription
2. Azure CLI installed and authenticated: `az login`
3. .NET 10 SDK
4. GitHub account with repository configured

## Deployment

### Automated (CI/CD)
Push to `main` branch triggers GitHub Actions workflow (`.github/workflows/azure-deploy.yml`):
1. Build .NET solution
2. Run tests
3. Build Docker images
4. Push to Container Registry
5. Update Container Apps
6. Database migrations run automatically on API startup

### Manual
```bash
cd infrastructure/scripts
./deploy.sh dev rsl-dev-rg westus
```

## Security

- All secrets stored in Azure Key Vault
- Container App managed identities for service authentication
- HTTPS-only enforcement
- SQL firewall configured for Azure services
- Automatic database migrations with logging
