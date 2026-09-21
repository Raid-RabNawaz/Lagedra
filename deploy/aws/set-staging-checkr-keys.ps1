# Writes Checkr Tenant test keys to /lagedra/staging/* only.
# Does not touch /lagedra/prod/* . Refuses live keys (ckr_sk_live_).
#
# Usage (from repo root):
#   .\deploy\aws\set-staging-checkr-keys.ps1 `
#     -SecretKey "ckr_sk_test_..." `
#     -WebhookSecret "<Test webhook signing secret>"
#
# Optional: retarget the running staging API, set Screening__Provider=Checkr,
# and force a new deployment:
#   .\deploy\aws\set-staging-checkr-keys.ps1 ... -Apply
#
# Webhook URL this secret must belong to:
#   https://api-staging.lagedra.com/v1/webhooks/screening/checkr
#   Mode: Test

param(
    [Parameter(Mandatory = $true)]
    [string] $SecretKey,

    [Parameter(Mandatory = $true)]
    [string] $WebhookSecret,

    [switch] $Apply,

    [string] $Region = "us-west-1",
    [string] $Prefix = "/lagedra/staging",
    [string] $Cluster = "lagedra-prod",
    [string] $Service = "lagedra-api-staging"
)

$ErrorActionPreference = "Stop"

if (-not $SecretKey.StartsWith("ckr_sk_test_")) {
    throw "Secret key must start with ckr_sk_test_ (refusing live / mismatched keys on staging)."
}

if ([string]::IsNullOrWhiteSpace($WebhookSecret)) {
    throw "Webhook secret is required (from the Checkr Test webhook)."
}

if ($WebhookSecret.StartsWith("ckr_sk_")) {
    throw "Webhook secret looks like an API key. Use the signing secret from the Test webhook."
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
    Write-Host "SSM is updated. Staging API still uses NoOp / old keys until you apply."
    Write-Host "Re-run with -Apply to inject Checkr__* secrets, set Screening__Provider=Checkr,"
    Write-Host "and force a new deployment of lagedra-api-staging."
    exit 0
}

$accountId = aws sts get-caller-identity --query Account --output text
$arnPrefix = "arn:aws:ssm:${Region}:${accountId}:parameter${Prefix}"

$taskArn = aws ecs describe-services `
    --cluster $Cluster `
    --services $Service `
    --region $Region `
    --query "services[0].taskDefinition" `
    --output text

if (-not $taskArn -or $taskArn -eq "None") {
    throw "Could not find ECS service $Service on cluster $Cluster. SSM keys are stored; apply the task definition manually."
}

$raw = aws ecs describe-task-definition `
    --task-definition $taskArn `
    --region $Region `
    --query taskDefinition `
    --output json | ConvertFrom-Json

$container = $raw.containerDefinitions | Where-Object { $_.name -eq "api" } | Select-Object -First 1
if (-not $container) {
    throw "Task definition $taskArn has no container named api."
}

$envReplace = @("Screening__Provider", "Checkr__PackageSlug")
$container.environment = @(
    @($container.environment | Where-Object { $envReplace -notcontains $_.name }) +
    [pscustomobject]@{ name = "Screening__Provider"; value = "Checkr" } +
    [pscustomobject]@{ name = "Checkr__PackageSlug"; value = "essential" }
)

$secretMap = @{
    "Checkr__SecretKey"     = "$arnPrefix/checkr-secret-key"
    "Checkr__WebhookSecret" = "$arnPrefix/checkr-webhook-secret"
}

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

$tempFile = Join-Path $env:TEMP "lagedra-api-staging-checkr.json"
$register | ConvertTo-Json -Depth 20 | Set-Content -Path $tempFile -Encoding utf8

aws ecs register-task-definition `
    --cli-input-json "file://$tempFile" `
    --region $Region | Out-Null

aws ecs update-service `
    --cluster $Cluster `
    --service $Service `
    --task-definition $raw.family `
    --force-new-deployment `
    --region $Region | Out-Null

Remove-Item $tempFile -ErrorAction SilentlyContinue
Write-Host "Registered a new $($raw.family) revision and forced a deployment of $Service."
Write-Host "Confirm the Test webhook URL is https://api-staging.lagedra.com/v1/webhooks/screening/checkr"
