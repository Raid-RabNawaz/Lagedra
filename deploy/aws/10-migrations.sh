#!/usr/bin/env bash
# Step 10: Run EF Core database migrations against the RDS instance.
# This temporarily opens the DB security group to your IP.

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 10: Database Migrations ─────────────────────────────────────"

: "${DB_SG_ID:?Set DB_SG_ID from step 1}"
: "${RDS_ENDPOINT:?Set RDS_ENDPOINT from step 2}"
: "${RDS_MASTER_PASSWORD:?Set RDS_MASTER_PASSWORD}"

MY_IP=$(curl -s https://checkip.amazonaws.com)/32
echo "Your public IP: ${MY_IP}"

# Temporarily allow your IP
echo "Opening DB SG to your IP..."
aws ec2 authorize-security-group-ingress \
  --group-id "${DB_SG_ID}" --protocol tcp --port 5432 --cidr "${MY_IP}" \
  --region "${AWS_REGION}" 2>/dev/null || echo "(rule may already exist)"

CONNECTION="Host=${RDS_ENDPOINT};Port=5432;Database=${RDS_DB_NAME};Username=${RDS_USERNAME};Password=${RDS_MASTER_PASSWORD}"

echo "Running migrations..."
cd "${REPO_ROOT}"

dotnet ef database update \
  --project src/Lagedra.ApiGateway \
  --connection "${CONNECTION}" \
  2>&1 || {
    echo "✗ Migration failed. Check the error above."
    echo "Removing your IP from DB SG..."
    aws ec2 revoke-security-group-ingress \
      --group-id "${DB_SG_ID}" --protocol tcp --port 5432 --cidr "${MY_IP}" \
      --region "${AWS_REGION}" 2>/dev/null || true
    exit 1
  }

echo "✓ Migrations applied successfully"

# Revoke access
echo "Removing your IP from DB SG..."
aws ec2 revoke-security-group-ingress \
  --group-id "${DB_SG_ID}" --protocol tcp --port 5432 --cidr "${MY_IP}" \
  --region "${AWS_REGION}" 2>/dev/null || true
echo "✓ DB SG cleaned up"
