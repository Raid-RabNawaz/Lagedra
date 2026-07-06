#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deposit-Return Handshake — one-off historical backfill.

    Marks the deposit-return handshake as settled on bookings the pre-handshake
    automatic refund job (DepositReturnJob) already refunded, so they keep
    rendering as "Closed" instead of newly surfacing as "AwaitingDepositReturn".

    Runs backfill-deposit-return-settled.sql. DRY RUN by default (SELECTs only,
    prints what would change). Pass -Apply to write. The write runs in a
    transaction and only touches Closed bookings with a deposit and no existing
    DepositReturnSettledAt (idempotent).

.PARAMETER ConnString
    libpq connection URI. Defaults to $env:DATABASE_URL, then to the local dev
    database. Override for staging / production.

.PARAMETER Cutoff
    Only backfill bookings CLOSED BEFORE this instant (set to your deploy time
    so only pre-handshake bookings are touched). Defaults to now.

.PARAMETER Apply
    Actually write the changes. Without it the script is a read-only preview.

.EXAMPLE
    # Preview only
    pwsh tools/scripts/backfill-deposit-return-settled.ps1 -Cutoff '2026-07-03 00:00:00+00'

.EXAMPLE
    # Apply
    pwsh tools/scripts/backfill-deposit-return-settled.ps1 -Cutoff '2026-07-03 00:00:00+00' -Apply
#>

[CmdletBinding()]
param(
    [string]$ConnString = $env:DATABASE_URL,
    [string]$Cutoff,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ConnString)) {
    $ConnString = "postgresql://lagedra:lagedra_pw@localhost:5432/lagedra_db"
    Write-Host "No -ConnString / DATABASE_URL provided; using local dev default." -ForegroundColor Yellow
}

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql not found on PATH. Install the PostgreSQL client or run the .sql file with your own tool."
}

$sql = Join-Path $PSScriptRoot "backfill-deposit-return-settled.sql"
if (-not (Test-Path $sql)) { throw "Cannot find $sql" }

$args = @($ConnString, "-v", "ON_ERROR_STOP=1")
if (-not [string]::IsNullOrWhiteSpace($Cutoff)) { $args += @("-v", "cutoff=$Cutoff") }
if ($Apply) { $args += @("-v", "apply=1") }
$args += @("-f", $sql)

if ($Apply) {
    Write-Host "Deposit-return backfill — APPLYING changes" -ForegroundColor Cyan
} else {
    Write-Host "Deposit-return backfill — DRY RUN (read-only preview)" -ForegroundColor Cyan
}
Write-Host ""

psql @args
if ($LASTEXITCODE -ne 0) { throw "psql exited with code $LASTEXITCODE." }
