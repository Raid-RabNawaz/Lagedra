#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Option A (Non-Custodial Payments) — Workstream 7 backfill / reconciliation
    report. READ-ONLY: runs noncustodial-backfill-report.sql (SELECTs only) and
    prints the two operational lists for the custodial -> non-custodial cutover:

      1. Hosts that are not Stripe-Connect-ready (must finish onboarding).
      2. Succeeded booking payments whose host is not Connect-ready (rent +
         deposit likely parked in the platform balance — reconcile in Stripe).

    See the WS7 runbook in PLAN.md for what to do with each list.

.PARAMETER ConnString
    libpq connection URI. Defaults to $env:DATABASE_URL, then to the local dev
    database. Override for staging / production.

.EXAMPLE
    pwsh tools/scripts/noncustodial-backfill-report.ps1

.EXAMPLE
    pwsh tools/scripts/noncustodial-backfill-report.ps1 -ConnString "postgresql://user:pw@host:5432/lagedra_db"
#>

[CmdletBinding()]
param(
    [string]$ConnString = $env:DATABASE_URL
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

$sql = Join-Path $PSScriptRoot "noncustodial-backfill-report.sql"
if (-not (Test-Path $sql)) { throw "Cannot find $sql" }

Write-Host "Non-custodial backfill report (read-only)" -ForegroundColor Cyan
Write-Host ""

psql $ConnString -v ON_ERROR_STOP=1 -f $sql
if ($LASTEXITCODE -ne 0) { throw "psql exited with code $LASTEXITCODE." }
