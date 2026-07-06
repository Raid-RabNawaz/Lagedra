#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for the arbitration pay-to-file flow.

    One DbContext changes. Every new column is nullable with no default, so
    existing rows are unaffected (behaviour-neutral):

      - ArbitrationDbContext (arbitration schema):
          adds arbitration_cases.FilingFeePaymentIntentId (varchar(255), null)
          and  arbitration_cases.FilingFeePaidAt (timestamptz, null).

    Note: a new ArbitrationStatus value ('PendingPayment') is added, but Status is
    persisted as a string, so no data back-fill is required for existing cases.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddArbitrationFilingFeePayment).

.EXAMPLE
    pwsh tools/scripts/db-migrate-arbitration-filing-fee.ps1

.EXAMPLE
    # Re-apply only (migration already committed)
    pwsh tools/scripts/db-migrate-arbitration-filing-fee.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddArbitrationFilingFeePayment"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/Arbitration"; Context = "ArbitrationDbContext" }
)

Write-Host "Arbitration pay-to-file — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

# Build once so the EF tooling reads the latest model snapshot.
Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ add $Name :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef migrations add $Name `
            --project         $proj `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

if (-not $SkipUpdate) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ database update :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef database update `
            --project         $proj `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "database update failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

Write-Host "Arbitration filing-fee migration complete." -ForegroundColor Green
