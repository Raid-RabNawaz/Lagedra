#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that seeds
    stripe.platform_fee_price_id on PlatformSettingsDbContext.

    Admin → Fees & Settings shows this under Stripe Connect only after the
    row exists. Value defaults to "" until an admin sets a live/test Price ID
    (price_…).

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddStripePlatformFeePriceIdSetting).

.EXAMPLE
    # Generate only (commit the migration, apply per environment later)
    pwsh tools/scripts/db-migrate-stripe-platform-fee-price.ps1 -SkipUpdate

.EXAMPLE
    # Apply already-generated migration (set ConnectionStrings__DefaultConnection)
    pwsh tools/scripts/db-migrate-stripe-platform-fee-price.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddStripePlatformFeePriceIdSetting"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Infrastructure"
$context = "PlatformSettingsDbContext"

Write-Host "Stripe platform fee price ID setting — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
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
        --context         $context `
        --output-dir      Migrations
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: $context" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $project `
        --startup-project $startup `
        --context         $context
    if ($LASTEXITCODE -ne 0) { throw "database update failed. Aborting." }
    Write-Host ""
}

Write-Host "Stripe platform fee price ID migration complete." -ForegroundColor Green
