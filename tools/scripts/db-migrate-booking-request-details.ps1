#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that adds Airbnb-style
    booking request fields to DealApplication.

    One DbContext changes:
      - BillingDbContext (activation_billing schema):
          * deal_applications.GuestCount  (int,         default 1, required)
          * deal_applications.Message     (nvarchar/varchar(1000), nullable)

    Existing rows backfill GuestCount = 1 via the column default; Message
    stays NULL for pre-migration applications, matching the "no cover
    note provided" semantic the API will return.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddBookingRequestGuestCountAndMessage).

.EXAMPLE
    pwsh tools/scripts/db-migrate-booking-request-details.ps1

.EXAMPLE
    # Re-apply on another environment, migration already committed
    pwsh tools/scripts/db-migrate-booking-request-details.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddBookingRequestGuestCountAndMessage"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ActivationAndBilling"
$context = "BillingDbContext"

Write-Host "Booking request guest count + message — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host "Project         : $project" -ForegroundColor DarkGray
Write-Host "DbContext       : $context" -ForegroundColor DarkGray
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

Write-Host "Booking request migration complete." -ForegroundColor Green
