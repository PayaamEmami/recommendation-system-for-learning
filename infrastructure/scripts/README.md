# Infrastructure Scripts

Utility scripts for managing RSL Azure infrastructure and deployments.

## Prerequisites

- **Azure CLI** installed and authenticated (`az login`)
- **Docker** installed (for `build-and-push.sh`)
- **.NET SDK 10** (for `run-migrations.sh`)
- Appropriate permissions on the Azure subscription

## Scripts Overview

### `get-resource-names.sh`

**Purpose**: Displays all Azure resource names in your environment (ACR, Key Vault, SQL Server, Container Apps, Jobs).

**Usage:**
```bash
./get-resource-names.sh [environment]
```

**Example:**
```bash
./get-resource-names.sh dev
```

**Output:**
```
Container Registry: rsldev<uniqueid>
  Usage: ./build-and-push.sh rsldev<uniqueid>
Key Vault: rsl-dev-kv-<uniqueid>
  Usage: ./run-migrations.sh dev rsl-dev-kv-<uniqueid>
...
```

**When to use**: Run this first to get the correct resource names for other scripts.

---

### `deploy.sh`

**Purpose**: Deploys infrastructure to Azure using Bicep templates. Automatically uses `parameters.*.local.json` if present (for secrets), otherwise falls back to `parameters.*.json`.

**Usage:**
```bash
./deploy.sh [environment] [resource-group] [location]
```

**Example:**
```bash
./deploy.sh dev rsl-dev-rg westus
```

**What it does:**
1. Creates resource group (if needed)
2. Validates Bicep template
3. Deploys all infrastructure (Container Apps, SQL, Key Vault, ACR, etc.)
4. Outputs deployment summary with URLs

**Important**:
- Secrets should be in `parameters.dev.local.json` (NOT committed to git)
- Bicep reads parameters → creates Key Vault → stores secrets for runtime use

---

### `build-and-push.sh`

**Purpose**: Builds Docker images for API, Web, and Jobs, then pushes to Azure Container Registry.

**Usage:**
```bash
./build-and-push.sh <registry-name> [image-tag]
```

**Example:**
```bash
# Get the registry name first
./get-resource-names.sh dev

# Then build and push
./build-and-push.sh rsldev<uniqueid> latest
```

**What it does:**
1. Logs into Azure Container Registry
2. Builds 3 Docker images:
   - `rsl-api`
   - `rsl-web`
   - `rsl-jobs`
3. Pushes all images to ACR

**Important**:
- Registry name is the **full ACR name** (e.g., `rsldev<uniqueid>`), NOT "dev"
- Run `./get-resource-names.sh dev` to get the correct name
- Docker must be running

**Note**: GitHub Actions automatically builds and deploys on push to `main`, so you typically don't need to run this manually.

---

### `run-migrations.sh`

**Purpose**: Runs Entity Framework Core database migrations against Azure SQL Database.

**Usage:**
```bash
./run-migrations.sh [environment] [key-vault-name]
```

**Example:**
```bash
# Get the Key Vault name first
./get-resource-names.sh dev

# Then run migrations
./run-migrations.sh dev rsl-dev-kv-<uniqueid>
```

**What it does:**
1. Retrieves SQL connection string from Key Vault
2. Installs EF Core tools (if needed)
3. Applies all pending migrations to the database

**Important**:
- Key Vault name includes a unique suffix (e.g., `rsl-dev-kv-<uniqueid>`)
- Run `./get-resource-names.sh dev` to get the exact name

---

## Deployment Workflow

### Initial Setup (First Time)

```bash
# 1. Login to Azure
az login

# 2. Deploy infrastructure (creates all Azure resources)
cd infrastructure/scripts
./deploy.sh dev rsl-dev-rg westus

# 3. Get resource names for next steps
./get-resource-names.sh dev

# 4. Build and push Docker images
./build-and-push.sh <acr-name-from-step-3>

# 5. Run database migrations
./run-migrations.sh dev <keyvault-name-from-step-3>

# 6. Trigger ingestion job to populate resources
az containerapp job start --name rsl-dev-ingestion-job --resource-group rsl-dev-rg
```

### Regular Deployment (After Code Changes)

**Recommended**: Push to `main` branch → GitHub Actions handles everything automatically

**Manual**:
```bash
# 1. Get resource names
./get-resource-names.sh dev

# 2. Build and push images
./build-and-push.sh <acr-name>

# 3. Run migrations (if schema changed)
./run-migrations.sh dev <keyvault-name>
```

---

## Common Commands

```bash
# Get all resource names
./get-resource-names.sh dev

# Deploy infrastructure changes
./deploy.sh dev rsl-dev-rg westus

# Manually trigger ingestion job
az containerapp job start --name rsl-dev-ingestion-job --resource-group rsl-dev-rg

# View job execution history
az containerapp job execution list --name rsl-dev-ingestion-job --resource-group rsl-dev-rg -o table

# View live logs from API
az containerapp logs show --name rsl-dev-api --resource-group rsl-dev-rg --tail 100 --follow

# View job logs (get execution name from history)
az containerapp job logs show --name rsl-dev-ingestion-job --resource-group rsl-dev-rg \
  --execution <execution-name> --container rsl-dev-ingestion-job --tail 100
```

---

## Troubleshooting

### "Registry names may contain only alpha numeric characters"
- **Problem**: Running `./build-and-push.sh dev` instead of `./build-and-push.sh rsldev<uniqueid>`
- **Solution**: Run `./get-resource-names.sh dev` to get the actual ACR name

### "Key Vault not found"
- **Problem**: Key Vault name doesn't include the unique suffix
- **Solution**: Run `./get-resource-names.sh dev` to get the full Key Vault name

### "Cannot connect to Docker daemon"
- **Problem**: Docker isn't running
- **Solution**: Start Docker Desktop, or use GitHub Actions to build images

### "No replicas found for execution"
- **Problem**: Job logs are no longer available (expired)
- **Solution**: Trigger a new job execution manually and check logs immediately

---

## Notes

- **Secrets Management**: Store secrets in `parameters.*.local.json` (gitignored). Bicep deployment reads them and creates Key Vault secrets for runtime.
- **GitHub Actions**: Automatically builds and deploys on push to `main`. See `.github/workflows/azure-deploy.yml`.
- **Resource Names**: All Azure resources include a unique suffix based on resource group ID to ensure global uniqueness.
- **Color Output**: Scripts use colored output (Green=Info, Yellow=Warning, Red=Error) for better readability.
