#!/bin/bash
set -e

# CRS Web Deployment Script
# Builds Blazor WebAssembly and deploys to S3

REGION="${AWS_REGION:-us-west-2}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

update_api_base_url() {
    local file_path="$1"
    local api_url="$2"

    if [ ! -f "$file_path" ]; then
        return
    fi

    sed -i "s|\"ApiBaseUrl\": \".*\"|\"ApiBaseUrl\": \"${api_url}\"|" "$file_path" 2>/dev/null || \
    sed -i '' "s|\"ApiBaseUrl\": \".*\"|\"ApiBaseUrl\": \"${api_url}\"|" "$file_path"
}

# Get AWS account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
BUCKET_NAME="crs-web-${ACCOUNT_ID}"

log_info "Deploying to S3 bucket: $BUCKET_NAME"

# Navigate to project root
cd "$(dirname "$0")/../.."

# Resolve current App Runner API URL so the published web config points at the live API.
SERVICE_ARN=$(aws apprunner list-services --query "ServiceSummaryList[?ServiceName=='crs-api'].ServiceArn" --output text --region $REGION 2>/dev/null || echo "")
if [ -z "$SERVICE_ARN" ] || [ "$SERVICE_ARN" = "None" ]; then
    log_error "Could not find App Runner service 'crs-api' in region $REGION."
    exit 1
fi

API_URL=$(aws apprunner describe-service --service-arn "$SERVICE_ARN" --query 'Service.ServiceUrl' --output text --region $REGION)
API_BASE_URL="https://${API_URL}"
log_info "Using API base URL: ${API_BASE_URL}"

# Build Blazor WebAssembly
log_info "Building Blazor WebAssembly..."
dotnet publish src/Crs.Web/Crs.Web.csproj -c Release -o publish/web

log_info "Updating published web config with current API URL..."
update_api_base_url "publish/web/wwwroot/appsettings.json" "$API_BASE_URL"
update_api_base_url "publish/web/wwwroot/appsettings.Production.json" "$API_BASE_URL"

# Sync to S3
log_info "Uploading to S3..."
aws s3 sync publish/web/wwwroot s3://$BUCKET_NAME --delete --region $REGION

# Set cache headers and content types for static assets
log_info "Setting cache headers..."
aws s3 cp s3://$BUCKET_NAME/ s3://$BUCKET_NAME/ \
    --recursive \
    --exclude "*" \
    --include "*.js" \
    --metadata-directive REPLACE \
    --cache-control "max-age=31536000" \
    --content-type "application/javascript" \
    --region $REGION

aws s3 cp s3://$BUCKET_NAME/ s3://$BUCKET_NAME/ \
    --recursive \
    --exclude "*" \
    --include "*.css" \
    --metadata-directive REPLACE \
    --cache-control "max-age=31536000" \
    --content-type "text/css" \
    --region $REGION

aws s3 cp s3://$BUCKET_NAME/ s3://$BUCKET_NAME/ \
    --recursive \
    --exclude "*" \
    --include "*.woff2" \
    --metadata-directive REPLACE \
    --cache-control "max-age=31536000" \
    --content-type "font/woff2" \
    --region $REGION

aws s3 cp s3://$BUCKET_NAME/ s3://$BUCKET_NAME/ \
    --recursive \
    --exclude "*" \
    --include "*.wasm" \
    --metadata-directive REPLACE \
    --cache-control "max-age=31536000" \
    --content-type "application/wasm" \
    --region $REGION

# HTML files should not be cached as aggressively
aws s3 cp s3://$BUCKET_NAME/index.html s3://$BUCKET_NAME/index.html \
    --metadata-directive REPLACE \
    --cache-control "max-age=300" \
    --content-type "text/html" \
    --region $REGION

WEB_URL="http://${BUCKET_NAME}.s3-website-${REGION}.amazonaws.com"
log_info "Deployment complete!"
log_info "Web URL: $WEB_URL"
log_info "Published API base URL: $API_BASE_URL"

# Optional: Invalidate CloudFront cache if distribution exists
DIST_ID=$(aws cloudfront list-distributions --query "DistributionList.Items[?contains(Origins.Items[].DomainName, '${BUCKET_NAME}')].Id" --output text 2>/dev/null || echo "")
if [ -n "$DIST_ID" ] && [ "$DIST_ID" != "None" ]; then
    log_info "Invalidating CloudFront cache..."
    aws cloudfront create-invalidation --distribution-id $DIST_ID --paths "/*"
fi
