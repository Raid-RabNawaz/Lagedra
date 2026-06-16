#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that removes the
    legacy `InsuranceRequired` flag from listings.

    Tenant rental insurance is no longer host-opt-in — every booking is
    quoted against a third-party insurance provider at booking time.
    The aggregate property, EF mapping, contracts, DTOs and UI checkbox
    have all been removed. This migration drops the underlying column.

    DbContext changes:
      - ListingsDbContext (listing_and_location schema):
          * listings.InsuranceRequired (bool, required)  ⟶  DROP COLUMN

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: DropListingInsuranceRequired).

.EXAMPLE
    pwsh tools/scripts/db-migrate-drop-insurance-required.ps1

.EXAMPLE
    # Re-apply on another environment; migration already committed.
    pwsh tools/scripts/db-migrate-drop-insurance-required.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "DropListingInsuranceRequired"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ListingAndLocation"
$context = "ListingsDbContext"

Write-Host "Drop listings.InsuranceRequired — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host "Project         : $project" -ForegroundColor DarkGray
Write-Host "DbContext       : $context" -ForegroundColor DarkGray
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

Write-Host "Drop InsuranceRequired migration complete." -ForegroundColor Green
