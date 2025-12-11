#!/bin/bash

# RSL Docker Build and Push Script
# This script builds Docker images and pushes them to Azure Container Registry

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

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install it first."
    exit 1
fi

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    exit 1
fi

# Parse command line arguments
REGISTRY_NAME=${1}
IMAGE_TAG=${2:-latest}

if [ -z "$REGISTRY_NAME" ]; then
    print_error "Usage: $0 <registry-name> [image-tag]"
    echo "Example: $0 rsldevacr latest"
    exit 1
fi

print_info "Building and pushing Docker images"
print_info "Registry: $REGISTRY_NAME.azurecr.io"
print_info "Tag: $IMAGE_TAG"

# Login to Azure Container Registry
print_info "Logging in to Azure Container Registry..."
az acr login --name "$REGISTRY_NAME"

# Navigate to repository root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$SCRIPT_DIR/../.."
cd "$REPO_ROOT"

# Build and push API image
print_info "Building API image..."
docker build -t "$REGISTRY_NAME.azurecr.io/rsl-api:$IMAGE_TAG" \
    -f src/Rsl.Api/Dockerfile .

print_info "Pushing API image..."
docker push "$REGISTRY_NAME.azurecr.io/rsl-api:$IMAGE_TAG"

# Build and push Web image
print_info "Building Web image..."
docker build -t "$REGISTRY_NAME.azurecr.io/rsl-web:$IMAGE_TAG" \
    -f src/Rsl.Web/Dockerfile .

print_info "Pushing Web image..."
docker push "$REGISTRY_NAME.azurecr.io/rsl-web:$IMAGE_TAG"

# Build and push Jobs image
print_info "Building Jobs image..."
docker build -t "$REGISTRY_NAME.azurecr.io/rsl-jobs:$IMAGE_TAG" \
    -f src/Rsl.Jobs/Dockerfile .

print_info "Pushing Jobs image..."
docker push "$REGISTRY_NAME.azurecr.io/rsl-jobs:$IMAGE_TAG"

print_info "All images built and pushed successfully!"
echo ""
print_info "Deployed images:"
echo "  - $REGISTRY_NAME.azurecr.io/rsl-api:$IMAGE_TAG"
echo "  - $REGISTRY_NAME.azurecr.io/rsl-web:$IMAGE_TAG"
echo "  - $REGISTRY_NAME.azurecr.io/rsl-jobs:$IMAGE_TAG"
