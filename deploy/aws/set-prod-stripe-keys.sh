#!/usr/bin/env bash
# Writes the live Stripe trio to /lagedra/prod/* only.
# Refuses sandbox keys. Does not touch /lagedra/staging/* .
#
# Use the live webhook signing secret from a destination whose URL is
# https://api.lagedra.com/v1/webhooks/stripe — not the sandbox whsec_.
#
# Usage:
#   PARAM_STRIPE_PUBLISHABLE_KEY='pk_live_...' \
#   PARAM_STRIPE_SECRET_KEY='sk_live_...' \
#   PARAM_STRIPE_WEBHOOK_SECRET='whsec_...' \
#   bash deploy/aws/set-prod-stripe-keys.sh
#
# Optional apply (retarget running prod API + worker and restart):
#   APPLY=1 bash deploy/aws/set-prod-stripe-keys.sh

set -euo pipefail

REGION="${AWS_REGION:-us-west-1}"
PREFIX="${SSM_PREFIX_PROD:-/lagedra/prod}"
CLUSTER="${ECS_CLUSTER:-lagedra-prod}"
APPLY="${APPLY:-0}"

PUBLISHABLE="${PARAM_STRIPE_PUBLISHABLE_KEY:-}"
SECRET="${PARAM_STRIPE_SECRET_KEY:-}"
WEBHOOK="${PARAM_STRIPE_WEBHOOK_SECRET:-}"

require_prefix() {
  local value="$1"
  local expected="$2"
  local label="$3"
  case "$value" in
    "${expected}"*) ;;
    *)
      echo "$label must start with $expected (refusing sandbox / mismatched keys on live)." >&2
      exit 1
      ;;
  esac
}

if [ -z "$PUBLISHABLE" ] || [ -z "$SECRET" ] || [ -z "$WEBHOOK" ]; then
  echo "Set PARAM_STRIPE_PUBLISHABLE_KEY, PARAM_STRIPE_SECRET_KEY, and PARAM_STRIPE_WEBHOOK_SECRET." >&2
  exit 1
fi

require_prefix "$PUBLISHABLE" "pk_live_" "Publishable key"
require_prefix "$SECRET" "sk_live_" "Secret key"
require_prefix "$WEBHOOK" "whsec_" "Webhook secret"

put_ssm() {
  local name="$1"
  local value="$2"
  aws ssm put-parameter \
    --name "${PREFIX}/${name}" \
    --value "${value}" \
    --type SecureString \
    --overwrite \
    --region "${REGION}" > /dev/null
  echo "Wrote ${PREFIX}/${name}"
}

put_ssm "stripe-publishable-key" "$PUBLISHABLE"
put_ssm "stripe-secret-key" "$SECRET"
put_ssm "stripe-webhook-secret" "$WEBHOOK"

if [ "$APPLY" != "1" ]; then
  echo
  echo "SSM is updated. Running prod tasks still have the previous secret/webhook until they restart."
  echo "Re-run with APPLY=1 to retarget lagedra-api and lagedra-worker and force new deployments."
  echo "Also set the same pk_live_ key on the production web build as VITE_STRIPE_PUBLISHABLE_KEY"
  echo "(GitHub secret STRIPE_PUBLISHABLE_KEY) and redeploy the frontend."
  exit 0
fi

ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
ARN_PREFIX="arn:aws:ssm:${REGION}:${ACCOUNT_ID}:parameter${PREFIX}"

apply_service() {
  local service="$1"
  local task_arn
  task_arn="$(aws ecs describe-services \
    --cluster "${CLUSTER}" \
    --services "${service}" \
    --region "${REGION}" \
    --query 'services[0].taskDefinition' \
    --output text)"

  if [ -z "$task_arn" ] || [ "$task_arn" = "None" ]; then
    echo "Could not find ECS service ${service} on cluster ${CLUSTER}. SSM keys are stored." >&2
    exit 1
  fi

  local tmp clean
  tmp="$(mktemp)"
  clean="$(mktemp)"

  aws ecs describe-task-definition \
    --task-definition "${task_arn}" \
    --region "${REGION}" \
    --query taskDefinition \
    --output json > "$tmp"

  python3 - "$tmp" "$clean" "$ARN_PREFIX" <<'PY'
import json, sys
src, dest, arn_prefix = sys.argv[1], sys.argv[2], sys.argv[3]
raw = json.load(open(src))
container = raw["containerDefinitions"][0]
container["environment"] = [
    e for e in container.get("environment", []) if e.get("name") != "Stripe__PublishableKey"
]
wanted = {
    "Stripe__PublishableKey": f"{arn_prefix}/stripe-publishable-key",
    "Stripe__SecretKey": f"{arn_prefix}/stripe-secret-key",
    "Stripe__WebhookSecret": f"{arn_prefix}/stripe-webhook-secret",
}
secrets = [s for s in container.get("secrets", []) if s.get("name") not in wanted]
secrets.extend({"name": k, "valueFrom": v} for k, v in wanted.items())
container["secrets"] = secrets
keep = (
    "family",
    "taskRoleArn",
    "executionRoleArn",
    "networkMode",
    "containerDefinitions",
    "volumes",
    "requiresCompatibilities",
    "cpu",
    "memory",
    "runtimePlatform",
)
payload = {k: raw[k] for k in keep if k in raw and raw[k] not in (None, [])}
json.dump(payload, open(dest, "w"))
PY

  aws ecs register-task-definition \
    --cli-input-json "file://${clean}" \
    --region "${REGION}" > /dev/null

  local family
  family="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["family"])' "$tmp")"

  aws ecs update-service \
    --cluster "${CLUSTER}" \
    --service "${service}" \
    --task-definition "${family}" \
    --force-new-deployment \
    --region "${REGION}" > /dev/null

  rm -f "$tmp" "$clean"
  echo "Registered a new ${family} revision and forced a deployment of ${service}."
}

apply_service "lagedra-api"
apply_service "lagedra-worker"

echo "Also set the same pk_live_ key on the production web build as VITE_STRIPE_PUBLISHABLE_KEY"
echo "(GitHub secret STRIPE_PUBLISHABLE_KEY) and redeploy the frontend."
