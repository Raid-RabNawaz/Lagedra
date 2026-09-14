#!/usr/bin/env bash
# Step 11: Give the API and worker a STABLE OUTBOUND IP.
#
# Fargate tasks currently run in the default VPC's public subnets with
# assignPublicIp=ENABLED, so every task gets a fresh ephemeral public IP and
# third parties see a different source address after each deploy or scale event.
#
# Streamline VRS allow-lists partner IPs and refuses calls from anywhere else
# (see docs/integrations/streamline/README.md), so we need a fixed egress
# address. This script moves the ECS services into private subnets whose default
# route is a NAT Gateway holding a single Elastic IP. That Elastic IP becomes the
# one address every outbound call originates from.
#
# Cost: NAT Gateway is ~$0.045/hr (~$33/mo) in us-west-1 plus ~$0.045/GB
# processed. An S3 gateway endpoint is created alongside it (free) so bucket
# traffic bypasses the NAT and does not incur processing charges.
#
# Safe to re-run: every resource is looked up by tag before being created.

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "${SCRIPT_DIR}/config.sh"

# Services to move behind the NAT. Override to migrate them one at a time.
EGRESS_SERVICES="${EGRESS_SERVICES:-${ECS_SERVICE_API} ${ECS_SERVICE_WORKER} ${ECS_SERVICE_API}-staging}"

NAT_NAME="${PROJECT}-nat"
EIP_NAME="${PROJECT}-nat-eip"
PRIVATE_RT_NAME="${PROJECT}-private-rt"
PRIVATE_SUBNET_PREFIX="${PROJECT}-private"

echo "── Step 11: Stable egress IP (NAT Gateway) ─────────────────────────"

# ── Locate the VPC and its public subnets ───────────────────────────────────
VPC_ID=$(aws ec2 describe-vpcs \
  --filters "Name=is-default,Values=true" \
  --query "Vpcs[0].VpcId" --output text \
  --region "${AWS_REGION}")

if [ "${VPC_ID}" = "None" ] || [ -z "${VPC_ID}" ]; then
  echo "✗ No default VPC found. Run 01-networking.sh first."
  exit 1
fi

VPC_CIDR=$(aws ec2 describe-vpcs --vpc-ids "${VPC_ID}" \
  --query "Vpcs[0].CidrBlock" --output text --region "${AWS_REGION}")
echo "✓ VPC: ${VPC_ID} (${VPC_CIDR})"

# A public subnet is needed to host the NAT Gateway itself.
PUBLIC_SUBNET_ID=$(aws ec2 describe-subnets \
  --filters "Name=vpc-id,Values=${VPC_ID}" "Name=default-for-az,Values=true" \
  --query "Subnets[0].SubnetId" --output text \
  --region "${AWS_REGION}")

if [ "${PUBLIC_SUBNET_ID}" = "None" ] || [ -z "${PUBLIC_SUBNET_ID}" ]; then
  echo "✗ No default (public) subnet found to host the NAT Gateway."
  exit 1
fi
echo "✓ NAT will live in public subnet: ${PUBLIC_SUBNET_ID}"

# Two AZs, because ECS services span two subnets.
# The AWS CLI on Windows emits CRLF, and a stray \r survives into array elements,
# which AWS then rejects with a confusing "availabilityZone is invalid" error
# that lists the very zone you passed. Strip carriage returns explicitly.
mapfile -t AZS < <(aws ec2 describe-subnets \
  --filters "Name=vpc-id,Values=${VPC_ID}" "Name=default-for-az,Values=true" \
  --query "Subnets[*].AvailabilityZone" --output text \
  --region "${AWS_REGION}" | tr -d '\r' | tr '\t' '\n' | sort -u | head -2)

if [ "${#AZS[@]}" -lt 2 ]; then
  echo "✗ Need at least 2 availability zones, found ${#AZS[@]}."
  exit 1
fi
echo "✓ Availability zones: ${AZS[*]}"

# ── Private subnets ─────────────────────────────────────────────────────────
# Default VPC subnets are aligned /20s starting at .0, .16, .32 … so the upper
# blocks are free. Override PRIVATE_CIDR_1/2 if your VPC is laid out differently.
VPC_PREFIX="$(echo "${VPC_CIDR}" | cut -d. -f1-2)"
PRIVATE_CIDR_1="${PRIVATE_CIDR_1:-${VPC_PREFIX}.128.0/20}"
PRIVATE_CIDR_2="${PRIVATE_CIDR_2:-${VPC_PREFIX}.144.0/20}"

create_private_subnet() {
  local az="$1" cidr="$2" name="$3" subnet_id

  subnet_id=$(aws ec2 describe-subnets \
    --filters "Name=vpc-id,Values=${VPC_ID}" "Name=tag:Name,Values=${name}" \
    --query "Subnets[0].SubnetId" --output text \
    --region "${AWS_REGION}" 2>/dev/null || echo "None")

  if [ "${subnet_id}" != "None" ] && [ -n "${subnet_id}" ]; then
    echo "${subnet_id}"
    return
  fi

  subnet_id=$(aws ec2 create-subnet \
    --vpc-id "${VPC_ID}" \
    --cidr-block "${cidr}" \
    --availability-zone "${az}" \
    --tag-specifications "ResourceType=subnet,Tags=[{Key=Name,Value=${name}},{Key=Project,Value=${PROJECT}},{Key=Tier,Value=private}]" \
    --query "Subnet.SubnetId" --output text \
    --region "${AWS_REGION}")

  echo "${subnet_id}" | tr -d '\r'
}

require_subnet() {
  local label="$1" subnet_id="$2" az="$3"
  if [ -z "${subnet_id}" ] || [ "${subnet_id}" = "None" ]; then
    echo "✗ Failed to create ${label} in ${az}. Nothing further was changed;"
    echo "  re-run once the cause above is resolved (the script is idempotent)."
    exit 1
  fi
}

PRIVATE_SUBNET_1=$(create_private_subnet "${AZS[0]}" "${PRIVATE_CIDR_1}" "${PRIVATE_SUBNET_PREFIX}-${AZS[0]}")
require_subnet "private subnet A" "${PRIVATE_SUBNET_1}" "${AZS[0]}"
echo "✓ Private subnet A: ${PRIVATE_SUBNET_1} (${AZS[0]})"
PRIVATE_SUBNET_2=$(create_private_subnet "${AZS[1]}" "${PRIVATE_CIDR_2}" "${PRIVATE_SUBNET_PREFIX}-${AZS[1]}")
require_subnet "private subnet B" "${PRIVATE_SUBNET_2}" "${AZS[1]}"
echo "✓ Private subnet B: ${PRIVATE_SUBNET_2} (${AZS[1]})"

# ── Elastic IP — this is the address Streamline allow-lists ─────────────────
EIP_ALLOC_ID=$(aws ec2 describe-addresses \
  --filters "Name=tag:Name,Values=${EIP_NAME}" \
  --query "Addresses[0].AllocationId" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${EIP_ALLOC_ID}" = "None" ] || [ -z "${EIP_ALLOC_ID}" ]; then
  EIP_ALLOC_ID=$(aws ec2 allocate-address \
    --domain vpc \
    --tag-specifications "ResourceType=elastic-ip,Tags=[{Key=Name,Value=${EIP_NAME}},{Key=Project,Value=${PROJECT}}]" \
    --query "AllocationId" --output text \
    --region "${AWS_REGION}")
  echo "✓ Allocated Elastic IP: ${EIP_ALLOC_ID}"
else
  echo "✓ Reusing Elastic IP: ${EIP_ALLOC_ID}"
fi

EGRESS_IP=$(aws ec2 describe-addresses \
  --allocation-ids "${EIP_ALLOC_ID}" \
  --query "Addresses[0].PublicIp" --output text \
  --region "${AWS_REGION}")

# ── NAT Gateway ─────────────────────────────────────────────────────────────
NAT_ID=$(aws ec2 describe-nat-gateways \
  --filter "Name=vpc-id,Values=${VPC_ID}" "Name=tag:Name,Values=${NAT_NAME}" \
           "Name=state,Values=pending,available" \
  --query "NatGateways[0].NatGatewayId" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${NAT_ID}" = "None" ] || [ -z "${NAT_ID}" ]; then
  NAT_ID=$(aws ec2 create-nat-gateway \
    --subnet-id "${PUBLIC_SUBNET_ID}" \
    --allocation-id "${EIP_ALLOC_ID}" \
    --tag-specifications "ResourceType=natgateway,Tags=[{Key=Name,Value=${NAT_NAME}},{Key=Project,Value=${PROJECT}}]" \
    --query "NatGateway.NatGatewayId" --output text \
    --region "${AWS_REGION}")
  echo "✓ Created NAT Gateway: ${NAT_ID}"
else
  echo "✓ Reusing NAT Gateway: ${NAT_ID}"
fi

echo "  Waiting for NAT Gateway to become available (this takes a few minutes)…"
aws ec2 wait nat-gateway-available --nat-gateway-ids "${NAT_ID}" --region "${AWS_REGION}"
echo "✓ NAT Gateway available"

# ── Private route table ─────────────────────────────────────────────────────
PRIVATE_RT_ID=$(aws ec2 describe-route-tables \
  --filters "Name=vpc-id,Values=${VPC_ID}" "Name=tag:Name,Values=${PRIVATE_RT_NAME}" \
  --query "RouteTables[0].RouteTableId" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${PRIVATE_RT_ID}" = "None" ] || [ -z "${PRIVATE_RT_ID}" ]; then
  PRIVATE_RT_ID=$(aws ec2 create-route-table \
    --vpc-id "${VPC_ID}" \
    --tag-specifications "ResourceType=route-table,Tags=[{Key=Name,Value=${PRIVATE_RT_NAME}},{Key=Project,Value=${PROJECT}}]" \
    --query "RouteTable.RouteTableId" --output text \
    --region "${AWS_REGION}")
  echo "✓ Created private route table: ${PRIVATE_RT_ID}"
else
  echo "✓ Reusing private route table: ${PRIVATE_RT_ID}"
fi

# Point the default route at the NAT. replace-route covers the case where the
# NAT was recreated and the route still references a dead gateway.
if aws ec2 create-route \
  --route-table-id "${PRIVATE_RT_ID}" \
  --destination-cidr-block "0.0.0.0/0" \
  --nat-gateway-id "${NAT_ID}" \
  --region "${AWS_REGION}" > /dev/null 2>&1; then
  echo "✓ Default route → NAT Gateway"
else
  aws ec2 replace-route \
    --route-table-id "${PRIVATE_RT_ID}" \
    --destination-cidr-block "0.0.0.0/0" \
    --nat-gateway-id "${NAT_ID}" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Default route updated → NAT Gateway"
fi

for subnet in "${PRIVATE_SUBNET_1}" "${PRIVATE_SUBNET_2}"; do
  CURRENT_RT=$(aws ec2 describe-route-tables \
    --filters "Name=association.subnet-id,Values=${subnet}" \
    --query "RouteTables[0].RouteTableId" --output text \
    --region "${AWS_REGION}" 2>/dev/null || echo "None")

  if [ "${CURRENT_RT}" != "${PRIVATE_RT_ID}" ]; then
    aws ec2 associate-route-table \
      --route-table-id "${PRIVATE_RT_ID}" \
      --subnet-id "${subnet}" \
      --region "${AWS_REGION}" > /dev/null
    echo "✓ Associated ${subnet} with private route table"
  else
    echo "✓ ${subnet} already routed through NAT"
  fi
done

# ── S3 gateway endpoint (free — keeps bucket traffic off the NAT) ───────────
S3_ENDPOINT_ID=$(aws ec2 describe-vpc-endpoints \
  --filters "Name=vpc-id,Values=${VPC_ID}" "Name=service-name,Values=com.amazonaws.${AWS_REGION}.s3" \
  --query "VpcEndpoints[0].VpcEndpointId" --output text \
  --region "${AWS_REGION}" 2>/dev/null || echo "None")

if [ "${S3_ENDPOINT_ID}" = "None" ] || [ -z "${S3_ENDPOINT_ID}" ]; then
  aws ec2 create-vpc-endpoint \
    --vpc-id "${VPC_ID}" \
    --service-name "com.amazonaws.${AWS_REGION}.s3" \
    --route-table-ids "${PRIVATE_RT_ID}" \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ Created S3 gateway endpoint"
else
  aws ec2 modify-vpc-endpoint \
    --vpc-endpoint-id "${S3_ENDPOINT_ID}" \
    --add-route-table-ids "${PRIVATE_RT_ID}" \
    --region "${AWS_REGION}" > /dev/null 2>&1 || true
  echo "✓ S3 gateway endpoint already exists"
fi

# ── Move ECS services into the private subnets ──────────────────────────────
# Each service keeps whichever security groups it already has — only the subnets
# and public-IP assignment change.
for service in ${EGRESS_SERVICES}; do
  EXISTS=$(aws ecs describe-services \
    --cluster "${ECS_CLUSTER}" --services "${service}" \
    --query "services[?status=='ACTIVE'].serviceName" --output text \
    --region "${AWS_REGION}" 2>/dev/null || echo "")

  if [ -z "${EXISTS}" ]; then
    echo "• Skipping ${service} (not an active service)"
    continue
  fi

  SERVICE_SGS=$(aws ecs describe-services \
    --cluster "${ECS_CLUSTER}" --services "${service}" \
    --query "services[0].networkConfiguration.awsvpcConfiguration.securityGroups" \
    --output text --region "${AWS_REGION}" | tr -d '\r' | tr '\t' ',')

  if [ -z "${SERVICE_SGS}" ] || [ "${SERVICE_SGS}" = "None" ]; then
    SERVICE_SGS=$(aws ec2 describe-security-groups \
      --filters "Name=group-name,Values=${SG_APP_NAME}" "Name=vpc-id,Values=${VPC_ID}" \
      --query "SecurityGroups[0].GroupId" --output text \
      --region "${AWS_REGION}")
  fi

  aws ecs update-service \
    --cluster "${ECS_CLUSTER}" \
    --service "${service}" \
    --network-configuration "awsvpcConfiguration={subnets=[${PRIVATE_SUBNET_1},${PRIVATE_SUBNET_2}],securityGroups=[${SERVICE_SGS}],assignPublicIp=DISABLED}" \
    --force-new-deployment \
    --region "${AWS_REGION}" > /dev/null
  echo "✓ ${service} → private subnets (sg: ${SERVICE_SGS}), assignPublicIp=DISABLED"
done

echo ""
echo "── Outputs ──────────────────────────────────────────────────────────"
echo "EGRESS_IP=${EGRESS_IP}          ← give this to Streamline"
echo "EIP_ALLOC_ID=${EIP_ALLOC_ID}"
echo "NAT_ID=${NAT_ID}"
echo "PRIVATE_SUBNET_1=${PRIVATE_SUBNET_1}"
echo "PRIVATE_SUBNET_2=${PRIVATE_SUBNET_2}"
echo "PRIVATE_RT_ID=${PRIVATE_RT_ID}"
echo ""
echo "Services are redeploying. Confirm the new egress IP once they stabilise:"
echo "  aws ecs wait services-stable --cluster ${ECS_CLUSTER} --services ${ECS_SERVICE_API} --region ${AWS_REGION}"
echo "  curl https://api.${DOMAIN_NAME}/health"
echo ""
echo "Then verify the source address the outside world sees matches ${EGRESS_IP}."
echo "Register it at https://partner.streamlinevrs.com/admin_pages/allowed_ips"
echo "(Streamline must seed the first entry for us — send them ${EGRESS_IP}.)"
