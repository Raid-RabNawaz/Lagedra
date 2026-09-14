# Uploads a web build to the staging SPA bucket, then invalidates CloudFront.
# Hashed assets go up first; index.html is written last with no-cache so a
# partial sync cannot leave the homepage importing a missing chunk (CloudFront
# would then serve index.html as JavaScript and the app dies on lazy load).
#
# Usage (from repo root):
#   .\deploy\aws\deploy-web-staging.ps1
#   .\deploy\aws\deploy-web-staging.ps1 -DistDir apps/web/dist-staging

param(
    [string] $DistDir = "apps/web/dist-staging",
    [string] $Bucket = "lagedra-web-staging",
    [string] $DistributionId = "E37RPWXO07K09F",
    [string] $Region = "us-west-1"
)

$ErrorActionPreference = "Stop"

$resolved = Resolve-Path $DistDir
$index = Join-Path $resolved "index.html"
if (-not (Test-Path $index)) {
    throw "No index.html under $resolved. Build the web app first."
}

Write-Host "Uploading assets from $resolved to s3://$Bucket"
aws s3 sync $resolved "s3://$Bucket" `
    --exclude "index.html" `
    --cache-control "public, max-age=31536000, immutable" `
    --region $Region
if ($LASTEXITCODE -ne 0) { throw "s3 sync failed" }

aws s3 cp $index "s3://$Bucket/index.html" `
    --content-type "text/html" `
    --cache-control "public, max-age=0, must-revalidate" `
    --region $Region
if ($LASTEXITCODE -ne 0) { throw "index.html upload failed" }

Write-Host "Checking that every local JS file landed on S3"
$localJs = Get-ChildItem (Join-Path $resolved "assets") -Filter *.js
$missing = @()
foreach ($file in $localJs) {
    $key = "assets/$($file.Name)"
    aws s3api head-object --bucket $Bucket --key $key --region $Region | Out-Null
    if ($LASTEXITCODE -ne 0) { $missing += $key }
}
if ($missing.Count -gt 0) {
    throw "S3 is missing $($missing.Count) JS file(s): $($missing -join ', ')"
}

Write-Host "Invalidating CloudFront $DistributionId"
aws cloudfront create-invalidation `
    --distribution-id $DistributionId `
    --paths "/*"
if ($LASTEXITCODE -ne 0) { throw "CloudFront invalidation failed" }

Write-Host "Staging web uploaded. Hard-refresh https://staging.lagedra.com after the invalidation completes."
