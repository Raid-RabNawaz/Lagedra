#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for the deposit-return handshake
    (non-custodial, host-held deposits).

    One DbContext changes. Every new column is nullable with no default, so
    existing rows are unaffected (behaviour-neutral):

      - BillingDbContext (billing schema):
          adds to deal_payment_confirmations:
            MoveOutInitiatedAt              (timestamptz, null)
            MoveOutInitiatedByUserId        (uuid, null)
            HostConfirmedDepositReturnedAt  (timestamptz, null)
            TenantConfirmedDepositReceivedAt(timestamptz, null)
            DepositReturnAmountCents        (bigint, null)
            DepositReturnMethod             (varchar(50), null)
            DepositReturnNote               (varchar(2000), null)
            DepositReturnSettledAt          (timestamptz, null)
            DepositReturnReminderSentAt     (timestamptz, null)

    A new DealPhase value ('AwaitingDepositReturn') is computed at read time,
    not persisted, so no data back-fill is required for existing rows. See
    tools/scripts/backfill-deposit-return-settled.sql for optionally marking
    already-refunded closed deals as settled.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddDepositReturnHandshake).

.EXAMPLE
    pwsh tools/scripts/db-migrate-deposit-return.ps1

.EXAMPLE
    # Re-apply only (migration already committed)
    pwsh tools/scripts/db-migrate-deposit-return.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddDepositReturnHandshake"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/ActivationAndBilling"; Context = "BillingDbContext" }
)

Write-Host "Deposit-return handshake — EF Core migrations" -ForegroundColor Cyan
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

Write-Host "Deposit-return handshake migration complete." -ForegroundColor Green
