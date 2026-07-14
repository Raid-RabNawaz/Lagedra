#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply EF Core migrations for deposit-return deduction evidence
    and the statutory return-window platform setting.

    Two DbContexts change:

      - BillingDbContext (billing schema):
          adds to deal_payment_confirmations:
            DepositReturnEvidenceManifestId  (uuid, null)

      - PlatformSettingsDbContext (platform schema):
          seeds deposit_return.window_days = 21
          (CA Civil Code §1950.5 return / itemization window)

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.PARAMETER Name
    Migration name (default: AddDepositReturnEvidenceManifest).

.EXAMPLE
    pwsh tools/scripts/db-migrate-deposit-return-evidence.ps1

.EXAMPLE
    # Re-apply only (migration already committed)
    pwsh tools/scripts/db-migrate-deposit-return-evidence.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddDepositReturnEvidenceManifest"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/ActivationAndBilling"; Context = "BillingDbContext" }
    @{ Project = "src/Lagedra.Infrastructure";               Context = "PlatformSettingsDbContext" }
)

Write-Host "Deposit-return evidence + window setting — EF Core migrations" -ForegroundColor Cyan
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

Write-Host "Deposit-return evidence migration complete." -ForegroundColor Green
