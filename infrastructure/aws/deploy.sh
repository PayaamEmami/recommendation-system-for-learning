#!/bin/bash
set -e

# RSL AWS Deployment Script
# This script deploys all RSL infrastructure to AWS
# All resources are prefixed with 'rsl-' for clear separation

# Prevent Git Bash on Windows from converting paths
export MSYS_NO_PATHCONV=1

# Configuration
REGION="${AWS_REGION:-us-west-2}"
ENVIRONMENT="${ENVIRONMENT:-dev}"
PREFIX="rsl"
PROJECT_TAG="RSL"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Check AWS CLI is configured
check_aws_cli() {
    log_info "Checking AWS CLI configuration..."
    if ! aws sts get-caller-identity &> /dev/null; then
        log_error "AWS CLI is not configured. Run 'aws configure' first."
        exit 1
    fi
    ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
    log_info "Using AWS Account: $ACCOUNT_ID"
}

# Create VPC and networking
create_vpc() {
    log_info "Creating VPC and networking..."

    # Check if VPC already exists
    VPC_ID=$(aws ec2 describe-vpcs --filters "Name=tag:Name,Values=${PREFIX}-vpc" --query 'Vpcs[0].VpcId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$VPC_ID" != "None" ] && [ -n "$VPC_ID" ]; then
        log_info "VPC already exists: $VPC_ID"
    else
        # Create VPC
        VPC_ID=$(aws ec2 create-vpc \
            --cidr-block 10.1.0.0/16 \
            --tag-specifications "ResourceType=vpc,Tags=[{Key=Name,Value=${PREFIX}-vpc},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'Vpc.VpcId' \
            --output text \
            --region $REGION)
        log_info "Created VPC: $VPC_ID"

        # Enable DNS hostnames
        aws ec2 modify-vpc-attribute --vpc-id $VPC_ID --enable-dns-hostnames '{"Value":true}' --region $REGION
    fi

    # Create Internet Gateway
    IGW_ID=$(aws ec2 describe-internet-gateways --filters "Name=tag:Name,Values=${PREFIX}-igw" --query 'InternetGateways[0].InternetGatewayId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$IGW_ID" = "None" ] || [ -z "$IGW_ID" ]; then
        IGW_ID=$(aws ec2 create-internet-gateway \
            --tag-specifications "ResourceType=internet-gateway,Tags=[{Key=Name,Value=${PREFIX}-igw},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'InternetGateway.InternetGatewayId' \
            --output text \
            --region $REGION)
        aws ec2 attach-internet-gateway --vpc-id $VPC_ID --internet-gateway-id $IGW_ID --region $REGION
        log_info "Created Internet Gateway: $IGW_ID"
    fi

    # Create public subnets in 2 AZs (required for RDS and load balancing)
    SUBNET_1_ID=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=${PREFIX}-subnet-1" --query 'Subnets[0].SubnetId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$SUBNET_1_ID" = "None" ] || [ -z "$SUBNET_1_ID" ]; then
        SUBNET_1_ID=$(aws ec2 create-subnet \
            --vpc-id $VPC_ID \
            --cidr-block 10.1.1.0/24 \
            --availability-zone ${REGION}a \
            --tag-specifications "ResourceType=subnet,Tags=[{Key=Name,Value=${PREFIX}-subnet-1},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'Subnet.SubnetId' \
            --output text \
            --region $REGION)
        aws ec2 modify-subnet-attribute --subnet-id $SUBNET_1_ID --map-public-ip-on-launch --region $REGION
        log_info "Created Subnet 1: $SUBNET_1_ID"
    fi

    SUBNET_2_ID=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=${PREFIX}-subnet-2" --query 'Subnets[0].SubnetId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$SUBNET_2_ID" = "None" ] || [ -z "$SUBNET_2_ID" ]; then
        SUBNET_2_ID=$(aws ec2 create-subnet \
            --vpc-id $VPC_ID \
            --cidr-block 10.1.2.0/24 \
            --availability-zone ${REGION}b \
            --tag-specifications "ResourceType=subnet,Tags=[{Key=Name,Value=${PREFIX}-subnet-2},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'Subnet.SubnetId' \
            --output text \
            --region $REGION)
        aws ec2 modify-subnet-attribute --subnet-id $SUBNET_2_ID --map-public-ip-on-launch --region $REGION
        log_info "Created Subnet 2: $SUBNET_2_ID"
    fi

    # Create Route Table and associate
    RTB_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=${PREFIX}-rtb" --query 'RouteTables[0].RouteTableId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$RTB_ID" = "None" ] || [ -z "$RTB_ID" ]; then
        RTB_ID=$(aws ec2 create-route-table \
            --vpc-id $VPC_ID \
            --tag-specifications "ResourceType=route-table,Tags=[{Key=Name,Value=${PREFIX}-rtb},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'RouteTable.RouteTableId' \
            --output text \
            --region $REGION)
        aws ec2 create-route --route-table-id $RTB_ID --destination-cidr-block 0.0.0.0/0 --gateway-id $IGW_ID --region $REGION
        aws ec2 associate-route-table --subnet-id $SUBNET_1_ID --route-table-id $RTB_ID --region $REGION
        aws ec2 associate-route-table --subnet-id $SUBNET_2_ID --route-table-id $RTB_ID --region $REGION
        log_info "Created Route Table: $RTB_ID"
    fi

    # Export for other functions
    export VPC_ID SUBNET_1_ID SUBNET_2_ID
}

# Create Security Groups
create_security_groups() {
    log_info "Creating Security Groups..."

    # API Security Group
    API_SG_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=${PREFIX}-api-sg" "Name=vpc-id,Values=$VPC_ID" --query 'SecurityGroups[0].GroupId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$API_SG_ID" = "None" ] || [ -z "$API_SG_ID" ]; then
        API_SG_ID=$(aws ec2 create-security-group \
            --group-name "${PREFIX}-api-sg" \
            --description "Security group for RSL API" \
            --vpc-id $VPC_ID \
            --tag-specifications "ResourceType=security-group,Tags=[{Key=Name,Value=${PREFIX}-api-sg},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'GroupId' \
            --output text \
            --region $REGION)
        aws ec2 authorize-security-group-ingress --group-id $API_SG_ID --protocol tcp --port 8080 --cidr 0.0.0.0/0 --region $REGION
        aws ec2 authorize-security-group-ingress --group-id $API_SG_ID --protocol tcp --port 443 --cidr 0.0.0.0/0 --region $REGION
        log_info "Created API Security Group: $API_SG_ID"
    fi

    # RDS Security Group
    RDS_SG_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=${PREFIX}-rds-sg" "Name=vpc-id,Values=$VPC_ID" --query 'SecurityGroups[0].GroupId' --output text --region $REGION 2>/dev/null || echo "None")

    if [ "$RDS_SG_ID" = "None" ] || [ -z "$RDS_SG_ID" ]; then
        RDS_SG_ID=$(aws ec2 create-security-group \
            --group-name "${PREFIX}-rds-sg" \
            --description "Security group for RSL RDS" \
            --vpc-id $VPC_ID \
            --tag-specifications "ResourceType=security-group,Tags=[{Key=Name,Value=${PREFIX}-rds-sg},{Key=Project,Value=${PROJECT_TAG}}]" \
            --query 'GroupId' \
            --output text \
            --region $REGION)
        # Allow PostgreSQL from API security group
        aws ec2 authorize-security-group-ingress --group-id $RDS_SG_ID --protocol tcp --port 5432 --source-group $API_SG_ID --region $REGION
        # Allow from anywhere for initial setup (you can restrict this later)
        aws ec2 authorize-security-group-ingress --group-id $RDS_SG_ID --protocol tcp --port 5432 --cidr 0.0.0.0/0 --region $REGION
        log_info "Created RDS Security Group: $RDS_SG_ID"
    fi

    export API_SG_ID RDS_SG_ID
}

# Create ECR Repositories
create_ecr() {
    log_info "Creating ECR Repositories..."

    for REPO in "rsl-api" "rsl-jobs"; do
        if ! aws ecr describe-repositories --repository-names $REPO --region $REGION &> /dev/null; then
            aws ecr create-repository \
                --repository-name $REPO \
                --image-scanning-configuration scanOnPush=true \
                --tags Key=Project,Value=${PROJECT_TAG} \
                --region $REGION
            log_info "Created ECR repository: $REPO"
        else
            log_info "ECR repository already exists: $REPO"
        fi
    done

    ECR_URI="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"
    export ECR_URI
}

# Create Secrets in Secrets Manager
create_secrets() {
    log_info "Creating Secrets Manager secrets..."

    # Check if secrets file exists (handle both running from repo root and from aws directory)
    SECRETS_FILE="secrets.env"
    if [ ! -f "$SECRETS_FILE" ]; then
        SECRETS_FILE="infrastructure/aws/secrets.env"
    fi
    if [ ! -f "$SECRETS_FILE" ]; then
        log_warn "Secrets file not found. Creating template at secrets.env"
        SECRETS_FILE="secrets.env"
        cat > "$SECRETS_FILE" << 'EOF'
# RSL Secrets Configuration
# Fill in these values and re-run the deploy script

# Database
DB_PASSWORD=your-strong-password-here

# OpenAI API Key (get from https://platform.openai.com/api-keys)
OPENAI_API_KEY=sk-your-openai-key

# JWT Secret (generate a random 64+ character string)
JWT_SECRET=your-jwt-secret-key-minimum-64-characters-long-for-security

# OpenSearch (will be auto-populated after creation)
OPENSEARCH_ENDPOINT=
EOF
        log_error "Please fill in the secrets in $SECRETS_FILE and run this script again."
        exit 1
    fi

    source "$SECRETS_FILE"

    if [ -z "$DB_PASSWORD" ] || [ "$DB_PASSWORD" = "your-strong-password-here" ]; then
        log_error "Please set DB_PASSWORD in $SECRETS_FILE"
        exit 1
    fi

    if [ -z "$OPENAI_API_KEY" ] || [ "$OPENAI_API_KEY" = "sk-your-openai-key" ]; then
        log_error "Please set OPENAI_API_KEY in $SECRETS_FILE"
        exit 1
    fi

    # Create or update secrets
    for SECRET_NAME in "${PREFIX}-secrets/db-password" "${PREFIX}-secrets/openai-api-key" "${PREFIX}-secrets/jwt-secret"; do
        if ! aws secretsmanager describe-secret --secret-id $SECRET_NAME --region $REGION &> /dev/null; then
            case $SECRET_NAME in
                *db-password)
                    aws secretsmanager create-secret --name $SECRET_NAME --secret-string "$DB_PASSWORD" --tags Key=Project,Value=${PROJECT_TAG} --region $REGION
                    ;;
                *openai-api-key)
                    aws secretsmanager create-secret --name $SECRET_NAME --secret-string "$OPENAI_API_KEY" --tags Key=Project,Value=${PROJECT_TAG} --region $REGION
                    ;;
                *jwt-secret)
                    aws secretsmanager create-secret --name $SECRET_NAME --secret-string "$JWT_SECRET" --tags Key=Project,Value=${PROJECT_TAG} --region $REGION
                    ;;
            esac
            log_info "Created secret: $SECRET_NAME"
        else
            log_info "Secret already exists: $SECRET_NAME"
        fi
    done

    export DB_PASSWORD OPENAI_API_KEY JWT_SECRET
}

# Create RDS PostgreSQL
create_rds() {
    log_info "Creating RDS PostgreSQL instance..."

    # Create DB Subnet Group
    if ! aws rds describe-db-subnet-groups --db-subnet-group-name ${PREFIX}-db-subnet --region $REGION &> /dev/null; then
        aws rds create-db-subnet-group \
            --db-subnet-group-name ${PREFIX}-db-subnet \
            --db-subnet-group-description "RSL Database Subnet Group" \
            --subnet-ids $SUBNET_1_ID $SUBNET_2_ID \
            --tags Key=Project,Value=${PROJECT_TAG} \
            --region $REGION
        log_info "Created DB Subnet Group: ${PREFIX}-db-subnet"
    fi

    # Create RDS instance
    if ! aws rds describe-db-instances --db-instance-identifier ${PREFIX}-db --region $REGION &> /dev/null; then
        aws rds create-db-instance \
            --db-instance-identifier ${PREFIX}-db \
            --db-instance-class db.t3.micro \
            --engine postgres \
            --engine-version 15 \
            --master-username rsladmin \
            --master-user-password "$DB_PASSWORD" \
            --allocated-storage 20 \
            --vpc-security-group-ids $RDS_SG_ID \
            --db-subnet-group-name ${PREFIX}-db-subnet \
            --db-name rsldb \
            --publicly-accessible \
            --backup-retention-period 7 \
            --storage-encrypted \
            --tags Key=Project,Value=${PROJECT_TAG} \
            --region $REGION
        log_info "Creating RDS instance: ${PREFIX}-db (this takes 5-10 minutes...)"

        # Wait for RDS to be available
        log_info "Waiting for RDS instance to be available..."
        aws rds wait db-instance-available --db-instance-identifier ${PREFIX}-db --region $REGION
        log_info "RDS instance is now available!"
    else
        log_info "RDS instance already exists: ${PREFIX}-db"
    fi

    # Get RDS endpoint
    RDS_ENDPOINT=$(aws rds describe-db-instances \
        --db-instance-identifier ${PREFIX}-db \
        --query 'DBInstances[0].Endpoint.Address' \
        --output text \
        --region $REGION)

    log_info "RDS Endpoint: $RDS_ENDPOINT"
    export RDS_ENDPOINT
}

# Create S3 bucket for static web hosting
create_s3_web() {
    log_info "Creating S3 bucket for static web hosting..."

    BUCKET_NAME="${PREFIX}-web-${ACCOUNT_ID}"

    if ! aws s3api head-bucket --bucket $BUCKET_NAME --region $REGION 2>/dev/null; then
        aws s3api create-bucket \
            --bucket $BUCKET_NAME \
            --region $REGION \
            --create-bucket-configuration LocationConstraint=$REGION

        # Enable static website hosting
        aws s3 website s3://$BUCKET_NAME --index-document index.html --error-document index.html

        # Disable block public access
        aws s3api put-public-access-block \
            --bucket $BUCKET_NAME \
            --public-access-block-configuration "BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false" \
            --region $REGION

        # Set bucket policy for public access (inline JSON to avoid Windows path issues)
        BUCKET_POLICY='{
            "Version": "2012-10-17",
            "Statement": [
                {
                    "Sid": "PublicReadGetObject",
                    "Effect": "Allow",
                    "Principal": "*",
                    "Action": "s3:GetObject",
                    "Resource": "arn:aws:s3:::'${BUCKET_NAME}'/*"
                }
            ]
        }'

        aws s3api put-bucket-policy --bucket $BUCKET_NAME --policy "$BUCKET_POLICY" --region $REGION

        # Add tags
        aws s3api put-bucket-tagging --bucket $BUCKET_NAME --tagging "TagSet=[{Key=Project,Value=${PROJECT_TAG}}]" --region $REGION

        log_info "Created S3 bucket: $BUCKET_NAME"
    else
        log_info "S3 bucket already exists: $BUCKET_NAME"
    fi

    WEB_URL="http://${BUCKET_NAME}.s3-website-${REGION}.amazonaws.com"
    export BUCKET_NAME WEB_URL
}

# Create CloudWatch Log Groups
create_cloudwatch_logs() {
    log_info "Creating CloudWatch Log Groups..."

    for LOG_NAME in "api" "jobs" "ingestion" "feed"; do
        LOG_GROUP="/rsl/$LOG_NAME"
        EXISTING=$(aws logs describe-log-groups --log-group-name-prefix "$LOG_GROUP" --region $REGION --query "logGroups[?logGroupName=='$LOG_GROUP'].logGroupName" --output text 2>/dev/null || echo "")
        if [ -z "$EXISTING" ]; then
            aws logs create-log-group --log-group-name "$LOG_GROUP" --tags Project=${PROJECT_TAG} --region $REGION
            aws logs put-retention-policy --log-group-name "$LOG_GROUP" --retention-in-days 30 --region $REGION
            log_info "Created log group: $LOG_GROUP"
        else
            log_info "Log group already exists: $LOG_GROUP"
        fi
    done
}

# Create IAM roles
create_iam_roles() {
    log_info "Creating IAM roles..."

    # App Runner role
    if ! aws iam get-role --role-name ${PREFIX}-apprunner-role &> /dev/null; then
        aws iam create-role \
            --role-name ${PREFIX}-apprunner-role \
            --assume-role-policy-document "$(cat iam/apprunner-trust-policy.json)" \
            --tags Key=Project,Value=${PROJECT_TAG}

        aws iam attach-role-policy \
            --role-name ${PREFIX}-apprunner-role \
            --policy-arn arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess

        # Attach custom policy for secrets and OpenSearch
        aws iam put-role-policy \
            --role-name ${PREFIX}-apprunner-role \
            --policy-name ${PREFIX}-app-policy \
            --policy-document "$(cat iam/app-policy.json)"

        log_info "Created App Runner role: ${PREFIX}-apprunner-role"
    fi

    # ECS Task Execution Role
    if ! aws iam get-role --role-name ${PREFIX}-ecs-execution-role &> /dev/null; then
        aws iam create-role \
            --role-name ${PREFIX}-ecs-execution-role \
            --assume-role-policy-document "$(cat iam/ecs-trust-policy.json)" \
            --tags Key=Project,Value=${PROJECT_TAG}

        aws iam attach-role-policy \
            --role-name ${PREFIX}-ecs-execution-role \
            --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy

        aws iam put-role-policy \
            --role-name ${PREFIX}-ecs-execution-role \
            --policy-name ${PREFIX}-ecs-secrets-policy \
            --policy-document "$(cat iam/ecs-secrets-policy.json)"

        log_info "Created ECS Execution role: ${PREFIX}-ecs-execution-role"
    fi

    # ECS Task Role
    if ! aws iam get-role --role-name ${PREFIX}-ecs-task-role &> /dev/null; then
        aws iam create-role \
            --role-name ${PREFIX}-ecs-task-role \
            --assume-role-policy-document "$(cat iam/ecs-trust-policy.json)" \
            --tags Key=Project,Value=${PROJECT_TAG}

        aws iam put-role-policy \
            --role-name ${PREFIX}-ecs-task-role \
            --policy-name ${PREFIX}-task-policy \
            --policy-document "$(cat iam/app-policy.json)"

        log_info "Created ECS Task role: ${PREFIX}-ecs-task-role"
    fi

    # EventBridge role for ECS
    if ! aws iam get-role --role-name ${PREFIX}-eventbridge-role &> /dev/null; then
        aws iam create-role \
            --role-name ${PREFIX}-eventbridge-role \
            --assume-role-policy-document "$(cat iam/eventbridge-trust-policy.json)" \
            --tags Key=Project,Value=${PROJECT_TAG}

        aws iam put-role-policy \
            --role-name ${PREFIX}-eventbridge-role \
            --policy-name ${PREFIX}-eventbridge-ecs-policy \
            --policy-document "$(cat iam/eventbridge-ecs-policy.json)"

        log_info "Created EventBridge role: ${PREFIX}-eventbridge-role"
    fi
}

# Create ECS Cluster
create_ecs_cluster() {
    log_info "Creating ECS Cluster..."

    if ! aws ecs describe-clusters --clusters ${PREFIX}-cluster --region $REGION --query 'clusters[0].status' --output text 2>/dev/null | grep -q ACTIVE; then
        aws ecs create-cluster \
            --cluster-name ${PREFIX}-cluster \
            --capacity-providers FARGATE FARGATE_SPOT \
            --default-capacity-provider-strategy capacityProvider=FARGATE,weight=1 \
            --tags key=Project,value=${PROJECT_TAG} \
            --region $REGION
        log_info "Created ECS cluster: ${PREFIX}-cluster"
    else
        log_info "ECS cluster already exists: ${PREFIX}-cluster"
    fi
}

# Create OpenSearch Serverless Collection
create_opensearch() {
    log_info "Creating OpenSearch Serverless collection..."

    # Check if collection exists
    COLLECTION_ID=$(aws opensearchserverless list-collections --region $REGION --query "collectionSummaries[?name=='${PREFIX}-search'].id" --output text 2>/dev/null || echo "")

    if [ -z "$COLLECTION_ID" ]; then
        # Create encryption policy first (required) - use inline JSON
        ENCRYPTION_POLICY='{"Rules":[{"ResourceType":"collection","Resource":["collection/'${PREFIX}'-search"]}],"AWSOwnedKey":true}'

        aws opensearchserverless create-security-policy \
            --name ${PREFIX}-encryption-policy \
            --type encryption \
            --policy "$ENCRYPTION_POLICY" \
            --region $REGION 2>/dev/null || true

        # Create network policy (public access for simplicity)
        NETWORK_POLICY='[{"Rules":[{"ResourceType":"collection","Resource":["collection/'${PREFIX}'-search"]},{"ResourceType":"dashboard","Resource":["collection/'${PREFIX}'-search"]}],"AllowFromPublic":true}]'

        aws opensearchserverless create-security-policy \
            --name ${PREFIX}-network-policy \
            --type network \
            --policy "$NETWORK_POLICY" \
            --region $REGION 2>/dev/null || true

        # Create data access policy
        DATA_ACCESS_POLICY='[{"Rules":[{"ResourceType":"collection","Resource":["collection/'${PREFIX}'-search"],"Permission":["aoss:*"]},{"ResourceType":"index","Resource":["index/'${PREFIX}'-search/*"],"Permission":["aoss:*"]}],"Principal":["arn:aws:iam::'${ACCOUNT_ID}':role/'${PREFIX}'-apprunner-role","arn:aws:iam::'${ACCOUNT_ID}':role/'${PREFIX}'-ecs-task-role","arn:aws:iam::'${ACCOUNT_ID}':root"]}]'

        aws opensearchserverless create-access-policy \
            --name ${PREFIX}-data-access-policy \
            --type data \
            --policy "$DATA_ACCESS_POLICY" \
            --region $REGION 2>/dev/null || true

        # Wait a moment for policies to propagate
        sleep 5

        # Create the collection
        COLLECTION_ID=$(aws opensearchserverless create-collection \
            --name ${PREFIX}-search \
            --type VECTORSEARCH \
            --tags key=Project,value=${PROJECT_TAG} \
            --region $REGION \
            --query 'createCollectionDetail.id' \
            --output text)

        log_info "Created OpenSearch collection: ${PREFIX}-search (ID: $COLLECTION_ID)"
        log_info "Waiting for OpenSearch collection to be active (this takes 2-5 minutes)..."

        # Wait for collection to be active
        while true; do
            STATUS=$(aws opensearchserverless list-collections --region $REGION --query "collectionSummaries[?name=='${PREFIX}-search'].status" --output text)
            if [ "$STATUS" = "ACTIVE" ]; then
                break
            fi
            echo -n "."
            sleep 10
        done
        echo ""
        log_info "OpenSearch collection is now active!"
    else
        log_info "OpenSearch collection already exists: ${PREFIX}-search"
    fi

    # Get collection endpoint
    OPENSEARCH_ENDPOINT=$(aws opensearchserverless list-collections --region $REGION --query "collectionSummaries[?name=='${PREFIX}-search'].collectionEndpoint" --output text)
    log_info "OpenSearch Endpoint: $OPENSEARCH_ENDPOINT"

    # Update secrets file with OpenSearch endpoint
    if [ -f "secrets.env" ]; then
        sed -i "s|OPENSEARCH_ENDPOINT=.*|OPENSEARCH_ENDPOINT=$OPENSEARCH_ENDPOINT|" secrets.env 2>/dev/null || \
        sed -i '' "s|OPENSEARCH_ENDPOINT=.*|OPENSEARCH_ENDPOINT=$OPENSEARCH_ENDPOINT|" secrets.env
    elif [ -f "infrastructure/aws/secrets.env" ]; then
        sed -i "s|OPENSEARCH_ENDPOINT=.*|OPENSEARCH_ENDPOINT=$OPENSEARCH_ENDPOINT|" infrastructure/aws/secrets.env 2>/dev/null || \
        sed -i '' "s|OPENSEARCH_ENDPOINT=.*|OPENSEARCH_ENDPOINT=$OPENSEARCH_ENDPOINT|" infrastructure/aws/secrets.env
    fi

    export OPENSEARCH_ENDPOINT
}

# Create App Runner Service
create_app_runner() {
    log_info "Creating App Runner service for API..."

    # Check if service exists
    SERVICE_ARN=$(aws apprunner list-services --region $REGION --query "ServiceSummaryList[?ServiceName=='${PREFIX}-api'].ServiceArn" --output text 2>/dev/null || echo "")

    if [ -z "$SERVICE_ARN" ]; then
        # Build connection string
        CONNECTION_STRING="Host=${RDS_ENDPOINT};Database=rsldb;Username=rsladmin;Password=${DB_PASSWORD}"

        # Create service
        SERVICE_ARN=$(aws apprunner create-service \
            --service-name ${PREFIX}-api \
            --source-configuration '{
                "AuthenticationConfiguration": {
                    "AccessRoleArn": "arn:aws:iam::'${ACCOUNT_ID}':role/'${PREFIX}'-apprunner-role"
                },
                "AutoDeploymentsEnabled": false,
                "ImageRepository": {
                    "ImageIdentifier": "'${ECR_URI}'/rsl-api:latest",
                    "ImageRepositoryType": "ECR",
                    "ImageConfiguration": {
                        "Port": "8080",
                        "RuntimeEnvironmentVariables": {
                            "ASPNETCORE_ENVIRONMENT": "Production",
                            "ConnectionStrings__DefaultConnection": "'"${CONNECTION_STRING}"'",
                            "Embedding__ApiKey": "'"${OPENAI_API_KEY}"'",
                            "Embedding__ModelName": "text-embedding-3-small",
                            "Embedding__Dimensions": "1536",
                            "OpenSearch__Endpoint": "'"${OPENSEARCH_ENDPOINT}"'",
                            "OpenSearch__IndexName": "rsl-resources",
                            "OpenSearch__EmbeddingDimensions": "1536",
                            "JwtSettings__SecretKey": "'"${JWT_SECRET}"'",
                            "JwtSettings__ExpirationMinutes": "60",
                            "Cors__AllowedOrigins__0": "'"${WEB_URL}"'",
                            "Registration__Enabled": "true"
                        }
                    }
                }
            }' \
            --instance-configuration '{"Cpu": "0.25 vCPU", "Memory": "0.5 GB"}' \
            --health-check-configuration '{"Protocol": "HTTP", "Path": "/health", "Interval": 20, "Timeout": 5, "HealthyThreshold": 1, "UnhealthyThreshold": 5}' \
            --tags Key=Project,Value=${PROJECT_TAG} \
            --region $REGION \
            --query 'Service.ServiceArn' \
            --output text)

        log_info "Creating App Runner service (this takes 2-5 minutes)..."

        # Wait for service to be running
        while true; do
            STATUS=$(aws apprunner describe-service --service-arn $SERVICE_ARN --region $REGION --query 'Service.Status' --output text)
            if [ "$STATUS" = "RUNNING" ]; then
                break
            fi
            echo -n "."
            sleep 10
        done
        echo ""
        log_info "App Runner service is now running!"
    else
        log_info "App Runner service already exists: ${PREFIX}-api"
    fi

    # Get service URL
    API_URL=$(aws apprunner describe-service --service-arn $SERVICE_ARN --region $REGION --query 'Service.ServiceUrl' --output text 2>/dev/null || \
              aws apprunner list-services --region $REGION --query "ServiceSummaryList[?ServiceName=='${PREFIX}-api'].ServiceUrl" --output text)
    log_info "API URL: https://$API_URL"
    export API_URL
}

# Register ECS Task Definitions
register_task_definitions() {
    log_info "Registering ECS task definitions..."

    # Build connection string
    CONNECTION_STRING="Host=${RDS_ENDPOINT};Database=rsldb;Username=rsladmin;Password=${DB_PASSWORD}"

    # Register task definitions using inline JSON
    for TASK in "ingestion" "feed"; do
        aws ecs register-task-definition \
            --family "${PREFIX}-${TASK}-task" \
            --network-mode "awsvpc" \
            --requires-compatibilities "FARGATE" \
            --cpu "256" \
            --memory "512" \
            --execution-role-arn "arn:aws:iam::${ACCOUNT_ID}:role/${PREFIX}-ecs-execution-role" \
            --task-role-arn "arn:aws:iam::${ACCOUNT_ID}:role/${PREFIX}-ecs-task-role" \
            --container-definitions '[{
                "name": "'${PREFIX}'-'${TASK}'",
                "image": "'${ECR_URI}'/rsl-jobs:latest",
                "essential": true,
                "command": ["'${TASK}'"],
                "environment": [
                    {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
                    {"name": "ConnectionStrings__DefaultConnection", "value": "'"${CONNECTION_STRING}"'"},
                    {"name": "Embedding__ApiKey", "value": "'"${OPENAI_API_KEY}"'"},
                    {"name": "Embedding__ModelName", "value": "text-embedding-3-small"},
                    {"name": "Embedding__Dimensions", "value": "1536"},
                    {"name": "OpenSearch__Endpoint", "value": "'"${OPENSEARCH_ENDPOINT}"'"},
                    {"name": "OpenSearch__IndexName", "value": "rsl-resources"},
                    {"name": "OpenAI__ApiKey", "value": "'"${OPENAI_API_KEY}"'"},
                    {"name": "OpenAI__Model", "value": "gpt-5-nano"},
                    {"name": "OpenAI__MaxTokens", "value": "16384"}
                ],
                "logConfiguration": {
                    "logDriver": "awslogs",
                    "options": {
                        "awslogs-group": "/rsl/'${TASK}'",
                        "awslogs-region": "'${REGION}'",
                        "awslogs-stream-prefix": "ecs"
                    }
                }
            }]' \
            --region $REGION > /dev/null

        log_info "Registered task definition: ${PREFIX}-${TASK}-task"
    done
}

# Create EventBridge rules for scheduled jobs
create_eventbridge_rules() {
    log_info "Creating EventBridge scheduled rules..."

    # Ingestion job - daily at midnight UTC
    if ! aws events describe-rule --name ${PREFIX}-ingestion-schedule --region $REGION &> /dev/null; then
        aws events put-rule \
            --name ${PREFIX}-ingestion-schedule \
            --schedule-expression "cron(0 0 * * ? *)" \
            --state ENABLED \
            --tags Key=Project,Value=${PROJECT_TAG} \
            --region $REGION
        log_info "Created EventBridge rule: ${PREFIX}-ingestion-schedule (daily at midnight UTC)"
    fi

    # Feed job - daily at 2 AM UTC
    if ! aws events describe-rule --name ${PREFIX}-feed-schedule --region $REGION &> /dev/null; then
        aws events put-rule \
            --name ${PREFIX}-feed-schedule \
            --schedule-expression "cron(0 2 * * ? *)" \
            --state ENABLED \
            --tags Key=Project,Value=${PROJECT_TAG} \
            --region $REGION
        log_info "Created EventBridge rule: ${PREFIX}-feed-schedule (daily at 2 AM UTC)"
    fi

    # Add ECS targets using inline JSON
    for TASK in "ingestion" "feed"; do
        TASK_DEF_ARN=$(aws ecs describe-task-definition --task-definition ${PREFIX}-${TASK}-task --region $REGION --query 'taskDefinition.taskDefinitionArn' --output text)

        TARGET_JSON='[{"Id":"'${PREFIX}'-'${TASK}'-target","Arn":"arn:aws:ecs:'${REGION}':'${ACCOUNT_ID}':cluster/'${PREFIX}'-cluster","RoleArn":"arn:aws:iam::'${ACCOUNT_ID}':role/'${PREFIX}'-eventbridge-role","EcsParameters":{"TaskDefinitionArn":"'${TASK_DEF_ARN}'","TaskCount":1,"LaunchType":"FARGATE","NetworkConfiguration":{"awsvpcConfiguration":{"Subnets":["'${SUBNET_1_ID}'","'${SUBNET_2_ID}'"],"SecurityGroups":["'${API_SG_ID}'"],"AssignPublicIp":"ENABLED"}}}}]'

        aws events put-targets \
            --rule ${PREFIX}-${TASK}-schedule \
            --targets "$TARGET_JSON" \
            --region $REGION

        log_info "Added ECS target to ${PREFIX}-${TASK}-schedule"
    done
}

# Print summary
print_summary() {
    echo ""
    echo "=============================================="
    echo -e "${GREEN}RSL AWS Deployment Complete!${NC}"
    echo "=============================================="
    echo ""
    echo "Resources created (all prefixed with 'rsl-'):"
    echo ""
    echo "Networking:"
    echo "  - VPC: ${PREFIX}-vpc ($VPC_ID)"
    echo "  - Subnets: ${PREFIX}-subnet-1, ${PREFIX}-subnet-2"
    echo ""
    echo "Container Registry:"
    echo "  - ECR: ${ECR_URI}/rsl-api"
    echo "  - ECR: ${ECR_URI}/rsl-jobs"
    echo ""
    echo "Database:"
    echo "  - RDS PostgreSQL: ${PREFIX}-db"
    echo "  - Endpoint: ${RDS_ENDPOINT}"
    echo ""
    echo "API:"
    echo "  - App Runner: ${PREFIX}-api"
    echo "  - URL: https://${API_URL}"
    echo ""
    echo "Web (Static):"
    echo "  - S3 Bucket: ${BUCKET_NAME}"
    echo "  - URL: ${WEB_URL}"
    echo ""
    echo "Vector Search:"
    echo "  - OpenSearch: ${PREFIX}-search"
    echo "  - Endpoint: ${OPENSEARCH_ENDPOINT}"
    echo ""
    echo "Scheduled Jobs:"
    echo "  - Ingestion: Daily at midnight UTC"
    echo "  - Feed: Daily at 2 AM UTC"
    echo ""
    echo "=============================================="
    echo "Next Steps:"
    echo "1. Build and push Docker images:"
    echo "   ./infrastructure/aws/build-and-push.sh"
    echo ""
    echo "2. Deploy web frontend:"
    echo "   aws s3 sync publish/web/wwwroot s3://${BUCKET_NAME}"
    echo ""
    echo "3. Update Blazor config (src/Rsl.Web/wwwroot/appsettings.json):"
    echo "   Set ApiBaseUrl to: https://${API_URL}"
    echo "=============================================="
}

# Main execution
main() {
    log_info "Starting RSL AWS deployment..."
    log_info "Region: $REGION"
    log_info "Environment: $ENVIRONMENT"
    echo ""

    check_aws_cli
    create_vpc
    create_security_groups
    create_ecr
    create_secrets
    create_cloudwatch_logs
    create_iam_roles
    create_rds
    create_s3_web
    create_ecs_cluster
    create_opensearch
    create_app_runner
    register_task_definitions
    create_eventbridge_rules
    print_summary
}

# Run main
main "$@"
