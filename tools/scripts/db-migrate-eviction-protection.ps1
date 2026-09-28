#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for Lagedra Partner Eviction Protection.

.DESCRIPTION
    BillingDbContext (activation_billing schema):
      - deal_applications.EvictionProtectionIncluded (bool, default false)
      - deal_applications.EvictionProtectionFeeCents (bigint, default 0)
      - deal_payment_confirmations.EvictionProtectionFeeCents (bigint, default 0)

    The guest fee itself is the platform setting eviction_protection.fee_cents.
    That row is created the first time an admin opens Platform Fees & Settings,
    defaulting to 5000 ($50). It is not a schema change.

    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddEvictionProtectionFee).

.EXAMPLE
    pwsh tools/scripts/db-migrate-eviction-protection.ps1 -SkipUpdate
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddEvictionProtectionFee"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ActivationAndBilling"

Write-Host "Eviction Protection — EF Core migration (BillingDbContext)" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "→ add $Name :: BillingDbContext" -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project         $project `
        --startup-project $startup `
        --context         BillingDbContext
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: BillingDbContext" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $project `
        --startup-project $startup `
        --context         BillingDbContext
    if ($LASTEXITCODE -ne 0) { throw "database update failed." }
    Write-Host ""
}

Write-Host "Eviction Protection migration complete." -ForegroundColor Green
