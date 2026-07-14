#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply EF Core migrations for partner booking payer fields.

    Two DbContexts change:
      - BillingDbContext (activation_billing schema):
          * deal_applications.PayerType   (string, default Tenant)
          * deal_applications.PayerUserId (uuid, nullable)
      - PartnerDbContext (partner_network schema):
          * partner_organizations.StripeCustomerId (string, nullable)

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.PARAMETER BillingName
    Billing migration name (default: AddApplicationPayerFields).

.PARAMETER PartnerName
    Partner migration name (default: AddPartnerOrganizationStripeCustomerId).

.EXAMPLE
    pwsh tools/scripts/db-migrate-partner-booking-payer.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-partner-booking-payer.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$BillingName = "AddApplicationPayerFields",
    [string]$PartnerName = "AddPartnerOrganizationStripeCustomerId"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{
        Name    = $BillingName
        Project = Join-Path $root "src/Lagedra.Modules/ActivationAndBilling"
        Context = "BillingDbContext"
    },
    @{
        Name    = $PartnerName
        Project = Join-Path $root "src/Lagedra.Modules/PartnerNetwork"
        Context = "PartnerDbContext"
    }
)

Write-Host "Partner booking payer fields — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

foreach ($c in $contexts) {
    if (-not $SkipAdd) {
        Write-Host "→ add $($c.Name) :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef migrations add $c.Name `
            --project         $c.Project `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $($c.Context). Aborting." }
        Write-Host ""
    }

    if (-not $SkipUpdate) {
        Write-Host "→ database update :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef database update `
            --project         $c.Project `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "database update failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

Write-Host "Partner booking payer migrations complete." -ForegroundColor Green
