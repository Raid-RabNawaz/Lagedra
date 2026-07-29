# Lagedra Deployment Guide

## Architecture Overview

Lagedra runs on AWS in the **us-west-1 (N. California)** region.

```
┌─────────────────────────────────────────────────────────────────────┐
│                         AWS us-west-1                              │
│                                                                     │
│  ┌──────────────┐          ┌──────────────────────────────────┐    │
│  │  CloudFront   │          │     ECS Cluster: lagedra-prod    │    │
│  │  lagedra.com  │          │                                  │    │
│  └──────┬───────┘          │  ┌────────────┐ ┌─────────────┐ │    │
│         │                   │  │ lagedra-api│ │lagedra-worker│ │    │
│         ▼                   │  │  (Fargate) │ │  (Fargate)  │ │    │
│  ┌──────────────┐          │  │  port 8080 │ │  port 5100  │ │    │
│  │ S3 Bucket     │          │  └─────┬──────┘ └──────┬──────┘ │    │
│  │ lagedra-web-  │          └────────┼───────────────┼────────┘    │
│  │ prod (SPA)    │                   │               │             │
│  └──────────────┘          ┌────────┼───────────────┼────────┐    │
│                             │  ALB   │    api.lagedra.com     │    │
│  ┌──────────────┐          └────────┼───────────────┼────────┘    │
│  │ ECR Repos     │                   │               │             │
│  │ lagedra/api   │                   ▼               ▼             │
│  │ lagedra/worker│          ┌──────────────────────────────────┐   │
│  └──────────────┘          │   RDS PostgreSQL: lagedra-db     │   │
│                             └──────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

## Key AWS Resources

| Resource          | Name / Identifier                                                           |
| ----------------- | --------------------------------------------------------------------------- |
| ECR (API)         | `930218374225.dkr.ecr.us-west-1.amazonaws.com/lagedra/api`                  |
| ECR (Worker)      | `930218374225.dkr.ecr.us-west-1.amazonaws.com/lagedra/worker`               |
| ECS Cluster       | `lagedra-prod`                                                              |
| ECS Service (API) | `lagedra-api`                                                               |
| ECS Service (Wkr) | `lagedra-worker`                                                            |
| S3 (Frontend)     | `lagedra-web-prod`                                                          |
| S3 (Evidence)     | `lagedra-evidence-prod`                                                     |
| S3 (Exports)      | `lagedra-exports-prod`                                                      |
| CloudFront        | Serves `lagedra.com` from S3 bucket                                         |
| ALB               | Routes `api.lagedra.com` to Fargate API tasks                               |
| RDS               | `lagedra-db` (PostgreSQL 16, db.t4g.micro)                                  |
| CloudWatch Logs   | Log group `/ecs/lagedra`, stream prefixes: `api`, `worker`                  |
| Task Definitions  | `deploy/aws/api-task-def.json`, `deploy/aws/worker-task-def.json`           |
| Config Script     | `deploy/aws/config.sh` (canonical naming for all resources)                 |

## The Dockerfile

The multi-stage [`Dockerfile`](Dockerfile) has 6 stages:

| Stage              | Base Image               | Purpose                                      |
| ------------------ | ------------------------ | -------------------------------------------- |
| `restore`          | `dotnet/sdk:10.0`        | Copies `.csproj` files, runs `dotnet restore` |
| `build`            | (from `restore`)         | Copies `src/`, runs `dotnet build`            |
| `publish-api`      | (from `build`)           | Publishes `Lagedra.ApiGateway`                |
| `publish-worker`   | (from `build`)           | Publishes `Lagedra.Worker`                    |
| `runtime`          | `dotnet/aspnet:10.0`     | Final API image (port 8080)                   |
| `runtime-worker`   | `dotnet/aspnet:10.0`     | Final Worker image (port 5100)                |

## Prerequisites

- **AWS CLI** installed and configured with credentials
- **Docker Desktop** running
- PowerShell (Windows) or Bash terminal

---

## Redeployment Procedures

### A. Backend Only (C# / .NET Changes)

Use this when you've changed any C# code in `src/`.

#### Step 1: Login to ECR

```powershell
aws ecr get-login-password --region us-west-1 | docker login --username AWS --password-stdin 930218374225.dkr.ecr.us-west-1.amazonaws.com
```

#### Step 2: Build Docker Images

```powershell
# Build API image (targets the 'runtime' stage)
docker build -t lagedra/api --target runtime -f Dockerfile .

# Build Worker image (targets the 'runtime-worker' stage)
docker build -t lagedra/worker --target runtime-worker -f Dockerfile .
```

#### Step 3: Tag and Push to ECR

```powershell
# Tag
docker tag lagedra/api:latest 930218374225.dkr.ecr.us-west-1.amazonaws.com/lagedra/api:latest
docker tag lagedra/worker:latest 930218374225.dkr.ecr.us-west-1.amazonaws.com/lagedra/worker:latest

# Push
docker push 930218374225.dkr.ecr.us-west-1.amazonaws.com/lagedra/api:latest
docker push 930218374225.dkr.ecr.us-west-1.amazonaws.com/lagedra/worker:latest
```

#### Step 4: Force ECS to Pull New Images

```powershell
aws ecs update-service --cluster lagedra-prod --service lagedra-api --force-new-deployment
aws ecs update-service --cluster lagedra-prod --service lagedra-worker --force-new-deployment
```

#### Step 5: Monitor Rollout

```powershell
aws ecs describe-services --cluster lagedra-prod --services lagedra-api lagedra-worker `
  --query "services[*].{name:serviceName,running:runningCount,desired:desiredCount,taskDef:taskDefinition}" `
  --output table
```

Wait until `running` equals `desired` (both should be 1). The old tasks drain automatically.

---

### B. Frontend Only (React / TypeScript Changes)

Use this when you've changed files in `apps/web/`.

Since Node.js is not installed on the host machine, we build inside Docker.

#### Step 1: Create Temporary Build Files

Create `apps/web/Dockerfile.build`:

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY . .
ARG VITE_API_BASE_URL
ARG VITE_GOOGLE_CLIENT_ID
ARG VITE_STRIPE_PUBLISHABLE_KEY
RUN pnpm run build

FROM scratch
COPY --from=build /app/dist /dist
```

Create `apps/web/.dockerignore`:

```
node_modules
dist
dist-new
```

#### Step 2: Build

```powershell
docker build -f apps/web/Dockerfile.build `
  --build-arg VITE_API_BASE_URL=https://api.lagedra.com `
  --build-arg VITE_GOOGLE_CLIENT_ID=262822188598-915tp2sk0cgsv9sj0c6nifnp661998ba.apps.googleusercontent.com `
  --build-arg VITE_STRIPE_PUBLISHABLE_KEY=pk_test_51TH4W49C1zhcxwIed13rUO089U9x7zqymxW7pjkgeEDIUFss6MBN9WPwXfQQSH3JWOOdnUSm0EYObonbUpprqBbh004eAq2YhU `
  -o apps/web/dist-new `
  apps/web
```

The built files will be output to `apps/web/dist-new/dist/`.

#### Step 3: Upload to S3

```powershell
aws s3 sync apps/web/dist-new/dist s3://lagedra-web-prod --delete
```

#### Step 4: Invalidate CloudFront Cache

```powershell
$distId = aws cloudfront list-distributions `
  --query "DistributionList.Items[?contains(Aliases.Items, 'lagedra.com')].Id" `
  --output text

aws cloudfront create-invalidation --distribution-id $distId --paths "/*"
```

#### Step 5: Clean Up

Delete the temporary `apps/web/Dockerfile.build` and `apps/web/.dockerignore` files.

---

### C. Environment Variable / Config Changes

Use this when you modify `deploy/aws/api-task-def.json` or `deploy/aws/worker-task-def.json`.

#### Step 1: Register New Task Definition

```powershell
# Register and note the new revision number from output
aws ecs register-task-definition --cli-input-json file://deploy/aws/api-task-def.json `
  --query "taskDefinition.revision" --output text

aws ecs register-task-definition --cli-input-json file://deploy/aws/worker-task-def.json `
  --query "taskDefinition.revision" --output text
```

#### Step 2: Update Services to Use New Revision

**IMPORTANT:** `--force-new-deployment` alone does NOT switch to a new task definition revision. You MUST explicitly specify `--task-definition` with the new revision number.

```powershell
# Replace NEW_REVISION with the number from Step 1
aws ecs update-service --cluster lagedra-prod --service lagedra-api `
  --task-definition lagedra-api:NEW_REVISION --force-new-deployment

aws ecs update-service --cluster lagedra-prod --service lagedra-worker `
  --task-definition lagedra-worker:NEW_REVISION --force-new-deployment
```

---

### D. Full Redeploy (Backend + Frontend)

When you have changes in both `src/` and `apps/web/`, run sections A and B in parallel since they are independent. The overall flow:

```
Backend path:                    Frontend path:
  Build Docker images              Build frontend in Docker
       │                                │
       ▼                                ▼
  Push to ECR                    Sync to S3
       │                                │
       ▼                                ▼
  Force ECS redeploy         Invalidate CloudFront
```

---

## Adding a New .NET Module

When you add a new module project (e.g., `src/Lagedra.Modules/NewModule/NewModule.csproj`):

1. Add it to `Lagedra.sln`
2. Add a `COPY` line in the `Dockerfile` restore stage:
   ```dockerfile
   COPY src/Lagedra.Modules/NewModule/NewModule.csproj  src/Lagedra.Modules/NewModule/
   ```
3. Rebuild and push as described in Section A.

---

## Monitoring & Troubleshooting

### Check Service Health

```powershell
aws ecs describe-services --cluster lagedra-prod --services lagedra-api lagedra-worker `
  --query "services[*].{name:serviceName,running:runningCount,desired:desiredCount,taskDef:taskDefinition}" `
  --output table
```

### View Recent Error Logs

```powershell
# API errors (last 30 minutes)
aws logs filter-log-events --log-group-name "/ecs/lagedra" `
  --log-stream-name-prefix "api" --filter-pattern "ERR" `
  --start-time ([DateTimeOffset]::UtcNow.AddMinutes(-30).ToUnixTimeMilliseconds()) `
  --query "events[*].message" --output text

# Worker errors (last 30 minutes)
aws logs filter-log-events --log-group-name "/ecs/lagedra" `
  --log-stream-name-prefix "worker" --filter-pattern "ERR" `
  --start-time ([DateTimeOffset]::UtcNow.AddMinutes(-30).ToUnixTimeMilliseconds()) `
  --query "events[*].message" --output text
```

### Test API Health

```powershell
Invoke-RestMethod https://api.lagedra.com/health
```

### Check Which Task Definition Revision Is Running

```powershell
aws ecs describe-services --cluster lagedra-prod --services lagedra-api `
  --query "services[0].deployments[*].{status:status,taskDef:taskDefinition,running:runningCount}" `
  --output table
```

### View All Logs (Not Just Errors)

```powershell
aws logs filter-log-events --log-group-name "/ecs/lagedra" `
  --log-stream-name-prefix "api" `
  --start-time ([DateTimeOffset]::UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds()) `
  --query "events[*].message" --output text
```

---

## Frontend Environment Variables

These are baked into the frontend at build time via Vite's `import.meta.env`:

| Variable                       | Production Value                                                    |
| ------------------------------ | ------------------------------------------------------------------- |
| `VITE_API_BASE_URL`            | `https://api.lagedra.com`                                           |
| `VITE_GOOGLE_CLIENT_ID`        | `262822188598-915tp2sk0cgsv9sj0c6nifnp661998ba.apps.googleusercontent.com` |
| `VITE_STRIPE_PUBLISHABLE_KEY`  | `pk_test_51TH4W49C1zhcxwIed13rUO089U9x7zqymxW7pjkgeEDIUFss6MBN9WPwXfQQSH3JWOOdnUSm0EYObonbUpprqBbh004eAq2YhU` |

Defined in [`apps/web/src/app/config.ts`](apps/web/src/app/config.ts).

---

## Backend Environment Variables

Configured in the ECS task definitions (`deploy/aws/api-task-def.json` and `deploy/aws/worker-task-def.json`).

**Plaintext (in task definition JSON):**

| Variable                          | Description                        |
| --------------------------------- | ---------------------------------- |
| `ASPNETCORE_ENVIRONMENT`          | `Production`                       |
| `ConnectionStrings__Default`      | RDS connection string              |
| `Jwt__Issuer`                     | `https://api.lagedra.com`          |
| `Jwt__Audience`                   | `https://lagedra.com`              |
| `App__BaseUrl`                    | `https://api.lagedra.com`          |
| `App__FrontendUrl`                | `https://lagedra.com`              |
| `Twilio__FromEmail`               | Sender email address               |
| `Twilio__FromName`                | `Lagedra`                          |
| `ExternalAuth__Google__ClientId`  | Google OAuth Client ID             |
| `Stripe__PublishableKey`          | Stripe public key                  |
| `MinIO__Endpoint`                 | `s3.us-west-1.amazonaws.com`       |

**Secrets (from AWS SSM Parameter Store):**

| Variable                 | SSM Path                                          |
| ------------------------ | ------------------------------------------------- |
| `Jwt__Secret`            | `/lagedra/prod/jwt-secret`                        |
| `Stripe__SecretKey`      | `/lagedra/prod/stripe-secret-key`                 |
| `Stripe__WebhookSecret`  | `/lagedra/prod/stripe-webhook-secret`              |
| `GoogleMaps__ApiKey`     | `/lagedra/prod/google-maps-key`                   |

---

## External Service Dependencies

| Service       | Purpose                        | Config Location              |
| ------------- | ------------------------------ | ---------------------------- |
| Twilio SendGrid | Transactional email            | SSM: `twilio-sendgrid-api-key` |
| Twilio SMS      | OTP + notification SMS         | SSM: account + API key / token |
| Google OAuth   | Social sign-in                 | Task def + frontend env      |
| Google Maps   | Map rendering                  | SSM Parameter Store          |
| Stripe        | Payment processing             | SSM + task def               |
| GoDaddy       | Domain DNS for `lagedra.com`   | GoDaddy DNS panel            |

---

## Common Issues & Fixes

### "dotnet restore" fails in Docker with missing .csproj

When you add a new module to the solution, you must also add a `COPY` line for its `.csproj` file in the Dockerfile's restore stage.

### ECS service not picking up new task definition revision

`--force-new-deployment` only restarts the same revision. You must pass `--task-definition lagedra-api:N` explicitly with the new revision number.

### Email sending fails (SendGrid 401/403)

Verify `Twilio__SendGridApiKey` and `Twilio__FromEmail` in SSM / task definition. The From address must be a verified sender/domain in SendGrid.

### SMS sending fails (Twilio 401)

Verify `Twilio__AccountSid`, `Twilio__MessagingServiceSid`, and either `Twilio__AuthToken` or `Twilio__ApiKeySid` + `Twilio__ApiKeySecret`.

### Frontend shows old content after deploy

CloudFront cache needs invalidation. Run the invalidation command from Section B Step 4.

### Container crashes on startup (Data Protection keys)

The Dockerfile creates `/app/data-protection-keys` with correct permissions. If you rebuild from scratch, ensure this directory creation step is present in both `runtime` and `runtime-worker` stages.
