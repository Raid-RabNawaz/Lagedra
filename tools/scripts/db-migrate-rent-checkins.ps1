#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for monthly rent check-ins.

    One DbContext changes — a single new table, so existing data is
    unaffected:

      - BillingDbContext (activation_billing schema):
          creates rent_check_ins:
            Id              (uuid, PK)
            DealId          (uuid)
            LandlordUserId  (uuid)
            PeriodStart     (date)
            PeriodEnd       (date)
            Status          (varchar(20): Pending | Received | Missed)
            RespondedAt     (timestamptz, null)
            Note            (varchar(500), null)
            CreatedAt / UpdatedAt (timestamptz)
          unique index on (DealId, PeriodStart); index on Status.

    Why: months 2+ rent is paid to the host directly (non-custodial model),
    so the platform asks the host monthly whether rent arrived. A "missed"
    answer raises a PaymentDefault compliance signal.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddRentCheckIns).

.EXAMPLE
    pwsh tools/scripts/db-migrate-rent-checkins.ps1

.EXAMPLE
    # Re-apply only (migration already committed)
    pwsh tools/scripts/db-migrate-rent-checkins.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddRentCheckIns"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/ActivationAndBilling"; Context = "BillingDbContext" }
)

Write-Host "Rent check-ins — EF Core migrations" -ForegroundColor Cyan
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

Write-Host "Rent check-ins migration complete." -ForegroundColor Green
