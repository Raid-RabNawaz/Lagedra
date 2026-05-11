#!/usr/bin/env bash
# Step 2: Create RDS PostgreSQL 16 instance.
# Cost: Free tier (750 hrs/mo for 12 months), then ~$13/mo

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 2: RDS PostgreSQL ───────────────────────────────────────────"

# Required inputs
: "${DB_SG_ID:?Set DB_SG_ID from step 1 output}"
: "${RDS_MASTER_PASSWORD:?Set RDS_MASTER_PASSWORD (strong password)}"

# Check if instance already exists
EXISTING=$(aws rds describe-db-instances \
  --db-instance-identifier "${RDS_INSTANCE_ID}" \
  --query "DBInstances[0].DBInstanceStatus" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "not-found")

if [ "${EXISTING}" != "not-found" ]; then
  echo "✓ RDS instance '${RDS_INSTANCE_ID}' already exists (status: ${EXISTING})"
else
  aws rds create-db-instance \
    --db-instance-identifier "${RDS_INSTANCE_ID}" \
    --db-instance-class "${RDS_INSTANCE_CLASS}" \
    --engine postgres \
    --engine-version 16.4 \
    --master-username "${RDS_USERNAME}" \
    --master-user-password "${RDS_MASTER_PASSWORD}" \
    --allocated-storage 20 \
    --storage-type gp3 \
    --vpc-security-group-ids "${DB_SG_ID}" \
    --no-multi-az \
    --no-publicly-accessible \
    --backup-retention-period 7 \
    --storage-encrypted \
    --db-name "${RDS_DB_NAME}" \
    --region "${AWS_REGION}"
  echo "✓ RDS instance creation started (takes ~5-10 minutes)"
fi

echo ""
echo "Wait for the instance to become available:"
echo "  aws rds wait db-instance-available --db-instance-identifier ${RDS_INSTANCE_ID} --region ${AWS_REGION}"
echo ""
echo "Then get the endpoint:"
echo "  aws rds describe-db-instances --db-instance-identifier ${RDS_INSTANCE_ID} --query 'DBInstances[0].Endpoint.Address' --output text --region ${AWS_REGION}"
