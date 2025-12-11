# Infrastructure Scripts

This directory contains utility scripts for managing the RSL Azure infrastructure.

## Scripts

### `deploy.sh`
Deploys the infrastructure to Azure using Bicep templates.

**Usage:**
```bash
./deploy.sh [environment] [resource-group] [location]
```

**Example:**
```bash
./deploy.sh dev rsl-dev-rg westus
```

### `build-and-push.sh`
Builds Docker images and pushes them to Azure Container Registry.

**Usage:**
```bash
./build-and-push.sh [environment] [registry-name]
```

### `setup-secrets.sh`
Interactive script to set up secrets in Azure Key Vault.

**Usage:**
```bash
./setup-secrets.sh [environment] [key-vault-name]
```

### `run-migrations.sh`
Runs database migrations against the Azure SQL database.

**Usage:**
```bash
./run-migrations.sh [environment]
```

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Appropriate permissions on the Azure subscription
- Docker installed (for build-and-push.sh)

## Notes

All scripts use colored output for better readability:
- 🟢 Green: Informational messages
- 🟡 Yellow: Warnings
- 🔴 Red: Errors
