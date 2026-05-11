#!/usr/bin/env bash
# Step 7: Create ECS task definitions and services (API + Worker).
# Cost: ~$15-25/mo (Fargate API + Fargate Spot Worker)

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 7: ECS Task Definitions & Services ──────────────────────────"

# Required inputs
: "${APP_SG_ID:?Set APP_SG_ID from step 1}"
: "${SUBNET_1:?Set SUBNET_1 from step 1}"
: "${SUBNET_2:?Set SUBNET_2 from step 1}"
: "${RDS_ENDPOINT:?Set RDS_ENDPOINT from step 2}"
: "${RDS_MASTER_PASSWORD:?Set RDS_MASTER_PASSWORD}"

EXEC_ROLE_ARN="arn:aws:iam::${AWS_ACCOUNT_ID}:role/${PROJECT}-ecs-execution"
TASK_ROLE_ARN="arn:aws:iam::${AWS_ACCOUNT_ID}:role/${PROJECT}-task-role"

# ── Create CloudWatch log groups ────────────────────────────────────────────
for lg in "/ecs/${PROJECT}-api" "/ecs/${PROJECT}-worker"; do
  aws logs create-log-group --log-group-name "${lg}" --region "${AWS_REGION}" 2>/dev/null \
    && echo "✓ Created log group: ${lg}" \
    || echo "✓ Log group exists: ${lg}"
  # Set retention to 14 days to save costs
  aws logs put-retention-policy --log-group-name "${lg}" --retention-in-days 14 --region "${AWS_REGION}" 2>/dev/null || true
done

# ── Generate API task definition ─────────────────────────────────────────────
CONNECTION_STRING="Host=${RDS_ENDPOINT};Port=5432;Database=${RDS_DB_NAME};Username=${RDS_USERNAME};Password=${RDS_MASTER_PASSWORD}"

cat > /tmp/api-task-def.json <<TASKEOF
{
  "family": "${PROJECT}-api",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "executionRoleArn": "${EXEC_ROLE_ARN}",
  "taskRoleArn": "${TASK_ROLE_ARN}",
  "containerDefinitions": [{
    "name": "api",
    "image": "${ECR_REGISTRY}/${ECR_REPO_API}:latest",
    "essential": true,
    "portMappings": [{"containerPort": 8080, "protocol": "tcp"}],
    "environment": [
      {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
      {"name": "ConnectionStrings__Default", "value": "${CONNECTION_STRING}"},
      {"name": "MinIO__Endpoint", "value": "s3.${AWS_REGION}.amazonaws.com"},
      {"name": "MinIO__UseHttps", "value": "true"},
      {"name": "MinIO__UseIamRole", "value": "true"},
      {"name": "MinIO__EvidenceBucket", "value": "${S3_EVIDENCE_BUCKET}"},
      {"name": "MinIO__ExportsBucket", "value": "${S3_EXPORTS_BUCKET}"},
      {"name": "App__BaseUrl", "value": "https://${API_DOMAIN}"},
      {"name": "App__FrontendUrl", "value": "https://${DOMAIN_NAME}"},
      {"name": "ClamAV__Enabled", "value": "false"}
    ],
    "secrets": [
      {"name": "Jwt__Secret", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/jwt-secret"},
      {"name": "Stripe__SecretKey", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/stripe-secret-key"},
      {"name": "Stripe__WebhookSecret", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/stripe-webhook-secret"},
      {"name": "GoogleMaps__ApiKey", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/google-maps-key"},
      {"name": "Encryption__Key", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/encryption-key"},
      {"name": "Signing__Secret", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/signing-secret"}
    ],
    "logConfiguration": {
      "logDriver": "awslogs",
      "options": {
        "awslogs-group": "/ecs/${PROJECT}-api",
        "awslogs-region": "${AWS_REGION}",
        "awslogs-stream-prefix": "api"
      }
    },
    "healthCheck": {
      "command": ["CMD-SHELL", "curl -sf http://localhost:8080/health || exit 1"],
      "interval": 30,
      "timeout": 5,
      "retries": 3,
      "startPeriod": 60
    }
  }]
}
TASKEOF

echo "✓ Generated API task definition"

# ── Generate Worker task definition ──────────────────────────────────────────
cat > /tmp/worker-task-def.json <<TASKEOF
{
  "family": "${PROJECT}-worker",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "${EXEC_ROLE_ARN}",
  "taskRoleArn": "${TASK_ROLE_ARN}",
  "containerDefinitions": [{
    "name": "worker",
    "image": "${ECR_REGISTRY}/${ECR_REPO_WORKER}:latest",
    "essential": true,
    "environment": [
      {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
      {"name": "ConnectionStrings__Default", "value": "${CONNECTION_STRING}"},
      {"name": "MinIO__Endpoint", "value": "s3.${AWS_REGION}.amazonaws.com"},
      {"name": "MinIO__UseHttps", "value": "true"},
      {"name": "MinIO__UseIamRole", "value": "true"},
      {"name": "MinIO__EvidenceBucket", "value": "${S3_EVIDENCE_BUCKET}"},
      {"name": "MinIO__ExportsBucket", "value": "${S3_EXPORTS_BUCKET}"},
      {"name": "ClamAV__Enabled", "value": "false"}
    ],
    "secrets": [
      {"name": "Jwt__Secret", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/jwt-secret"},
      {"name": "Stripe__SecretKey", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/stripe-secret-key"},
      {"name": "Encryption__Key", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/encryption-key"},
      {"name": "Signing__Secret", "valueFrom": "arn:aws:ssm:${AWS_REGION}:${AWS_ACCOUNT_ID}:parameter${SSM_PREFIX}/signing-secret"}
    ],
    "logConfiguration": {
      "logDriver": "awslogs",
      "options": {
        "awslogs-group": "/ecs/${PROJECT}-worker",
        "awslogs-region": "${AWS_REGION}",
        "awslogs-stream-prefix": "worker"
      }
    },
    "healthCheck": {
      "command": ["CMD-SHELL", "curl -sf http://localhost:5100/healthz || exit 1"],
      "interval": 30,
      "timeout": 5,
      "retries": 3,
      "startPeriod": 60
    }
  }]
}
TASKEOF

echo "✓ Generated Worker task definition"

# ── Register task definitions ────────────────────────────────────────────────
aws ecs register-task-definition --cli-input-json file:///tmp/api-task-def.json --region "${AWS_REGION}" > /dev/null
echo "✓ Registered API task definition"

aws ecs register-task-definition --cli-input-json file:///tmp/worker-task-def.json --region "${AWS_REGION}" > /dev/null
echo "✓ Registered Worker task definition"

# ── Create ALB ───────────────────────────────────────────────────────────────
ALB_ARN=$(aws elbv2 describe-load-balancers \
  --names "${ALB_NAME}" \
  --query "LoadBalancers[0].LoadBalancerArn" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${ALB_ARN}" = "None" ] || [ -z "${ALB_ARN}" ]; then
  ALB_ARN=$(aws elbv2 create-load-balancer \
    --name "${ALB_NAME}" \
    --subnets "${SUBNET_1}" "${SUBNET_2}" \
    --security-groups "${APP_SG_ID}" \
    --type application \
    --query "LoadBalancers[0].LoadBalancerArn" --output text \
    --region "${AWS_REGION}")
  echo "✓ Created ALB: ${ALB_NAME}"
else
  echo "✓ ALB already exists: ${ALB_NAME}"
fi

# Get VPC ID from the ALB
VPC_ID=$(aws elbv2 describe-load-balancers \
  --load-balancer-arns "${ALB_ARN}" \
  --query "LoadBalancers[0].VpcId" --output text \
  --region "${AWS_REGION}")

# ── Create Target Group ──────────────────────────────────────────────────────
TG_ARN=$(aws elbv2 describe-target-groups \
  --names "${TG_NAME}" \
  --query "TargetGroups[0].TargetGroupArn" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${TG_ARN}" = "None" ] || [ -z "${TG_ARN}" ]; then
  TG_ARN=$(aws elbv2 create-target-group \
    --name "${TG_NAME}" \
    --protocol HTTP \
    --port 8080 \
    --vpc-id "${VPC_ID}" \
    --target-type ip \
    --health-check-path /health \
    --health-check-interval-seconds 30 \
    --healthy-threshold-count 2 \
    --query "TargetGroups[0].TargetGroupArn" --output text \
    --region "${AWS_REGION}")
  echo "✓ Created target group: ${TG_NAME}"
else
  echo "✓ Target group already exists: ${TG_NAME}"
fi

# ── Create HTTP Listener ─────────────────────────────────────────────────────
LISTENER_ARN=$(aws elbv2 describe-listeners \
  --load-balancer-arn "${ALB_ARN}" \
  --query "Listeners[?Port==\`80\`].ListenerArn" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "")

if [ -z "${LISTENER_ARN}" ]; then
  aws elbv2 create-listener \
    --load-balancer-arn "${ALB_ARN}" \
    --protocol HTTP --port 80 \
    --default-actions "Type=forward,TargetGroupArn=${TG_ARN}" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Created HTTP listener on port 80"
else
  echo "✓ HTTP listener already exists"
fi

# ── Create ECS Services ──────────────────────────────────────────────────────
EXISTING_API_SVC=$(aws ecs describe-services \
  --cluster "${ECS_CLUSTER}" --services "${ECS_SERVICE_API}" \
  --query "services[?status=='ACTIVE'].serviceName" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "")

if [ -z "${EXISTING_API_SVC}" ]; then
  aws ecs create-service \
    --cluster "${ECS_CLUSTER}" \
    --service-name "${ECS_SERVICE_API}" \
    --task-definition "${PROJECT}-api" \
    --desired-count 1 \
    --launch-type FARGATE \
    --network-configuration "awsvpcConfiguration={subnets=[${SUBNET_1},${SUBNET_2}],securityGroups=[${APP_SG_ID}],assignPublicIp=ENABLED}" \
    --load-balancers "targetGroupArn=${TG_ARN},containerName=api,containerPort=8080" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Created API ECS service"
else
  echo "✓ API ECS service already exists"
fi

EXISTING_WORKER_SVC=$(aws ecs describe-services \
  --cluster "${ECS_CLUSTER}" --services "${ECS_SERVICE_WORKER}" \
  --query "services[?status=='ACTIVE'].serviceName" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "")

if [ -z "${EXISTING_WORKER_SVC}" ]; then
  aws ecs create-service \
    --cluster "${ECS_CLUSTER}" \
    --service-name "${ECS_SERVICE_WORKER}" \
    --task-definition "${PROJECT}-worker" \
    --desired-count 1 \
    --capacity-provider-strategy "capacityProvider=FARGATE_SPOT,weight=1" \
    --network-configuration "awsvpcConfiguration={subnets=[${SUBNET_1},${SUBNET_2}],securityGroups=[${APP_SG_ID}],assignPublicIp=ENABLED}" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Created Worker ECS service (Fargate Spot)"
else
  echo "✓ Worker ECS service already exists"
fi

echo ""
echo "── Outputs ──────────────────────────────────────────────────────────"
ALB_DNS=$(aws elbv2 describe-load-balancers \
  --load-balancer-arns "${ALB_ARN}" \
  --query "LoadBalancers[0].DNSName" --output text \
  --region "${AWS_REGION}")
echo "ALB_ARN=${ALB_ARN}"
echo "ALB_DNS=${ALB_DNS}"
echo "TG_ARN=${TG_ARN}"
echo ""
echo "API is available at: http://${ALB_DNS}"
