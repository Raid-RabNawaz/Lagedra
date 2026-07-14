#!/usr/bin/env bash
# Generate and apply the EF Core migration for inquiry partner participants.
set -euo pipefail

SKIP_ADD=0
SKIP_UPDATE=0
NAME="AddInquiryPartnerParticipant"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-add) SKIP_ADD=1; shift ;;
    --skip-update) SKIP_UPDATE=1; shift ;;
    --name) NAME="$2"; shift 2 ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"
PROJECT="$ROOT/src/Lagedra.Modules/StructuredInquiry"
CONTEXT="InquiryDbContext"

echo "Inquiry Partner Participant — EF Core migration"
dotnet build "$ROOT/Lagedra.sln" --nologo --verbosity quiet

if [[ $SKIP_ADD -eq 0 ]]; then
  echo "→ add $NAME :: $CONTEXT"
  dotnet ef migrations add "$NAME" \
    --project "$PROJECT" \
    --startup-project "$STARTUP" \
    --context "$CONTEXT"
fi

if [[ $SKIP_UPDATE -eq 0 ]]; then
  echo "→ database update :: $CONTEXT"
  dotnet ef database update \
    --project "$PROJECT" \
    --startup-project "$STARTUP" \
    --context "$CONTEXT"
fi

echo "Inquiry partner participant migration complete."
