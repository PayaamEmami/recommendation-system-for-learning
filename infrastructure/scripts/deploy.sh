#!/bin/bash

# RSL Azure Deployment Script
# This script deploys the RSL application to Azure using Azure CLI and Bicep

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    echo "Visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

# Parse command line arguments
ENVIRONMENT=${1:-dev}
RESOURCE_GROUP=${2:-rsl-$ENVIRONMENT-rg}
LOCATION=${3:-westus}

# Determine which parameters file to use (prefer .local.json for secrets)
PARAMETERS_FILE="../bicep/parameters.$ENVIRONMENT.local.json"
if [ ! -f "$PARAMETERS_FILE" ]; then
    PARAMETERS_FILE="../bicep/parameters.$ENVIRONMENT.json"
    print_warning "Local parameters file not found, using: $PARAMETERS_FILE"
else
    print_info "Using local parameters file: $PARAMETERS_FILE"
fi

print_info "Starting deployment to Azure"
print_info "Environment: $ENVIRONMENT"
print_info "Resource Group: $RESOURCE_GROUP"
print_info "Location: $LOCATION"

# Create resource group if it doesn't exist
print_info "Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --tags "Environment=$ENVIRONMENT" "Application=RSL" "ManagedBy=AzureCLI"

# Validate Bicep template
print_info "Validating Bicep template..."
az deployment group validate \
    --resource-group "$RESOURCE_GROUP" \
    --template-file ../bicep/main-container-apps.bicep \
    --parameters "$PARAMETERS_FILE"

# Deploy infrastructure
print_info "Deploying infrastructure..."
DEPLOYMENT_NAME="rsl-deployment-$(date +%Y%m%d-%H%M%S)"
az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file ../bicep/main-container-apps.bicep \
    --parameters "$PARAMETERS_FILE" \
    --name "$DEPLOYMENT_NAME" \
    --verbose

# Get deployment outputs
print_info "Retrieving deployment outputs..."
CONTAINER_REGISTRY=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query properties.outputs.containerRegistryName.value -o tsv)

CONTAINER_REGISTRY_LOGIN_SERVER=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query properties.outputs.containerRegistryLoginServer.value -o tsv)

API_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query properties.outputs.apiAppUrl.value -o tsv)

WEB_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query properties.outputs.webAppUrl.value -o tsv)

print_info "Deployment completed successfully!"
echo ""
print_info "Deployment Summary:"
echo "  Container Registry: $CONTAINER_REGISTRY_LOGIN_SERVER"
echo "  API URL: $API_URL"
echo "  Web URL: $WEB_URL"
echo ""
print_warning "Next steps:"
echo "  1. Build and push Docker images using build-and-push.sh"
echo "  2. Run database migrations"
echo "  3. Configure Azure OpenAI and Azure AI Search endpoints"
echo "  4. Test the application"
