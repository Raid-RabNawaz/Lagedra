#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"

declare -A CONTEXTS=(
  ["src/Lagedra.Auth"]="AuthDbContext"
  ["src/Lagedra.Modules/ActivationAndBilling"]="BillingDbContext"
  ["src/Lagedra.Modules/IdentityAndVerification"]="IdentityVerificationDbContext"
  ["src/Lagedra.Modules/ListingAndLocation"]="ListingDbContext"
  ["src/Lagedra.Modules/StructuredInquiry"]="InquiryDbContext"
  ["src/Lagedra.Modules/VerificationAndRisk"]="VerificationDbContext"
  ["src/Lagedra.Modules/InsuranceIntegration"]="InsuranceDbContext"
  ["src/Lagedra.Modules/ComplianceMonitoring"]="ComplianceDbContext"
  ["src/Lagedra.Modules/Arbitration"]="ArbitrationDbContext"
  ["src/Lagedra.Modules/JurisdictionPacks"]="JurisdictionDbContext"
  ["src/Lagedra.Modules/Evidence"]="EvidenceDbContext"
  ["src/Lagedra.Modules/Privacy"]="PrivacyDbContext"
  ["src/Lagedra.Modules/Notifications"]="NotificationDbContext"
  ["src/Lagedra.Modules/AntiAbuseAndIntegrity"]="IntegrityDbContext"
  ["src/Lagedra.Modules/ContentManagement"]="ContentDbContext"
  ["src/Lagedra.TruthSurface"]="TruthSurfaceDbContext"
  ["src/Lagedra.Compliance"]="ComplianceLedgerDbContext"
)

for proj_rel in "${!CONTEXTS[@]}"; do
  proj="$ROOT/$proj_rel"
  ctx="${CONTEXTS[$proj_rel]}"
  if [ -d "$proj" ]; then
    echo "Migrating $ctx..."
    dotnet ef database update \
      --project "$proj" \
      --startup-project "$STARTUP" \
      --context "$ctx"
  else
    echo "Skipping $ctx — project not yet created."
  fi
done

echo "All migrations complete."
