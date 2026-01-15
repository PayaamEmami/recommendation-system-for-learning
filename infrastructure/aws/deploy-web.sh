#!/bin/bash
set -e

# RSL Web Deployment Script
# Builds Blazor WebAssembly and deploys to S3

REGION="${AWS_REGION:-us-west-2}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Get AWS account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
BUCKET_NAME="rsl-web-${ACCOUNT_ID}"

log_info "Deploying to S3 bucket: $BUCKET_NAME"

# Navigate to project root
cd "$(dirname "$0")/../.."

# Build Blazor WebAssembly
log_info "Building Blazor WebAssembly..."
dotnet publish src/Rsl.Web/Rsl.Web.csproj -c Release -o publish/web

# Sync to S3
log_info "Uploading to S3..."
aws s3 sync publish/web/wwwroot s3://$BUCKET_NAME --delete --region $REGION

# Set cache headers for static assets
log_info "Setting cache headers..."
aws s3 cp s3://$BUCKET_NAME/ s3://$BUCKET_NAME/ \
    --recursive \
    --exclude "*" \
    --include "*.js" \
    --include "*.css" \
    --include "*.woff2" \
    --include "*.wasm" \
    --metadata-directive REPLACE \
    --cache-control "max-age=31536000" \
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

# Optional: Invalidate CloudFront cache if distribution exists
DIST_ID=$(aws cloudfront list-distributions --query "DistributionList.Items[?contains(Origins.Items[].DomainName, '${BUCKET_NAME}')].Id" --output text 2>/dev/null || echo "")
if [ -n "$DIST_ID" ] && [ "$DIST_ID" != "None" ]; then
    log_info "Invalidating CloudFront cache..."
    aws cloudfront create-invalidation --distribution-id $DIST_ID --paths "/*"
fi
