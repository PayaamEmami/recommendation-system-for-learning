#!/bin/bash
set -e

# RSL Docker Build and Push Script
# Builds Docker images and pushes to ECR

REGION="${AWS_REGION:-us-west-2}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Get AWS account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
ECR_URI="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

log_info "AWS Account: $ACCOUNT_ID"
log_info "ECR URI: $ECR_URI"
log_info "Region: $REGION"

# Login to ECR
log_info "Logging into ECR..."
aws ecr get-login-password --region $REGION | docker login --username AWS --password-stdin $ECR_URI

# Navigate to project root
cd "$(dirname "$0")/../.."
log_info "Building from: $(pwd)"

# Build and push API image
log_info "Building rsl-api image..."
docker build -t rsl-api:latest -f src/Rsl.Api/Dockerfile .
docker tag rsl-api:latest $ECR_URI/rsl-api:latest
docker tag rsl-api:latest $ECR_URI/rsl-api:$(git rev-parse --short HEAD 2>/dev/null || echo "manual")

log_info "Pushing rsl-api to ECR..."
docker push $ECR_URI/rsl-api:latest
docker push $ECR_URI/rsl-api:$(git rev-parse --short HEAD 2>/dev/null || echo "manual")

# Build and push Jobs image
log_info "Building rsl-jobs image..."
docker build -t rsl-jobs:latest -f src/Rsl.Jobs/Dockerfile .
docker tag rsl-jobs:latest $ECR_URI/rsl-jobs:latest
docker tag rsl-jobs:latest $ECR_URI/rsl-jobs:$(git rev-parse --short HEAD 2>/dev/null || echo "manual")

log_info "Pushing rsl-jobs to ECR..."
docker push $ECR_URI/rsl-jobs:latest
docker push $ECR_URI/rsl-jobs:$(git rev-parse --short HEAD 2>/dev/null || echo "manual")

log_info "Done! Images pushed to ECR:"
log_info "  - $ECR_URI/rsl-api:latest"
log_info "  - $ECR_URI/rsl-jobs:latest"

# Optionally update App Runner
read -p "Update App Runner service with new image? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    log_info "Updating App Runner service..."
    SERVICE_ARN=$(aws apprunner list-services --region $REGION --query "ServiceSummaryList[?ServiceName=='rsl-api'].ServiceArn" --output text)
    if [ -n "$SERVICE_ARN" ]; then
        aws apprunner start-deployment --service-arn $SERVICE_ARN --region $REGION
        log_info "Deployment started for App Runner service"
    else
        log_error "App Runner service 'rsl-api' not found"
    fi
fi

# Optionally update ECS task definitions
read -p "Update ECS task definitions with new image? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    log_info "ECS tasks will use the latest image on next scheduled run"
    log_info "To trigger jobs manually, run:"
    log_info "  aws ecs run-task --cluster rsl-cluster --task-definition rsl-ingestion-task --launch-type FARGATE --network-configuration 'awsvpcConfiguration={subnets=[SUBNET_ID],securityGroups=[SG_ID],assignPublicIp=ENABLED}' --region $REGION"
fi
