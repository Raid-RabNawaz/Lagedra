#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Backfill deal_applications.TenantVerificationTierAtRequest from legacy
    snapshot names (BackgroundVerified / PartnerGuaranteed) to Trusted / Elite.
    No EF schema change — the column is already a string.
#>

param(
    [string] $ConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$apiGateway = Join-Path $root "src/Lagedra.ApiGateway"

if (-not $ConnectionString) {
    $devSettingsPath = Join-Path $apiGateway "appsettings.Development.json"
    if (Test-Path $devSettingsPath) {
        $ConnectionString = (Get-Content $devSettingsPath -Raw | ConvertFrom-Json).ConnectionStrings.Default
    }
}

if (-not $ConnectionString) {
    throw "Pass -ConnectionString or set ConnectionStrings.Default in appsettings.Development.json."
}

Write-Host "Backfilling legacy trust-tier snapshot names..." -ForegroundColor Cyan

# Npgsql-style connection string works with `psql` if available; otherwise print SQL.
$sql = @"
UPDATE activation_billing.deal_applications
SET "TenantVerificationTierAtRequest" = 'Trusted'
WHERE "TenantVerificationTierAtRequest" = 'BackgroundVerified';

UPDATE activation_billing.deal_applications
SET "TenantVerificationTierAtRequest" = 'Elite'
WHERE "TenantVerificationTierAtRequest" = 'PartnerGuaranteed';
"@

$psql = Get-Command psql -ErrorAction SilentlyContinue
if ($psql) {
    $sql | & psql $ConnectionString
    if ($LASTEXITCODE -ne 0) { throw "psql backfill failed." }
    Write-Host "Done." -ForegroundColor Green
}
elseif (Get-Command docker -ErrorAction SilentlyContinue) {
    $sql | docker exec -i lagedra-pg psql -U lagedra -d lagedra_db
    if ($LASTEXITCODE -ne 0) { throw "docker psql backfill failed." }
    Write-Host "Done." -ForegroundColor Green
}
else {
    Write-Host "psql not on PATH. Run this SQL against activation_billing:" -ForegroundColor Yellow
    Write-Host $sql
}
