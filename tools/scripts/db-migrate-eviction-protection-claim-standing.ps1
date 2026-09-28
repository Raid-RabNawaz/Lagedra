#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for Eviction Protection early move-out standing.

.DESCRIPTION
    BillingDbContext (activation_billing schema):
      - deal_payment_confirmations.EvictionProtectionClaimStanding
        (nullable string: EndedNoClaim or ClaimReviewable)

    Set only when a covered stay ends before the scheduled check-out.
    EndedNoClaim means coverage ended with the booking.
    ClaimReviewable means the early move-out was recorded as documented
    nonpayment and a claim may still be reviewed.

    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddEvictionProtectionClaimStanding).

.EXAMPLE
    pwsh tools/scripts/db-migrate-eviction-protection-claim-standing.ps1 -SkipUpdate
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddEvictionProtectionClaimStanding"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ActivationAndBilling"

Write-Host "Eviction Protection claim standing — EF Core migration (BillingDbContext)" -ForegroundColor Cyan
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

Write-Host "Eviction Protection claim standing migration complete." -ForegroundColor Green
