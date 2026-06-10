#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migrations for the tenant platform service fee.

    Two DbContexts change:
      - BillingDbContext (activation_billing schema):
          adds deal_payment_confirmations.ServiceFeeCents (bigint, default 0).
      - PlatformSettingsDbContext (platform schema):
          seeds the new "service_fee.tenant_bps" platform setting (default "0").

    The tenant service fee is charged at checkout as a percentage (basis
    points) of the first month's rent and kept by the platform. A rate of 0
    bps disables it, so applying this migration is behaviour-neutral until an
    admin sets a non-zero rate via Admin → Fees & Settings.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.PARAMETER Name
    Migration name (default: AddTenantServiceFee).

.PARAMETER SettingsOnly
    Only touch PlatformSettingsDbContext (use for changes that affect platform
    settings seed rows but not the billing schema, e.g. adding the flat-fee mode).

.EXAMPLE
    pwsh tools/scripts/db-migrate-tenant-service-fee.ps1

.EXAMPLE
    # Follow-up: add the flat-vs-percentage mode seed rows (settings only)
    pwsh tools/scripts/db-migrate-tenant-service-fee.ps1 -Name AddTenantServiceFeeMode -SettingsOnly

.EXAMPLE
    # Re-apply only (e.g. on another environment), migrations already committed
    pwsh tools/scripts/db-migrate-tenant-service-fee.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [switch]$SettingsOnly,
    [string]$Name = "AddTenantServiceFee"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/ActivationAndBilling"; Context = "BillingDbContext" }
    @{ Project = "src/Lagedra.Infrastructure";               Context = "PlatformSettingsDbContext" }
)

if ($SettingsOnly) {
    $contexts = $contexts | Where-Object { $_.Context -eq "PlatformSettingsDbContext" }
}

Write-Host "Tenant platform service fee — EF Core migrations" -ForegroundColor Cyan
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

Write-Host "Tenant service fee migrations complete." -ForegroundColor Green
