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
│   ├── main.bicep              # Main infrastructure template
│   ├── parameters.*.json       # Environment-specific parameters
│   └── modules/                # Modular Bicep templates
└── scripts/
    ├── deploy.sh               # Deploy infrastructure
    ├── build-and-push.sh       # Build and push Docker images
    ├── setup-secrets.sh        # Configure Azure Key Vault
    └── run-migrations.sh       # Run database migrations
```

## Azure Resources Deployed

- **Azure App Service** (3 instances: API, Web, Jobs)
- **Azure Container Registry:** Docker image storage
- **Azure SQL Database:** Application database
- **Azure Key Vault:** Secret management
- **Application Insights:** Monitoring and logging
- **Azure OpenAI & Azure AI Search:** Must be created manually

## Prerequisites

1. Azure account with active subscription
2. Azure CLI installed: `brew install azure-cli`
3. Docker Desktop
4. .NET 10 SDK
5. Azure OpenAI and Azure AI Search resources created

## Quick Start

```bash
# Login to Azure
az login

# Deploy infrastructure
cd infrastructure/scripts
./deploy.sh dev rsl-dev-rg eastus

# Configure secrets
./setup-secrets.sh dev

# Build and push images
./build-and-push.sh <registry-name> latest

# Run database migrations
./run-migrations.sh dev

# Restart services
az webapp restart --name rsl-dev-api --resource-group rsl-dev-rg
az webapp restart --name rsl-dev-web --resource-group rsl-dev-rg
az webapp restart --name rsl-dev-jobs --resource-group rsl-dev-rg
```

## CI/CD

GitHub Actions workflow (`.github/workflows/azure-deploy.yml`) provides automated deployment on push to main branch.

## Cost Estimation

- **Development:** ~$100-120/month
- **Production:** ~$450-500/month

Costs include App Service, SQL Database, Container Registry, Azure AI Search, Application Insights, and Azure OpenAI usage.

## Security

- All secrets stored in Azure Key Vault
- Managed identities for service authentication
- HTTPS-only enforcement
- SQL firewall rules
- Non-root Docker containers
