#!/usr/bin/env bash
# Step 8: Build and deploy frontend SPA to S3 + CloudFront.
# Cost: ~$1-2/mo

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 8: Frontend Deployment ──────────────────────────────────────"

# ── Build the SPA ────────────────────────────────────────────────────────────
echo "Building web app..."
cd "${REPO_ROOT}/apps/web"

cat > .env.production <<ENVEOF
VITE_API_BASE_URL=https://${API_DOMAIN}
VITE_GOOGLE_CLIENT_ID=${VITE_GOOGLE_CLIENT_ID:-}
VITE_STRIPE_PUBLISHABLE_KEY=${VITE_STRIPE_PUBLISHABLE_KEY:-}
VITE_GOOGLE_MAPS_API_KEY=${VITE_GOOGLE_MAPS_API_KEY:-}
ENVEOF

pnpm install --frozen-lockfile
pnpm build
echo "✓ Frontend built"

# ── Upload to S3 ─────────────────────────────────────────────────────────────
aws s3 sync dist/ "s3://${S3_WEB_BUCKET}" --delete --region "${AWS_REGION}"
echo "✓ Uploaded to s3://${S3_WEB_BUCKET}"

# ── Create CloudFront OAC + Distribution ─────────────────────────────────────
echo ""
echo "To complete frontend setup, create a CloudFront distribution:"
echo ""
echo "  1. Go to AWS Console → CloudFront → Create Distribution"
echo "  2. Origin: ${S3_WEB_BUCKET}.s3.${AWS_REGION}.amazonaws.com"
echo "  3. Origin Access: Origin Access Control (OAC) — create new"
echo "  4. Default root object: index.html"
echo "  5. Add custom error response: 403 → /index.html (200), 404 → /index.html (200)"
echo "  6. Alternate domain name (CNAME): ${DOMAIN_NAME}"
echo "  7. SSL certificate: select the ACM cert from step 9"
echo "  8. After creation, update S3 bucket policy with the OAC policy CloudFront provides"
echo ""
echo "Or use the CLI (requires a distribution config JSON — see deploy/aws/cloudfront-config.json)"

cd "${REPO_ROOT}"
