#!/usr/bin/env bash
# Step 3: Create S3 buckets (replacing MinIO).
# Cost: ~$0.50/mo

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 3: S3 Buckets ───────────────────────────────────────────────"

create_private_bucket() {
  local bucket_name="$1"
  if aws s3api head-bucket --bucket "${bucket_name}" --region "${AWS_REGION}" 2>/dev/null; then
    echo "✓ Bucket '${bucket_name}' already exists"
  else
    aws s3 mb "s3://${bucket_name}" --region "${AWS_REGION}"
    aws s3api put-public-access-block \
      --bucket "${bucket_name}" \
      --public-access-block-configuration \
      "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true" \
      --region "${AWS_REGION}"
    echo "✓ Created private bucket: ${bucket_name}"
  fi
}

# Evidence and exports buckets (private)
create_private_bucket "${S3_EVIDENCE_BUCKET}"
create_private_bucket "${S3_EXPORTS_BUCKET}"

# Web SPA bucket (will be served through CloudFront)
if aws s3api head-bucket --bucket "${S3_WEB_BUCKET}" --region "${AWS_REGION}" 2>/dev/null; then
  echo "✓ Bucket '${S3_WEB_BUCKET}' already exists"
else
  aws s3 mb "s3://${S3_WEB_BUCKET}" --region "${AWS_REGION}"
  echo "✓ Created web bucket: ${S3_WEB_BUCKET}"
fi

echo ""
echo "── Outputs ──────────────────────────────────────────────────────────"
echo "S3_EVIDENCE_BUCKET=${S3_EVIDENCE_BUCKET}"
echo "S3_EXPORTS_BUCKET=${S3_EXPORTS_BUCKET}"
echo "S3_WEB_BUCKET=${S3_WEB_BUCKET}"
