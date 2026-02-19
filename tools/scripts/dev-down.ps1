#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stop all Lagedra services. Use -RemoveVolumes to also wipe persistent data.
#>
param(
    [switch]$RemoveVolumes
)

$root = Resolve-Path "$PSScriptRoot\..\.."
Push-Location $root

if ($RemoveVolumes) {
    Write-Host "Stopping services and removing volumes..." -ForegroundColor Yellow
    docker compose down -v
} else {
    Write-Host "Stopping services (volumes preserved)..." -ForegroundColor Yellow
    docker compose down
}

Pop-Location
