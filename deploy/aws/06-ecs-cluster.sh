#!/usr/bin/env bash
# Step 6: Create ECS cluster and IAM roles.
# Cost: Free (cluster itself is free; you pay per running task)

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 6: ECS Cluster & IAM ────────────────────────────────────────"

# Create ECS cluster
EXISTING_CLUSTER=$(aws ecs describe-clusters \
  --clusters "${ECS_CLUSTER}" \
  --query "clusters[?status=='ACTIVE'].clusterName" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "")

if [ -n "${EXISTING_CLUSTER}" ]; then
  echo "✓ ECS cluster '${ECS_CLUSTER}' already exists"
else
  aws ecs create-cluster \
    --cluster-name "${ECS_CLUSTER}" \
    --capacity-providers FARGATE FARGATE_SPOT \
    --default-capacity-provider-strategy \
      "capacityProvider=FARGATE,weight=1" \
      "capacityProvider=FARGATE_SPOT,weight=1" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Created ECS cluster: ${ECS_CLUSTER}"
fi

# ── Task Execution Role ─────────────────────────────────────────────────────
EXEC_ROLE_NAME="${PROJECT}-ecs-execution"
TRUST_POLICY='{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"ecs-tasks.amazonaws.com"},"Action":"sts:AssumeRole"}]}'

if aws iam get-role --role-name "${EXEC_ROLE_NAME}" > /dev/null 2>&1; then
  echo "✓ Execution role '${EXEC_ROLE_NAME}' already exists"
else
  aws iam create-role \
    --role-name "${EXEC_ROLE_NAME}" \
    --assume-role-policy-document "${TRUST_POLICY}" > /dev/null
  echo "✓ Created execution role: ${EXEC_ROLE_NAME}"
fi

aws iam attach-role-policy \
  --role-name "${EXEC_ROLE_NAME}" \
  --policy-arn "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy" 2>/dev/null || true

# Allow execution role to read SSM parameters (for ECS secrets injection)
EXEC_SSM_POLICY='{
  "Version":"2012-10-17",
  "Statement":[
    {
      "Effect":"Allow",
      "Action":["ssm:GetParameters","ssm:GetParameter"],
      "Resource":"arn:aws:ssm:'"${AWS_REGION}"':'"${AWS_ACCOUNT_ID}"':parameter'"${SSM_PREFIX}"'/*"
    },
    {
      "Effect":"Allow",
      "Action":["secretsmanager:GetSecretValue"],
      "Resource":"arn:aws:secretsmanager:'"${AWS_REGION}"':'"${AWS_ACCOUNT_ID}"':secret:'"${SSM_PREFIX}"'/*"
    }
  ]
}'

aws iam put-role-policy \
  --role-name "${EXEC_ROLE_NAME}" \
  --policy-name "${PROJECT}-exec-secrets" \
  --policy-document "${EXEC_SSM_POLICY}" 2>/dev/null || true
echo "✓ Execution role policies attached (ECS + SSM + Secrets Manager)"

# ── Task Role (runtime permissions) ──────────────────────────────────────────
TASK_ROLE_NAME="${PROJECT}-task-role"

if aws iam get-role --role-name "${TASK_ROLE_NAME}" > /dev/null 2>&1; then
  echo "✓ Task role '${TASK_ROLE_NAME}' already exists"
else
  aws iam create-role \
    --role-name "${TASK_ROLE_NAME}" \
    --assume-role-policy-document "${TRUST_POLICY}" > /dev/null
  echo "✓ Created task role: ${TASK_ROLE_NAME}"
fi

aws iam put-role-policy \
  --role-name "${TASK_ROLE_NAME}" \
  --policy-name "${PROJECT}-task-policy" \
  --policy-document file://"${SCRIPT_DIR}/task-role-policy.json" 2>/dev/null || true
echo "✓ Task role policy attached (S3 + SSM)"

echo ""
echo "── Outputs ──────────────────────────────────────────────────────────"
echo "ECS_CLUSTER=${ECS_CLUSTER}"
echo "EXEC_ROLE_ARN=arn:aws:iam::${AWS_ACCOUNT_ID}:role/${EXEC_ROLE_NAME}"
echo "TASK_ROLE_ARN=arn:aws:iam::${AWS_ACCOUNT_ID}:role/${TASK_ROLE_NAME}"
