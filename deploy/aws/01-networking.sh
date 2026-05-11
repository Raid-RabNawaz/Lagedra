#!/usr/bin/env bash
# Step 1: Set up VPC security groups using the default VPC.
# Cost: Free

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 1: Networking ──────────────────────────────────────────────"

# Get default VPC
VPC_ID=$(aws ec2 describe-vpcs \
  --filters "Name=is-default,Values=true" \
  --query "Vpcs[0].VpcId" --output text \
  --region "${AWS_REGION}")

if [ "${VPC_ID}" = "None" ] || [ -z "${VPC_ID}" ]; then
  echo "✗ No default VPC found. Create one with: aws ec2 create-default-vpc"
  exit 1
fi
echo "✓ Default VPC: ${VPC_ID}"

# Get subnets (need at least 2 for ALB)
SUBNET_IDS=$(aws ec2 describe-subnets \
  --filters "Name=vpc-id,Values=${VPC_ID}" \
  --query "Subnets[*].SubnetId" --output text \
  --region "${AWS_REGION}")
echo "✓ Subnets: ${SUBNET_IDS}"

# Create application security group (idempotent check)
APP_SG_ID=$(aws ec2 describe-security-groups \
  --filters "Name=group-name,Values=${SG_APP_NAME}" "Name=vpc-id,Values=${VPC_ID}" \
  --query "SecurityGroups[0].GroupId" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${APP_SG_ID}" = "None" ] || [ -z "${APP_SG_ID}" ]; then
  APP_SG_ID=$(aws ec2 create-security-group \
    --group-name "${SG_APP_NAME}" \
    --description "Lagedra application services" \
    --vpc-id "${VPC_ID}" \
    --query "GroupId" --output text \
    --region "${AWS_REGION}")
  echo "✓ Created app SG: ${APP_SG_ID}"

  aws ec2 authorize-security-group-ingress \
    --group-id "${APP_SG_ID}" --protocol tcp --port 80 --cidr 0.0.0.0/0 \
    --region "${AWS_REGION}" 2>/dev/null || true
  aws ec2 authorize-security-group-ingress \
    --group-id "${APP_SG_ID}" --protocol tcp --port 443 --cidr 0.0.0.0/0 \
    --region "${AWS_REGION}" 2>/dev/null || true
  aws ec2 authorize-security-group-ingress \
    --group-id "${APP_SG_ID}" --protocol tcp --port 8080 --cidr 0.0.0.0/0 \
    --region "${AWS_REGION}" 2>/dev/null || true
  echo "✓ App SG ingress rules added (80, 443, 8080)"
else
  echo "✓ App SG already exists: ${APP_SG_ID}"
fi

# Create database security group
DB_SG_ID=$(aws ec2 describe-security-groups \
  --filters "Name=group-name,Values=${SG_DB_NAME}" "Name=vpc-id,Values=${VPC_ID}" \
  --query "SecurityGroups[0].GroupId" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${DB_SG_ID}" = "None" ] || [ -z "${DB_SG_ID}" ]; then
  DB_SG_ID=$(aws ec2 create-security-group \
    --group-name "${SG_DB_NAME}" \
    --description "Lagedra database — only reachable from app SG" \
    --vpc-id "${VPC_ID}" \
    --query "GroupId" --output text \
    --region "${AWS_REGION}")
  echo "✓ Created DB SG: ${DB_SG_ID}"

  aws ec2 authorize-security-group-ingress \
    --group-id "${DB_SG_ID}" --protocol tcp --port 5432 \
    --source-group "${APP_SG_ID}" \
    --region "${AWS_REGION}" 2>/dev/null || true
  echo "✓ DB SG ingress: port 5432 from app SG only"
else
  echo "✓ DB SG already exists: ${DB_SG_ID}"
fi

echo ""
echo "── Outputs ──────────────────────────────────────────────────────────"
echo "VPC_ID=${VPC_ID}"
echo "APP_SG_ID=${APP_SG_ID}"
echo "DB_SG_ID=${DB_SG_ID}"
echo "SUBNET_IDS=${SUBNET_IDS}"
echo ""
echo "Save these values — they are needed by subsequent scripts."
