#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that records how a listing
    entered Lagedra (manual, URL, Excel, XML, or channel).

    One DbContext changes:

      - ListingsDbContext (listings schema):
          listings.AddedVia        varchar(50)  NOT NULL default 'Manual'
          listings.AddedViaDetail  varchar(200) NULL

    Existing rows default to Manual. Listing analytics also joins
    channel_listing_maps so already-synced Hostaway/OwnerRez/etc. listings
    still show the channel name without a data backfill.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddListingAddedVia).

.EXAMPLE
    pwsh tools/scripts/db-migrate-listing-added-via.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-listing-added-via.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddListingAddedVia"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/ListingAndLocation"; Context = "ListingsDbContext" }
)

Write-Host "Add listings.AddedVia — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

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

Write-Host "Add listings.AddedVia migration complete." -ForegroundColor Green
