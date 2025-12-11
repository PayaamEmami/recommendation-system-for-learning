#!/bin/bash

# RSL Database Migration Script
# This script runs Entity Framework Core migrations against Azure SQL Database

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

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet CLI is not installed. Please install .NET SDK first."
    exit 1
fi

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    exit 1
fi

# Parse command line arguments
ENVIRONMENT=${1:-dev}
KEY_VAULT_NAME=${2:-rsl-$ENVIRONMENT-kv}

print_info "Running database migrations"
print_info "Environment: $ENVIRONMENT"
print_info "Key Vault: $KEY_VAULT_NAME"

# Get connection string from Key Vault
print_info "Retrieving connection string from Key Vault..."
CONNECTION_STRING=$(az keyvault secret show \
    --vault-name "$KEY_VAULT_NAME" \
    --name "SqlConnectionString" \
    --query value -o tsv)

if [ -z "$CONNECTION_STRING" ]; then
    print_error "Failed to retrieve connection string from Key Vault"
    exit 1
fi

# Navigate to repository root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$SCRIPT_DIR/../.."
cd "$REPO_ROOT"

# Check if EF Core tools are installed
if ! dotnet ef --version &> /dev/null; then
    print_warning "EF Core tools not found. Installing..."
    dotnet tool install --global dotnet-ef
fi

# Run migrations
print_info "Applying migrations to database..."
cd src/Rsl.Infrastructure

dotnet ef database update \
    --startup-project ../Rsl.Api \
    --connection "$CONNECTION_STRING" \
    --verbose

print_info "Database migrations completed successfully!"
