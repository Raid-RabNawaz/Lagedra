#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for Option A (Non-Custodial
    Payments) — Workstream 0.

    One DbContext changes. Every new column has a safe default, so existing rows
    are unaffected (behaviour-neutral back-fill):

      - IdentityDbContext (identity_verification schema):
          adds host_stripe_accounts.TaxStatus and BankAccountStatus
          (varchar(30), default 'Unknown'; populated on the next Stripe
          account status sync via account.updated webhook or status poll).

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).
    Use on environments where the migration is already committed.

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddHostPayoutRequirementStatus).

.EXAMPLE
    pwsh tools/scripts/db-migrate-noncustodial-payments.ps1

.EXAMPLE
    # Re-apply only (migration already committed)
    pwsh tools/scripts/db-migrate-noncustodial-payments.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddHostPayoutRequirementStatus"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/IdentityAndVerification"; Context = "IdentityDbContext" }
)

Write-Host "Option A (Non-Custodial Payments) — EF Core migrations" -ForegroundColor Cyan
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

Write-Host "Non-custodial payments migration complete." -ForegroundColor Green
