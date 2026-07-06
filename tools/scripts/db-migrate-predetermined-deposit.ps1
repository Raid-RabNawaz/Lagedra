#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migrations for the Predetermined-Deposit
    Booking refactor.

    Four DbContexts change. Every new column is nullable or has a safe default,
    so existing rows are unaffected (behaviour-neutral back-fill):

      - ListingsDbContext (listing_location schema):
          adds listings.DepositUnverifiedCents, DepositBackgroundVerifiedCents,
          DepositPartnerGuaranteedCents (all bigint NULL; NULL falls back to
          MaxDepositCents at request time).

      - BillingDbContext (activation_billing schema):
          adds deal_applications snapshot columns (SelectedDepositAmountCents,
          TenantVerificationTierAtRequest, RentAmountSnapshotCents,
          InsuranceFeeSnapshotCents, ServiceFeeSnapshotCents,
          TotalPayableSnapshotCents) + tenant/host Truth Surface consent
          columns; extends DealApplicationStatus / PaymentConfirmationStatus
          (stored as int/string, no schema change for the enum values).

      - TruthSurfaceDbContext (truth_surface schema):
          adds snapshot consent metadata (tenant/host user id, at, ip,
          user agent, version) + IsLocked / LockedAt lock columns.

      - AuditDbContext (audit_log schema):
          initial migration (the context was added for the decoupled
          IAuditTrailWriter and had no migrations yet).

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).
    Use on environments where the migrations are already committed.

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.PARAMETER Name
    Migration name (default: AddPredeterminedDepositBooking).

.EXAMPLE
    pwsh tools/scripts/db-migrate-predetermined-deposit.ps1

.EXAMPLE
    # Re-apply only (e.g. on another environment), migrations already committed
    pwsh tools/scripts/db-migrate-predetermined-deposit.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddPredeterminedDepositBooking"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

# Order matters only for readability — EF applies each context independently.
$contexts = @(
    @{ Project = "src/Lagedra.Modules/ListingAndLocation";  Context = "ListingsDbContext" }
    @{ Project = "src/Lagedra.Modules/ActivationAndBilling"; Context = "BillingDbContext" }
    @{ Project = "src/Lagedra.TruthSurface";                 Context = "TruthSurfaceDbContext" }
    @{ Project = "src/Lagedra.Modules/AuditLog";             Context = "AuditDbContext" }
)

Write-Host "Predetermined-Deposit Booking — EF Core migrations" -ForegroundColor Cyan
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

Write-Host "Predetermined-deposit booking migrations complete." -ForegroundColor Green
