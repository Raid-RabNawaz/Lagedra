# Writes the Screen & Protect *production* subscription key to
# /lagedra/prod/truvi-subscription-key only. Does not touch staging.
# Copy the key from https://developer.truvi.com (live product, not sandbox).
# Do not commit the key or paste it into chat.
#
# Usage (from repo root):
#   .\deploy\aws\set-prod-truvi-key.ps1 -SubscriptionKey "<production key>"
#
# Optional: point running prod API + worker at the live host and restart:
#   .\deploy\aws\set-prod-truvi-key.ps1 -SubscriptionKey "<production key>" -Apply
#
# Official live paths:
#   POST https://developer.api.truvi.com/screen-and-protect/verificationRequests
#   PUT  https://developer.api.truvi.com/screen-and-protect/verificationRequests/cancel

param(
    [Parameter(Mandatory = $true)]
    [string] $SubscriptionKey,

    [switch] $Apply,

    [string] $Region = "us-west-1",
    [string] $Prefix = "/lagedra/prod",
    [string] $Cluster = "lagedra-prod",
    [string[]] $Services = @("lagedra-api", "lagedra-worker"),
    [string] $BaseUrl = "https://developer.api.truvi.com/screen-and-protect"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SubscriptionKey)) {
    throw "SubscriptionKey is required (production key from https://developer.truvi.com)."
}

if ($BaseUrl -like "*-sandbox*") {
    throw "Refusing a sandbox BaseUrl on production."
}

$name = "$Prefix/truvi-subscription-key"
aws ssm put-parameter `
    --name $name `
    --value $SubscriptionKey `
    --type SecureString `
    --overwrite `
    --region $Region | Out-Null
Write-Host "Wrote $name"

if (-not $Apply) {
    Write-Host ""
    Write-Host "SSM is updated. Running prod tasks still use the previous key and sandbox URL"
    Write-Host "until you apply. Re-run with -Apply to set Insurance__Truvi__BaseUrl=$BaseUrl"
    Write-Host "and force new deployments of lagedra-api and lagedra-worker."
    exit 0
}

function Update-TruviTaskDefinition([string] $ServiceName) {
    $taskArn = aws ecs describe-services `
        --cluster $Cluster `
        --services $ServiceName `
        --region $Region `
        --query "services[0].taskDefinition" `
        --output text

    if (-not $taskArn -or $taskArn -eq "None") {
        throw "Could not find ECS service $ServiceName on cluster $Cluster. SSM key is stored; apply the task definition manually."
    }

    $raw = aws ecs describe-task-definition `
        --task-definition $taskArn `
        --region $Region `
        --query taskDefinition `
        --output json | ConvertFrom-Json

    $container = $raw.containerDefinitions | Select-Object -First 1
    if (-not $container) {
        throw "Task definition $taskArn has no container."
    }

    $envReplace = @("Insurance__Truvi__BaseUrl", "Insurance__Truvi__ScreeningEnabled")
    $container.environment = @(
        @($container.environment | Where-Object { $envReplace -notcontains $_.name }) +
        [pscustomobject]@{ name = "Insurance__Truvi__BaseUrl"; value = $BaseUrl } +
        [pscustomobject]@{ name = "Insurance__Truvi__ScreeningEnabled"; value = "true" }
    )

    $accountId = aws sts get-caller-identity --query Account --output text
    $secretArn = "arn:aws:ssm:${Region}:${accountId}:parameter${Prefix}/truvi-subscription-key"
    $kept = @($container.secrets | Where-Object { $_.name -ne "Insurance__Truvi__SubscriptionKey" })
    $kept += [pscustomobject]@{ name = "Insurance__Truvi__SubscriptionKey"; valueFrom = $secretArn }
    $container.secrets = $kept

    $register = [ordered]@{
        family                  = $raw.family
        networkMode             = $raw.networkMode
        requiresCompatibilities = $raw.requiresCompatibilities
        cpu                     = $raw.cpu
        memory                  = $raw.memory
        executionRoleArn        = $raw.executionRoleArn
        taskRoleArn             = $raw.taskRoleArn
        containerDefinitions    = $raw.containerDefinitions
    }
    if ($raw.volumes) { $register.volumes = $raw.volumes }
    if ($raw.runtimePlatform) { $register.runtimePlatform = $raw.runtimePlatform }

    $tempFile = Join-Path $env:TEMP "lagedra-$ServiceName-truvi.json"
    $register | ConvertTo-Json -Depth 20 | Set-Content -Path $tempFile -Encoding utf8

    aws ecs register-task-definition `
        --cli-input-json "file://$tempFile" `
        --region $Region | Out-Null

    aws ecs update-service `
        --cluster $Cluster `
        --service $ServiceName `
        --task-definition $raw.family `
        --force-new-deployment `
        --region $Region | Out-Null

    Remove-Item $tempFile -ErrorAction SilentlyContinue
    Write-Host "Registered a new $($raw.family) revision and forced a deployment of $ServiceName."
}

foreach ($serviceName in $Services) {
    Update-TruviTaskDefinition $serviceName
}

Write-Host "Prod create: POST $BaseUrl/verificationRequests"
Write-Host "Prod cancel: PUT  $BaseUrl/verificationRequests/cancel"
