#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply EF Core migrations for Twilio SMS + phone verification.

    Two DbContexts change:
      - AuthDbContext (auth schema):
          adds phone OTP columns to AspNetUsers —
            PhoneVerificationCodeHash, PhoneVerificationExpiresAt,
            PhoneVerificationSentAt, PhoneVerificationWindowStartedAt,
            PhoneVerificationSendCount.
      - NotificationDbContext (notifications schema):
          renames RecipientEmail → RecipientAddress on notifications,
          renames BrevoMessageId → ProviderMessageId on delivery_logs.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.PARAMETER AuthName
    Auth migration name (default: AddPhoneVerificationFields).

.PARAMETER NotificationsName
    Notifications migration name (default: AddSmsChannelRecipientAddress).

.EXAMPLE
    pwsh tools/scripts/db-migrate-twilio-sms.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-twilio-sms.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$AuthName = "AddPhoneVerificationFields",
    [string]$NotificationsName = "AddSmsChannelRecipientAddress"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$authProject = Join-Path $root "src/Lagedra.Auth"
$notifProject = Join-Path $root "src/Lagedra.Modules/Notifications"

Write-Host "Twilio SMS / phone verification — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "→ add $AuthName :: AuthDbContext" -ForegroundColor Cyan
    dotnet ef migrations add $AuthName `
        --project         $authProject `
        --startup-project $startup `
        --context         AuthDbContext
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed for AuthDbContext. Aborting." }
    Write-Host ""

    Write-Host "→ add $NotificationsName :: NotificationDbContext" -ForegroundColor Cyan
    dotnet ef migrations add $NotificationsName `
        --project         $notifProject `
        --startup-project $startup `
        --context         NotificationDbContext
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed for NotificationDbContext. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: AuthDbContext" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $authProject `
        --startup-project $startup `
        --context         AuthDbContext
    if ($LASTEXITCODE -ne 0) { throw "database update failed for AuthDbContext. Aborting." }
    Write-Host ""

    Write-Host "→ database update :: NotificationDbContext" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $notifProject `
        --startup-project $startup `
        --context         NotificationDbContext
    if ($LASTEXITCODE -ne 0) { throw "database update failed for NotificationDbContext. Aborting." }
    Write-Host ""
}

Write-Host "Twilio SMS migrations complete." -ForegroundColor Green
