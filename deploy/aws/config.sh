#!/usr/bin/env bash
# Shared configuration for all AWS deployment scripts.
# Edit these values ONCE before running any script.

set -euo pipefail

# ── AWS basics ──────────────────────────────────────────────────────────────
export AWS_REGION="${AWS_REGION:-us-west-1}"
export AWS_ACCOUNT_ID="${AWS_ACCOUNT_ID:-$(aws sts get-caller-identity --query Account --output text 2>/dev/null || echo 'UNKNOWN')}"

# ── Naming ──────────────────────────────────────────────────────────────────
export PROJECT="lagedra"
export ENV_NAME="${ENV_NAME:-prod}"

# ── ECR ─────────────────────────────────────────────────────────────────────
export ECR_REGISTRY="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
export ECR_REPO_API="${PROJECT}/api"
export ECR_REPO_WORKER="${PROJECT}/worker"

# ── ECS ─────────────────────────────────────────────────────────────────────
export ECS_CLUSTER="${PROJECT}-${ENV_NAME}"
export ECS_SERVICE_API="${PROJECT}-api"
export ECS_SERVICE_WORKER="${PROJECT}-worker"

# ── RDS ─────────────────────────────────────────────────────────────────────
export RDS_INSTANCE_ID="${PROJECT}-db"
export RDS_DB_NAME="${PROJECT}_db"
export RDS_USERNAME="${PROJECT}"
export RDS_INSTANCE_CLASS="db.t4g.micro"

# ── S3 ──────────────────────────────────────────────────────────────────────
export S3_EVIDENCE_BUCKET="${PROJECT}-evidence-${ENV_NAME}"
export S3_EXPORTS_BUCKET="${PROJECT}-exports-${ENV_NAME}"
export S3_WEB_BUCKET="${PROJECT}-web-${ENV_NAME}"

# ── Networking ──────────────────────────────────────────────────────────────
export SG_APP_NAME="${PROJECT}-app-sg"
export SG_DB_NAME="${PROJECT}-db-sg"
export ALB_NAME="${PROJECT}-alb"
export TG_NAME="${PROJECT}-api-tg"

# ── Domain (update these when you have a domain) ────────────────────────────
export DOMAIN_NAME="${DOMAIN_NAME:-lagedra.com}"
export API_DOMAIN="api.${DOMAIN_NAME}"

# ── SSM / Secrets paths ────────────────────────────────────────────────────
export SSM_PREFIX="/${PROJECT}/${ENV_NAME}"

echo "✓ Config loaded — region=${AWS_REGION} account=${AWS_ACCOUNT_ID} env=${ENV_NAME}"
