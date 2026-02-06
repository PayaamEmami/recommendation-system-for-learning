# RSL AWS Infrastructure

This directory contains all AWS infrastructure and deployment scripts for the RSL project.

## Prerequisites

1. **AWS CLI** installed and configured:
   ```bash
   aws configure
   ```

2. **Docker** installed and running

3. **.NET 10 SDK** installed

## Quick Start

### 1. Create secrets file

Create `infrastructure/aws/secrets.env` with your secrets:

```bash
# Database password (create a strong password)
DB_PASSWORD=YourStrongPassword123!

# OpenAI API Key (from https://platform.openai.com/api-keys)
OpenAI__ApiKey=sk-your-openai-key

# JWT Secret (64+ characters)
JWT_SECRET=your-very-long-jwt-secret-key-at-least-64-characters-for-security
```

### 2. Deploy infrastructure

```bash
cd infrastructure/aws
chmod +x deploy.sh
./deploy.sh
```

Optional flags:
- `ENABLE_OPENSEARCH=true ./deploy.sh` creates OpenSearch (default is skipped) and enables ingestion/feed schedules.

This creates all AWS resources:
- VPC and networking (`rsl-vpc`, `rsl-subnet-*`)
- ECR repositories (`rsl-api`, `rsl-jobs`)
- RDS PostgreSQL (`rsl-db`)
- S3 bucket for web hosting (`rsl-web-*`)
- OpenSearch Serverless (`rsl-search`)
- App Runner service (`rsl-api`)
- ECS cluster and scheduled tasks (`rsl-cluster`)
- IAM roles and policies (`rsl-*-role`)
- CloudWatch log groups (`/rsl/*`)

### 3. Build and push Docker images

```bash
chmod +x build-and-push.sh
./build-and-push.sh
```

### 4. Deploy web frontend

```bash
chmod +x deploy-web.sh
./deploy-web.sh
```

## Resource Naming

All resources are prefixed with `rsl-` for clear separation from other projects:

| Resource Type | Name Pattern |
|--------------|--------------|
| VPC | `rsl-vpc` |
| Subnets | `rsl-subnet-1`, `rsl-subnet-2` |
| Security Groups | `rsl-api-sg`, `rsl-rds-sg` |
| ECR Repositories | `rsl-api`, `rsl-jobs` |
| RDS Instance | `rsl-db` |
| S3 Bucket | `rsl-web-{account-id}` |
| App Runner | `rsl-api` |
| ECS Cluster | `rsl-cluster` |
| ECS Tasks | `rsl-ingestion-task`, `rsl-feed-task`, `rsl-x-ingestion-task` |
| EventBridge Rules | `rsl-ingestion-schedule`, `rsl-feed-schedule`, `rsl-x-ingestion-schedule` |
| OpenSearch | `rsl-search` |
| Secrets | `rsl-secrets/*` |
| Log Groups | `/rsl/*` |
| IAM Roles | `rsl-*-role` |

## Manual Operations

### Trigger scheduled jobs manually

```bash
# Run ingestion job
aws ecs run-task \
  --cluster rsl-cluster \
  --task-definition rsl-ingestion-task \
  --launch-type FARGATE \
  --network-configuration 'awsvpcConfiguration={subnets=[SUBNET_ID],securityGroups=[SG_ID],assignPublicIp=ENABLED}' \
  --region us-west-2

# Run feed generation job
aws ecs run-task \
  --cluster rsl-cluster \
  --task-definition rsl-feed-task \
  --launch-type FARGATE \
  --network-configuration 'awsvpcConfiguration={subnets=[SUBNET_ID],securityGroups=[SG_ID],assignPublicIp=ENABLED}' \
  --region us-west-2

# Run X ingestion job
aws ecs run-task \
  --cluster rsl-cluster \
  --task-definition rsl-x-ingestion-task \
  --launch-type FARGATE \
  --network-configuration 'awsvpcConfiguration={subnets=[SUBNET_ID],securityGroups=[SG_ID],assignPublicIp=ENABLED}' \
  --region us-west-2
```

### View logs

```bash
# API logs
aws logs tail /rsl/api --follow --region us-west-2

# Job logs
aws logs tail /rsl/ingestion --follow --region us-west-2
aws logs tail /rsl/feed --follow --region us-west-2
aws logs tail /rsl/x-ingestion --follow --region us-west-2
```

### Update App Runner service

```bash
# Trigger new deployment
SERVICE_ARN=$(aws apprunner list-services --query "ServiceSummaryList[?ServiceName=='rsl-api'].ServiceArn" --output text --region us-west-2)
aws apprunner start-deployment --service-arn $SERVICE_ARN --region us-west-2
```

### Connect to RDS

```bash
# Get RDS endpoint
aws rds describe-db-instances --db-instance-identifier rsl-db --query 'DBInstances[0].Endpoint.Address' --output text --region us-west-2

# Connect with psql
psql -h <endpoint> -U rsladmin -d rsldb
```

## GitHub Actions Secrets

For CI/CD, add these secrets to your GitHub repository:

| Secret | Description |
|--------|-------------|
| `AWS_ACCESS_KEY_ID` | AWS access key |
| `AWS_SECRET_ACCESS_KEY` | AWS secret key |
| `SQL_ADMIN_PASSWORD` | RDS master password for `rsl-db` |
| `SQL_ADMIN_USERNAME` | RDS master username (optional, defaults to `rsladmin`) |
| `OpenAI__ApiKey` | OpenAI API key for ingestion and embeddings |
| `JWT_SECRET_KEY` | JWT signing secret (64+ chars) |
