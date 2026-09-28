#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that stores each imported
    listing's PMS price reading (monthly or nightly) and the raw amount.

    One DbContext change:
      - ChannelDbContext (channel_integration schema):
          channel_listing_maps gains nullable PmsPriceBasis (string)
          and nullable PmsSourceAmountCents (bigint).

    Existing rows stay unanswered (both columns null) until the next sync
    stores the amount and the host chooses a period.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddChannelListingPriceBasis).

.EXAMPLE
    pwsh tools/scripts/db-migrate-channel-listing-price-basis.ps1
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddChannelListingPriceBasis"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ChannelIntegration"
$context = "ChannelDbContext"

Write-Host "Channel listing price basis — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project  : $startup" -ForegroundColor DarkGray
Write-Host "Migration project: $project" -ForegroundColor DarkGray
Write-Host ""

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

Write-Host "Channel listing price basis migration complete." -ForegroundColor Green
