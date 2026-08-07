#!/usr/bin/env bash
# Generate and apply the EF Core migration that adds OAuth token storage
# (encrypted refresh token + expiry) to channel_connections.
#
# Hosts link OwnerRez by authorizing Lagedra's OAuth app rather than pasting a
# personal access token, and OwnerRez access tokens expire after thirty days, so
# OwnerRezTokenRefreshJob needs somewhere to keep the refresh token and expiry.
# Additive and nullable, so applying it is behaviour-neutral.
#
# Usage:
#   ./tools/scripts/db-migrate-channel-oauth-tokens.sh
#   ./tools/scripts/db-migrate-channel-oauth-tokens.sh --skip-add
#   ./tools/scripts/db-migrate-channel-oauth-tokens.sh --skip-update

set -euo pipefail

SKIP_ADD=0
SKIP_UPDATE=0
NAME="AddChannelOAuthTokens"

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
PROJECT="$ROOT/src/Lagedra.Modules/ChannelIntegration"
CONTEXT="ChannelDbContext"

echo "ChannelIntegration OAuth tokens — EF Core migration"
echo "Startup project : $STARTUP"
echo ""

echo "Building solution..."
dotnet build "$ROOT/Lagedra.sln" --nologo --verbosity quiet
echo ""

if [[ $SKIP_ADD -eq 0 ]]; then
  echo "→ add $NAME :: $CONTEXT"
  dotnet ef migrations add "$NAME" \
    --project "$PROJECT" \
    --startup-project "$STARTUP" \
    --context "$CONTEXT"
  echo ""
fi

if [[ $SKIP_UPDATE -eq 0 ]]; then
  echo "→ database update :: $CONTEXT"
  dotnet ef database update \
    --project "$PROJECT" \
    --startup-project "$STARTUP" \
    --context "$CONTEXT"
  echo ""
fi

echo "ChannelIntegration OAuth token migration complete."
