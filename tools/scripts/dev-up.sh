#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

echo "Starting Lagedra services..."
docker compose up -d --build

echo "Waiting for postgres to be healthy..."
for i in $(seq 1 30); do
  health=$(docker inspect --format "{{.State.Health.Status}}" lagedra-pg 2>/dev/null || echo "unknown")
  [ "$health" = "healthy" ] && break
  sleep 2
done

echo "Running EF Core migrations..."
"$(dirname "$0")/db-migrate.sh"

echo "Seeding reference data..."
"$(dirname "$0")/db-seed.sh"

echo ""
echo "All services are up:"
echo "  API (Swagger)   -> http://localhost:8080/swagger"
echo "  Web app         -> http://localhost:3000"
echo "  Admin app       -> http://localhost:3001"
echo "  Marketing site  -> http://localhost:3002"
echo "  MinIO console   -> http://localhost:9001"
