#!/usr/bin/env bash
# Step 4: Store secrets in SSM Parameter Store (free) and Secrets Manager.
# Cost: ~$0.40/mo for Secrets Manager (DB password only)

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 4: Secrets ──────────────────────────────────────────────────"
echo ""
echo "This script creates SSM parameters. You must provide the actual values."
echo "Run with environment variables set, e.g.:"
echo ""
echo "  PARAM_JWT_SECRET='your-secret' \\"
echo "  PARAM_STRIPE_SECRET_KEY='sk_live_...' \\"
echo "  PARAM_STRIPE_WEBHOOK_SECRET='whsec_...' \\"
echo "  PARAM_BREVO_PASSWORD='...' \\"
echo "  PARAM_TWILIO_ACCOUNT_SID='ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx' \\"
echo "  PARAM_TWILIO_AUTH_TOKEN='...' \\"
echo "  PARAM_TWILIO_MESSAGING_SERVICE_SID='MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx' \\"
echo "  PARAM_GOOGLE_MAPS_KEY='AIza...' \\"
echo "  PARAM_PERSONA_API_KEY='persona_...' \\"
echo "  PARAM_ENCRYPTION_KEY='...' \\"
echo "  PARAM_SIGNING_SECRET='...' \\"
echo "  RDS_MASTER_PASSWORD='...' \\"
echo "  bash deploy/aws/04-secrets.sh"
echo ""

put_ssm_param() {
  local name="$1"
  local value="$2"
  if [ -z "${value}" ] || [ "${value}" = "" ]; then
    echo "⚠ Skipping ${name} (no value provided)"
    return
  fi
  aws ssm put-parameter \
    --name "${SSM_PREFIX}/${name}" \
    --value "${value}" \
    --type SecureString \
    --overwrite \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ ${SSM_PREFIX}/${name}"
}

# SSM Parameter Store (free for standard parameters)
put_ssm_param "jwt-secret"            "${PARAM_JWT_SECRET:-}"
put_ssm_param "stripe-secret-key"     "${PARAM_STRIPE_SECRET_KEY:-}"
put_ssm_param "stripe-webhook-secret" "${PARAM_STRIPE_WEBHOOK_SECRET:-}"
put_ssm_param "brevo-password"        "${PARAM_BREVO_PASSWORD:-}"
put_ssm_param "twilio-account-sid"    "${PARAM_TWILIO_ACCOUNT_SID:-}"
put_ssm_param "twilio-auth-token"     "${PARAM_TWILIO_AUTH_TOKEN:-}"
put_ssm_param "twilio-messaging-service-sid" "${PARAM_TWILIO_MESSAGING_SERVICE_SID:-}"
put_ssm_param "google-maps-key"       "${PARAM_GOOGLE_MAPS_KEY:-}"
put_ssm_param "persona-api-key"       "${PARAM_PERSONA_API_KEY:-}"
put_ssm_param "encryption-key"        "${PARAM_ENCRYPTION_KEY:-}"
put_ssm_param "signing-secret"        "${PARAM_SIGNING_SECRET:-}"

# DB password in Secrets Manager (~$0.40/secret/month)
if [ -n "${RDS_MASTER_PASSWORD:-}" ]; then
  aws secretsmanager create-secret \
    --name "${SSM_PREFIX}/db-password" \
    --secret-string "${RDS_MASTER_PASSWORD}" \
    --region "${AWS_REGION}" 2>/dev/null \
  || aws secretsmanager update-secret \
    --secret-id "${SSM_PREFIX}/db-password" \
    --secret-string "${RDS_MASTER_PASSWORD}" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Secrets Manager: ${SSM_PREFIX}/db-password"
else
  echo "⚠ Skipping DB password (RDS_MASTER_PASSWORD not set)"
fi

echo ""
echo "Done. Secrets are stored at prefix: ${SSM_PREFIX}/"
