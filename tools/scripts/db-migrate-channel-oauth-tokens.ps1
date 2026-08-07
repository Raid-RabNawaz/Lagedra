#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration adding OAuth token storage to
    channel connections.

    One DbContext changes:
      - ChannelDbContext (channel_integration schema):
          channel_connections gains encrypted_refresh_token + token_expires_at.

    Needed because hosts now link OwnerRez by authorizing Lagedra's OAuth app
    instead of pasting a personal access token, and OwnerRez access tokens expire
    after thirty days — so the refresh token and expiry have to be persisted for
    OwnerRezTokenRefreshJob to renew them.

    Additive and nullable, so applying it is behaviour-neutral for existing
    connections.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddChannelOAuthTokens).

.EXAMPLE
    pwsh tools/scripts/db-migrate-channel-oauth-tokens.ps1

.EXAMPLE
    # Re-apply only (e.g. on another environment), migration already committed
    pwsh tools/scripts/db-migrate-channel-oauth-tokens.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddChannelOAuthTokens"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ChannelIntegration"
$context = "ChannelDbContext"

Write-Host "ChannelIntegration OAuth tokens — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project  : $startup" -ForegroundColor DarkGray
Write-Host "Migration project: $project" -ForegroundColor DarkGray
Write-Host ""

# Build once so the EF tooling reads the latest model snapshot.
Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "→ add $Name :: $context" -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project         $project `
        --startup-project $startup `
        --context         $context
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $context. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: $context" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $project `
        --startup-project $startup `
        --context         $context
    if ($LASTEXITCODE -ne 0) { throw "database update failed for $context. Aborting." }
    Write-Host ""
}

Write-Host "ChannelIntegration OAuth token migration complete." -ForegroundColor Green
