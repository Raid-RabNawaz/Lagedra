# Writes Checkr Tenant live keys to /lagedra/prod/* only.
# Does not touch /lagedra/staging/* . Refuses test keys (ckr_sk_test_).
#
# Usage (from repo root):
#   .\deploy\aws\set-prod-checkr-keys.ps1 `
#     -SecretKey "ckr_sk_live_..." `
#     -WebhookSecret "<Live webhook signing secret>"
#
# Optional: retarget the running prod API + worker, set Screening__Provider=Checkr,
# and force new deployments:
#   .\deploy\aws\set-prod-checkr-keys.ps1 ... -Apply
#
# Webhook URL this secret must belong to:
#   https://api.lagedra.com/v1/webhooks/screening/checkr
#   Mode: Live

param(
    [Parameter(Mandatory = $true)]
    [string] $SecretKey,

    [Parameter(Mandatory = $true)]
    [string] $WebhookSecret,

    [switch] $Apply,

    [string] $Region = "us-west-1",
    [string] $Prefix = "/lagedra/prod",
    [string] $Cluster = "lagedra-prod",
    [string[]] $Services = @("lagedra-api", "lagedra-worker")
)

$ErrorActionPreference = "Stop"

if (-not $SecretKey.StartsWith("ckr_sk_live_")) {
    throw "Secret key must start with ckr_sk_live_ (refusing test / mismatched keys on live)."
}

if ([string]::IsNullOrWhiteSpace($WebhookSecret)) {
    throw "Webhook secret is required (from the Checkr Live webhook)."
}

if ($WebhookSecret.StartsWith("ckr_sk_")) {
    throw "Webhook secret looks like an API key. Use the signing secret from the Live webhook."
}

if ($WebhookSecret.StartsWith("ckr_sk_test_") -or $SecretKey.StartsWith("ckr_sk_test_")) {
    throw "Refusing Checkr test credentials on production."
}

$parameters = @(
    @{ Name = "checkr-secret-key"; Value = $SecretKey },
    @{ Name = "checkr-webhook-secret"; Value = $WebhookSecret }
)

foreach ($parameter in $parameters) {
    $name = "$Prefix/$($parameter.Name)"
    aws ssm put-parameter `
        --name $name `
        --value $parameter.Value `
        --type SecureString `
        --overwrite `
        --region $Region | Out-Null
    Write-Host "Wrote $name"
}

if (-not $Apply) {
    Write-Host ""
    Write-Host "SSM is updated. Prod API/worker still use NoOp / old keys until you apply."
    Write-Host "Re-run with -Apply to inject Checkr__* secrets, set Screening__Provider=Checkr,"
    Write-Host "and force new deployments of lagedra-api and lagedra-worker."
    exit 0
}

$accountId = aws sts get-caller-identity --query Account --output text
$arnPrefix = "arn:aws:ssm:${Region}:${accountId}:parameter${Prefix}"
$secretMap = @{
    "Checkr__SecretKey"     = "$arnPrefix/checkr-secret-key"
    "Checkr__WebhookSecret" = "$arnPrefix/checkr-webhook-secret"
}

function Update-CheckrTaskDefinition([string] $ServiceName) {
    $taskArn = aws ecs describe-services `
        --cluster $Cluster `
        --services $ServiceName `
        --region $Region `
        --query "services[0].taskDefinition" `
        --output text

    if (-not $taskArn -or $taskArn -eq "None") {
        throw "Could not find ECS service $ServiceName on cluster $Cluster. SSM keys are stored; apply the task definition manually."
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

    $envReplace = @("Screening__Provider", "Checkr__PackageSlug")
    $container.environment = @(
        @($container.environment | Where-Object { $envReplace -notcontains $_.name }) +
        [pscustomobject]@{ name = "Screening__Provider"; value = "Checkr" } +
        [pscustomobject]@{ name = "Checkr__PackageSlug"; value = "essential" }
    )

    $kept = @($container.secrets | Where-Object { -not $secretMap.ContainsKey($_.name) })
    foreach ($entry in $secretMap.GetEnumerator()) {
        $kept += [pscustomobject]@{ name = $entry.Key; valueFrom = $entry.Value }
    }
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

    $tempFile = Join-Path $env:TEMP "lagedra-$ServiceName-checkr.json"
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
    Update-CheckrTaskDefinition $serviceName
}

Write-Host "Confirm the Live webhook URL is https://api.lagedra.com/v1/webhooks/screening/checkr"
