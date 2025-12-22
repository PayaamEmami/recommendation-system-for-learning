#!/bin/bash

# Helper script to get Azure resource names for RSL deployment

set -e

# Colors
GREEN='\033[0;32m'
NC='\033[0m'

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

# Parse arguments
ENVIRONMENT=${1:-dev}
RESOURCE_GROUP="rsl-$ENVIRONMENT-rg"

print_info "Fetching resource names for environment: $ENVIRONMENT"
echo ""

# Get Container Registry
ACR_NAME=$(az acr list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv 2>/dev/null)
if [ -n "$ACR_NAME" ]; then
    echo "Container Registry: $ACR_NAME"
    echo "  Usage: ./build-and-push.sh $ACR_NAME"
else
    echo "Container Registry: NOT FOUND"
fi

# Get Key Vault
KV_NAME=$(az keyvault list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv 2>/dev/null)
if [ -n "$KV_NAME" ]; then
    echo "Key Vault: $KV_NAME"
    echo "  Usage: ./run-migrations.sh $ENVIRONMENT $KV_NAME"
else
    echo "Key Vault: NOT FOUND"
fi

# Get SQL Server
SQL_SERVER=$(az sql server list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv 2>/dev/null)
if [ -n "$SQL_SERVER" ]; then
    echo "SQL Server: $SQL_SERVER"
fi

# Get Container Apps
API_NAME=$(az containerapp list --resource-group "$RESOURCE_GROUP" --query "[?contains(name, 'api')].name" -o tsv 2>/dev/null)
WEB_NAME=$(az containerapp list --resource-group "$RESOURCE_GROUP" --query "[?contains(name, 'web')].name" -o tsv 2>/dev/null)

if [ -n "$API_NAME" ]; then
    echo "API Container App: $API_NAME"
fi
if [ -n "$WEB_NAME" ]; then
    echo "Web Container App: $WEB_NAME"
fi

# Get Jobs
INGESTION_JOB=$(az containerapp job list --resource-group "$RESOURCE_GROUP" --query "[?contains(name, 'ingestion')].name" -o tsv 2>/dev/null)
FEED_JOB=$(az containerapp job list --resource-group "$RESOURCE_GROUP" --query "[?contains(name, 'feed')].name" -o tsv 2>/dev/null)

if [ -n "$INGESTION_JOB" ]; then
    echo "Ingestion Job: $INGESTION_JOB"
fi
if [ -n "$FEED_JOB" ]; then
    echo "Feed Generation Job: $FEED_JOB"
fi

echo ""
print_info "Quick commands:"
echo "# Build and push images:"
echo "  ./build-and-push.sh $ACR_NAME"
echo ""
echo "# Run migrations:"
echo "  ./run-migrations.sh $ENVIRONMENT $KV_NAME"
echo ""
echo "# Trigger ingestion job:"
echo "  az containerapp job start --name $INGESTION_JOB --resource-group $RESOURCE_GROUP"
echo ""
echo "# View logs:"
echo "  az containerapp logs show --name $API_NAME --resource-group $RESOURCE_GROUP --tail 100 --follow"
