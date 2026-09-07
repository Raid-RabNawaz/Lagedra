# Writes the sandbox Stripe trio to /lagedra/staging/* only.
# Does not touch /lagedra/prod/* .
#
# Usage (from repo root):
#   .\deploy\aws\set-staging-stripe-keys.ps1 `
#     -PublishableKey "pk_test_..." `
#     -SecretKey "sk_test_..." `
#     -WebhookSecret "whsec_..."
#
# Optional: also retarget the running staging API and restart it
#   .\deploy\aws\set-staging-stripe-keys.ps1 ... -Apply

param(
    [Parameter(Mandatory = $true)]
    [string] $PublishableKey,

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

function Assert-Prefix([string] $Value, [string] $Expected, [string] $Label) {
    if (-not $Value.StartsWith($Expected)) {
        throw "$Label must start with $Expected (got a different key type)."
    }
}

Assert-Prefix $PublishableKey "pk_test_" "Publishable key"
Assert-Prefix $SecretKey "sk_test_" "Secret key"
Assert-Prefix $WebhookSecret "whsec_" "Webhook secret"

$parameters = @(
    @{ Name = "stripe-publishable-key"; Value = $PublishableKey },
    @{ Name = "stripe-secret-key"; Value = $SecretKey },
    @{ Name = "stripe-webhook-secret"; Value = $WebhookSecret }
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
    Write-Host "SSM is updated. Staging API still reads the old /lagedra/prod Stripe paths until you apply."
    Write-Host "Re-run with -Apply to retarget lagedra-api-staging and force a new deployment."
    Write-Host "Also set the same pk_test_ key on the staging web build as VITE_STRIPE_PUBLISHABLE_KEY."
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

$container.environment = @(
    $container.environment | Where-Object { $_.name -ne "Stripe__PublishableKey" }
)

$secretMap = @{
    "Stripe__PublishableKey" = "$arnPrefix/stripe-publishable-key"
    "Stripe__SecretKey"      = "$arnPrefix/stripe-secret-key"
    "Stripe__WebhookSecret"  = "$arnPrefix/stripe-webhook-secret"
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

$tempFile = Join-Path $env:TEMP "lagedra-api-staging-stripe.json"
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
Write-Host "Set the same pk_test_ key on the staging web build as VITE_STRIPE_PUBLISHABLE_KEY."
