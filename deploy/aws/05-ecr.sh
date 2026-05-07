#!/usr/bin/env bash
# Step 5: Create ECR repositories and push Docker images.
# Cost: ~$0.50/mo for storage

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 5: ECR Repositories ─────────────────────────────────────────"

# Create repos (idempotent)
for repo in "${ECR_REPO_API}" "${ECR_REPO_WORKER}"; do
  aws ecr describe-repositories --repository-names "${repo}" --region "${AWS_REGION}" > /dev/null 2>&1 \
    && echo "✓ Repository '${repo}' already exists" \
    || {
      aws ecr create-repository \
        --repository-name "${repo}" \
        --image-scanning-configuration scanOnPush=true \
        --region "${AWS_REGION}" > /dev/null
      echo "✓ Created repository: ${repo}"
    }
done

# Set lifecycle policy (keep last 10 images to save storage costs)
LIFECYCLE_POLICY='{"rules":[{"rulePriority":1,"description":"Keep last 10 images","selection":{"tagStatus":"any","countType":"imageCountMoreThan","countNumber":10},"action":{"type":"expire"}}]}'

for repo in "${ECR_REPO_API}" "${ECR_REPO_WORKER}"; do
  aws ecr put-lifecycle-policy \
    --repository-name "${repo}" \
    --lifecycle-policy-text "${LIFECYCLE_POLICY}" \
    --region "${AWS_REGION}" > /dev/null
done
echo "✓ Lifecycle policies set (keep last 10 images)"

echo ""
echo "── Build and Push ───────────────────────────────────────────────────"
echo ""
echo "Login to ECR:"
echo "  aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ECR_REGISTRY}"
echo ""
echo "Build and push API image:"
echo "  docker build --target runtime -t ${ECR_REGISTRY}/${ECR_REPO_API}:latest -f Dockerfile ."
echo "  docker push ${ECR_REGISTRY}/${ECR_REPO_API}:latest"
echo ""
echo "Build and push Worker image:"
echo "  docker build --target runtime-worker -t ${ECR_REGISTRY}/${ECR_REPO_WORKER}:latest -f Dockerfile ."
echo "  docker push ${ECR_REGISTRY}/${ECR_REPO_WORKER}:latest"
