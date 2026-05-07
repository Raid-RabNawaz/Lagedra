#!/usr/bin/env bash
# Step 9: Configure Route53, ACM certificates, and HTTPS.
# Cost: $0.50/mo for hosted zone + free ACM certs

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

echo "── Step 9: DNS & SSL ────────────────────────────────────────────────"

: "${ALB_ARN:?Set ALB_ARN from step 7}"

# ── ACM Certificate (for CloudFront — must be in us-east-1) ──────────────────
echo "Requesting wildcard certificate for CloudFront (us-east-1)..."
CF_CERT_ARN=$(aws acm list-certificates \
  --query "CertificateSummaryList[?DomainName=='${DOMAIN_NAME}'].CertificateArn" \
  --output text --region us-east-1 2>/dev/null || echo "")

if [ -z "${CF_CERT_ARN}" ]; then
  CF_CERT_ARN=$(aws acm request-certificate \
    --domain-name "${DOMAIN_NAME}" \
    --subject-alternative-names "*.${DOMAIN_NAME}" \
    --validation-method DNS \
    --query "CertificateArn" --output text \
    --region us-east-1)
  echo "✓ Requested certificate: ${CF_CERT_ARN}"
  echo "  → Validate it via DNS (add the CNAME records shown in ACM console)"
else
  echo "✓ Certificate already exists: ${CF_CERT_ARN}"
fi

# ── ACM Certificate (for ALB — in same region as ALB) ────────────────────────
echo ""
echo "Requesting API certificate for ALB (${AWS_REGION})..."
ALB_CERT_ARN=$(aws acm list-certificates \
  --query "CertificateSummaryList[?DomainName=='${API_DOMAIN}'].CertificateArn" \
  --output text --region "${AWS_REGION}" 2>/dev/null || echo "")

if [ -z "${ALB_CERT_ARN}" ]; then
  ALB_CERT_ARN=$(aws acm request-certificate \
    --domain-name "${API_DOMAIN}" \
    --validation-method DNS \
    --query "CertificateArn" --output text \
    --region "${AWS_REGION}")
  echo "✓ Requested certificate: ${ALB_CERT_ARN}"
  echo "  → Validate it via DNS (add the CNAME records shown in ACM console)"
else
  echo "✓ Certificate already exists: ${ALB_CERT_ARN}"
fi

# ── Route53 hosted zone ─────────────────────────────────────────────────────
echo ""
ZONE_ID=$(aws route53 list-hosted-zones-by-name \
  --dns-name "${DOMAIN_NAME}" \
  --query "HostedZones[?Name=='${DOMAIN_NAME}.'].Id" --output text 2>/dev/null || echo "")

if [ -z "${ZONE_ID}" ]; then
  ZONE_ID=$(aws route53 create-hosted-zone \
    --name "${DOMAIN_NAME}" \
    --caller-reference "$(date +%s)" \
    --query "HostedZone.Id" --output text)
  echo "✓ Created hosted zone: ${ZONE_ID}"
  echo "  → Update your domain registrar's nameservers to the ones shown in Route53"
else
  echo "✓ Hosted zone already exists: ${ZONE_ID}"
fi

# ── Add HTTPS listener to ALB ───────────────────────────────────────────────
echo ""
echo "After the ALB certificate is validated, add the HTTPS listener:"
echo ""
echo "  # Get target group ARN"
echo "  TG_ARN=\$(aws elbv2 describe-target-groups --names ${TG_NAME} --query 'TargetGroups[0].TargetGroupArn' --output text --region ${AWS_REGION})"
echo ""
echo "  # Create HTTPS listener"
echo "  aws elbv2 create-listener \\"
echo "    --load-balancer-arn ${ALB_ARN} \\"
echo "    --protocol HTTPS --port 443 \\"
echo "    --certificates CertificateArn=${ALB_CERT_ARN} \\"
echo "    --default-actions Type=forward,TargetGroupArn=\${TG_ARN} \\"
echo "    --region ${AWS_REGION}"
echo ""
echo "  # Redirect HTTP → HTTPS"
echo "  HTTP_LISTENER=\$(aws elbv2 describe-listeners --load-balancer-arn ${ALB_ARN} --query \"Listeners[?Port==\\\`80\\\`].ListenerArn\" --output text --region ${AWS_REGION})"
echo "  aws elbv2 modify-listener --listener-arn \${HTTP_LISTENER} \\"
echo "    --default-actions 'Type=redirect,RedirectConfig={Protocol=HTTPS,Port=443,StatusCode=HTTP_301}' \\"
echo "    --region ${AWS_REGION}"

echo ""
echo "── Outputs ──────────────────────────────────────────────────────────"
echo "CF_CERT_ARN=${CF_CERT_ARN}"
echo "ALB_CERT_ARN=${ALB_CERT_ARN}"
echo "ZONE_ID=${ZONE_ID}"
