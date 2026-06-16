#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for the ChannelIntegration module.

    One DbContext changes:
      - ChannelDbContext (channel_integration schema):
          channel_connections, channel_listing_maps, channel_booking_links,
          channel_sync_cursors (+ the module's own outbox_messages table).

    This module is the provider-agnostic PMS / channel integration layer
    (OwnerRez today; Hostaway / Guesty / … later). Applying the migration is
    behaviour-neutral until a host connects a channel and a provider is wired.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: InitialCreateChannelIntegration).

.EXAMPLE
    pwsh tools/scripts/db-migrate-channel-integration.ps1

.EXAMPLE
    # Re-apply only (e.g. on another environment), migration already committed
    pwsh tools/scripts/db-migrate-channel-integration.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "InitialCreateChannelIntegration"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ChannelIntegration"
$context = "ChannelDbContext"

Write-Host "ChannelIntegration module — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
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

Write-Host "ChannelIntegration migration complete." -ForegroundColor Green
