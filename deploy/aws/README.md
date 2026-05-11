# AWS Deployment Scripts

Step-by-step scripts for deploying Lagedra to AWS using ECS Fargate.

## Prerequisites

- AWS CLI v2 installed and configured (`aws configure`)
- Docker Desktop installed
- `bash` shell (Git Bash on Windows, or WSL)

## Quick Start

1. **Edit `config.sh`** — set your AWS region, account ID, and domain name.

2. **Run scripts in order:**

```bash
# From the repository root:

# 1. Networking (VPC security groups)
bash deploy/aws/01-networking.sh

# 2. RDS PostgreSQL (save the endpoint from output)
RDS_MASTER_PASSWORD='YourStrongPassword' bash deploy/aws/02-rds.sh

# 3. S3 buckets
bash deploy/aws/03-s3.sh

# 4. Secrets (SSM Parameter Store)
PARAM_JWT_SECRET='...' \
PARAM_STRIPE_SECRET_KEY='sk_live_...' \
# ... set all params ...
bash deploy/aws/04-secrets.sh

# 5. ECR repositories
bash deploy/aws/05-ecr.sh

# 6. ECS cluster + IAM roles
bash deploy/aws/06-ecs-cluster.sh

# 7. ECS services (needs outputs from steps 1, 2)
APP_SG_ID='sg-xxx' \
SUBNET_1='subnet-xxx' \
SUBNET_2='subnet-yyy' \
RDS_ENDPOINT='lagedra-db.xxx.rds.amazonaws.com' \
RDS_MASTER_PASSWORD='...' \
bash deploy/aws/07-ecs-services.sh

# 8. Frontend deployment
bash deploy/aws/08-frontend.sh

# 9. DNS & SSL certificates
ALB_ARN='arn:aws:...' bash deploy/aws/09-dns-ssl.sh

# 10. Database migrations
DB_SG_ID='sg-xxx' \
RDS_ENDPOINT='lagedra-db.xxx.rds.amazonaws.com' \
RDS_MASTER_PASSWORD='...' \
bash deploy/aws/10-migrations.sh
```

## GitHub Actions CI/CD

After initial setup, deployments are automated via `.github/workflows/deploy.yml`.

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AWS_DEPLOY_ROLE_ARN` | IAM role ARN for GitHub OIDC |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID |
| `STRIPE_PUBLISHABLE_KEY` | Stripe publishable key |
| `GOOGLE_MAPS_API_KEY` | Google Maps API key |

### Required GitHub Variables

| Variable | Description |
|----------|-------------|
| `AWS_REGION` | AWS region (e.g. `eu-west-1`) |
| `VITE_API_BASE_URL` | API URL (e.g. `https://api.yourdomain.com`) |
| `S3_WEB_BUCKET` | S3 bucket for SPA (e.g. `lagedra-web-prod`) |
| `CLOUDFRONT_DISTRIBUTION_ID` | CloudFront distribution ID |

## Estimated Monthly Cost

| Service | Cost |
|---------|------|
| RDS PostgreSQL (db.t4g.micro) | $0 (free tier) / $13 |
| ECS Fargate API (0.5 vCPU, 1GB) | ~$15 |
| ECS Fargate Worker (Spot, 0.25 vCPU) | ~$3-5 |
| ALB | ~$16.50 |
| S3 + CloudFront | ~$2-3 |
| CloudWatch Logs | ~$1-2 |
| **Total** | **~$40-45/mo** (year 1) |
