#!/usr/bin/env bash
# Writes the sandbox Stripe trio to /lagedra/staging/* only.
# Does not touch /lagedra/prod/* .
#
# Usage:
#   PARAM_STRIPE_PUBLISHABLE_KEY='pk_test_...' \
#   PARAM_STRIPE_SECRET_KEY='sk_test_...' \
#   PARAM_STRIPE_WEBHOOK_SECRET='whsec_...' \
#   bash deploy/aws/set-staging-stripe-keys.sh
#
# Optional apply (retarget running staging API + restart):
#   APPLY=1 bash deploy/aws/set-staging-stripe-keys.sh

set -euo pipefail

REGION="${AWS_REGION:-us-west-1}"
PREFIX="${SSM_PREFIX_STAGING:-/lagedra/staging}"
CLUSTER="${ECS_CLUSTER:-lagedra-prod}"
SERVICE="${ECS_SERVICE_STAGING:-lagedra-api-staging}"
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
      echo "$label must start with $expected (got a different key type)." >&2
      exit 1
      ;;
  esac
}

if [ -z "$PUBLISHABLE" ] || [ -z "$SECRET" ] || [ -z "$WEBHOOK" ]; then
  echo "Set PARAM_STRIPE_PUBLISHABLE_KEY, PARAM_STRIPE_SECRET_KEY, and PARAM_STRIPE_WEBHOOK_SECRET." >&2
  exit 1
fi

require_prefix "$PUBLISHABLE" "pk_test_" "Publishable key"
require_prefix "$SECRET" "sk_test_" "Secret key"
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
  echo "SSM is updated. Staging API still reads the old /lagedra/prod Stripe paths until you apply."
  echo "Re-run with APPLY=1 to retarget ${SERVICE} and force a new deployment."
  echo "Also set the same pk_test_ key on the staging web build as VITE_STRIPE_PUBLISHABLE_KEY."
  exit 0
fi

ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
ARN_PREFIX="arn:aws:ssm:${REGION}:${ACCOUNT_ID}:parameter${PREFIX}"

TASK_ARN="$(aws ecs describe-services \
  --cluster "${CLUSTER}" \
  --services "${SERVICE}" \
  --region "${REGION}" \
  --query 'services[0].taskDefinition' \
  --output text)"

if [ -z "$TASK_ARN" ] || [ "$TASK_ARN" = "None" ]; then
  echo "Could not find ECS service ${SERVICE} on cluster ${CLUSTER}. SSM keys are stored; apply the task definition manually." >&2
  exit 1
fi

TMP="$(mktemp)"
CLEAN="$(mktemp)"
trap 'rm -f "$TMP" "$CLEAN"' EXIT

aws ecs describe-task-definition \
  --task-definition "${TASK_ARN}" \
  --region "${REGION}" \
  --query taskDefinition \
  --output json > "$TMP"

python3 - "$TMP" "$CLEAN" "$ARN_PREFIX" <<'PY'
import json, sys
src, dest, arn_prefix = sys.argv[1], sys.argv[2], sys.argv[3]
raw = json.load(open(src))
container = next(c for c in raw["containerDefinitions"] if c["name"] == "api")
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
  --cli-input-json "file://${CLEAN}" \
  --region "${REGION}" > /dev/null

FAMILY="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["family"])' "$TMP")"

aws ecs update-service \
  --cluster "${CLUSTER}" \
  --service "${SERVICE}" \
  --task-definition "${FAMILY}" \
  --force-new-deployment \
  --region "${REGION}" > /dev/null

echo "Registered a new ${FAMILY} revision and forced a deployment of ${SERVICE}."
echo "Set the same pk_test_ key on the staging web build as VITE_STRIPE_PUBLISHABLE_KEY."
