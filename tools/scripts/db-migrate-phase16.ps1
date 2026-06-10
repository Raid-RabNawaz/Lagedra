#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Apply the three Phase 16 (Booking Flow Optimization) EF Core migrations.

    Migrations applied (idempotent — already-applied ones are skipped by EF):
      - ListingsDbContext  : 20260512202411_AddDefaultDepositCentsToListings
      - AuthDbContext      : 20260512202426_AddStripeCustomerIdToUsers
      - BillingDbContext   : 20260512202439_AddStripePaymentMethodIdToApplications

    Run AFTER `dotnet build` so the EF tooling reads the latest model snapshot.

.PARAMETER WhatIf
    Print the dotnet ef commands without executing them.

.EXAMPLE
    pwsh tools/scripts/db-migrate-phase16.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-phase16.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$migrations = @(
    @{
        Project   = "src/Lagedra.Modules/ListingAndLocation"
        Context   = "ListingsDbContext"
        Migration = "20260512202411_AddDefaultDepositCentsToListings"
    },
    @{
        Project   = "src/Lagedra.Auth"
        Context   = "AuthDbContext"
        Migration = "20260512202426_AddStripeCustomerIdToUsers"
    },
    @{
        Project   = "src/Lagedra.Modules/ActivationAndBilling"
        Context   = "BillingDbContext"
        Migration = "20260512202439_AddStripePaymentMethodIdToApplications"
    }
)

Write-Host "Phase 16 — Booking Flow Optimization migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

foreach ($m in $migrations) {
    $proj = Join-Path $root $m.Project
    if (-not (Test-Path $proj)) {
        Write-Host "Skipping $($m.Context) — project not found at $proj" -ForegroundColor Yellow
        continue
    }

    Write-Host "→ $($m.Context) :: $($m.Migration)" -ForegroundColor Cyan

    if ($PSCmdlet.ShouldProcess($m.Context, "dotnet ef database update $($m.Migration)")) {
        dotnet ef database update $m.Migration `
            --project        $proj `
            --startup-project $startup `
            --context        $m.Context

        if ($LASTEXITCODE -ne 0) {
            throw "Migration failed for $($m.Context) ($($m.Migration)). Aborting."
        }
    }

    Write-Host ""
}

Write-Host "Phase 16 migrations complete." -ForegroundColor Green
