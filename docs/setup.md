# VoidPulse Setup Guide

This guide covers installation, configuration, and deployment of VoidPulse.

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| .NET SDK | 10.0+ | Build and run the backend |
| Docker | 24.0+ | Container runtime |
| Docker Compose | 2.20+ | Multi-container orchestration |
| PostgreSQL | 16+ | Primary database (or use Docker) |
| Redis | 7+ | Caching layer (or use Docker) |

### Optional Software

| Software | Version | Purpose |
|----------|---------|---------|
| kubectl | 1.28+ | Kubernetes deployment |
| EF Core CLI | 10.0+ | Database migrations |

### Install .NET 10 SDK

**Windows:**
```powershell
winget install Microsoft.DotNet.SDK.10
```

**macOS:**
```bash
brew install dotnet-sdk
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

Verify installation:
```bash
dotnet --version
```

### Install Docker

Follow the official guide: https://docs.docker.com/get-docker/

Verify installation:
```bash
docker --version
docker compose version
```

### Install EF Core CLI (optional, for migrations)

```bash
dotnet tool install --global dotnet-ef
```

---

## Quick Start with Docker Compose

The fastest way to get VoidPulse running locally.

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/VoidPulse.git
cd VoidPulse
```

### 2. Configure Environment

```bash
cp .env.example .env
```

Edit `.env` and set your values:

```bash
# ============================================
# VoidPulse — Environment Variables
# ============================================

# Database connection string
# For Docker Compose, use the service name "db" as the host
DATABASE_URL=Host=db;Port=5432;Database=voidpulse;Username=postgres;Password=postgres

# Redis connection
# For Docker Compose, use the service name "redis" as the host
REDIS_URL=redis:6379

# JWT signing secret — MUST be at least 32 characters
# Generate one with: openssl rand -base64 48
JWT__Secret=your-secret-key-here-must-be-at-least-32-characters-long

# Access token expiry in minutes (default: 30)
JWT__AccessTokenExpiryMinutes=30

# Refresh token expiry in days (default: 7)
JWT__RefreshTokenExpiryDays=7

# ASP.NET Core environment (Development, Staging, Production)
ASPNETCORE_ENVIRONMENT=Development

# Allowed CORS origins (comma-separated)
CORS__AllowedOrigins=http://localhost:3000
```

### 3. Start All Services

```bash
docker compose up -d
```

This starts three containers:
- `backend` -- VoidPulse API on port 8080
- `db` -- PostgreSQL 16 on port 5432
- `redis` -- Redis 7 on port 6379

### 4. Verify

```bash
curl http://localhost:8080/api/v1/health
```

Expected response:
```json
{"status":"healthy","timestamp":"2026-01-30T12:00:00Z"}
```

### 5. Stop Services

```bash
docker compose down
```

To also remove volumes (database data):
```bash
docker compose down -v
```

---

## Local Development Setup

For active development, run PostgreSQL and Redis in Docker while running the .NET API locally.

### 1. Start Infrastructure

```bash
docker compose up -d db redis
```

### 2. Restore Dependencies

```bash
dotnet restore src/backend/
```

### 3. Configure Local Environment

The API reads configuration from `src/backend/VoidPulse.Api/appsettings.Development.json` and environment variables. For local development, environment variables or a `.env` file at the project root are sufficient.

Set these environment variables (or update `appsettings.Development.json`):

```bash
# Linux/macOS
export DATABASE_URL="Host=localhost;Port=5432;Database=voidpulse;Username=postgres;Password=postgres"
export REDIS_URL="localhost:6379"
export JWT__Secret="dev-super-secret-key-change-in-production-min-32-chars!!"
```

```powershell
# Windows PowerShell
$env:DATABASE_URL = "Host=localhost;Port=5432;Database=voidpulse;Username=postgres;Password=postgres"
$env:REDIS_URL = "localhost:6379"
$env:JWT__Secret = "dev-super-secret-key-change-in-production-min-32-chars!!"
```

### 4. Run Database Migrations

```bash
dotnet ef database update \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api
```

### 5. Run the API

```bash
dotnet run --project src/backend/VoidPulse.Api
```

The API will start on `http://localhost:8080`.

In development mode, Swagger UI is available at `http://localhost:8080/swagger`.

### 6. Run Tests

```bash
# All tests
dotnet test src/backend/

# Unit tests only
dotnet test src/backend/VoidPulse.Tests/ --filter "FullyQualifiedName~Unit"

# Integration tests only
dotnet test src/backend/VoidPulse.Tests/ --filter "FullyQualifiedName~Integration"

# With verbose output
dotnet test src/backend/ --verbosity normal
```

---

## Database Setup

### Using Docker (Recommended for Development)

The `docker-compose.yml` automatically provisions a PostgreSQL 16 instance:

```bash
docker compose up -d db
```

Default credentials:
- Host: `localhost`
- Port: `5432`
- Database: `voidpulse`
- User: `postgres`
- Password: `postgres`

### Manual PostgreSQL Setup

If you prefer running PostgreSQL natively:

1. Install PostgreSQL 16.
2. Create the database:

```sql
CREATE DATABASE voidpulse;
```

3. Update the `DATABASE_URL` environment variable to point to your instance.

### Creating Migrations

When entity models change, create a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api \
  --output-dir Data/Migrations
```

Apply the migration:

```bash
dotnet ef database update \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api
```

### Reverting Migrations

Roll back the last migration:

```bash
dotnet ef database update <PreviousMigrationName> \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api
```

---

## Redis Setup

### Using Docker (Recommended)

```bash
docker compose up -d redis
```

Default: `localhost:6379`, no password.

### Manual Redis Setup

1. Install Redis 7.
2. Start the server: `redis-server`
3. Set `REDIS_URL=localhost:6379` in your environment.

### Verifying Redis

```bash
redis-cli ping
# Expected output: PONG
```

---

## Production Deployment

### Docker Deployment

#### Build the Image

```bash
docker build -t voidpulse-backend:latest .
```

The multi-stage Dockerfile:
1. Builds with the .NET 10 SDK image
2. Publishes a Release build
3. Runs on the lightweight ASP.NET runtime image
4. Runs as a non-root user (`appuser`)
5. Includes a health check

#### Run the Container

```bash
docker run -d \
  --name voidpulse-api \
  -p 8080:8080 \
  -e DATABASE_URL="Host=your-db-host;Port=5432;Database=voidpulse;Username=app_user;Password=strong_password" \
  -e REDIS_URL="your-redis-host:6379" \
  -e JWT__Secret="$(openssl rand -base64 48)" \
  -e JWT__AccessTokenExpiryMinutes=15 \
  -e JWT__RefreshTokenExpiryDays=7 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e CORS__AllowedOrigins="https://your-domain.com" \
  voidpulse-backend:latest
```

### Docker Compose (Production-like)

For production-like environments, override values via `.env`:

```bash
# .env
DATABASE_URL=Host=db;Port=5432;Database=voidpulse;Username=postgres;Password=<strong-password>
REDIS_URL=redis:6379
JWT__Secret=<generated-secret-at-least-32-chars>
ASPNETCORE_ENVIRONMENT=Production
CORS__AllowedOrigins=https://your-domain.com
```

```bash
docker compose up -d
```

### Kubernetes Deployment

The `k8s/` directory contains all required manifests.

#### 1. Create Namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

#### 2. Configure Secrets

**Important**: Edit `k8s/secrets.yaml` and replace all placeholder values with real base64-encoded secrets. In production, use an external secrets manager (HashiCorp Vault, AWS Secrets Manager, etc.) instead of committing secrets to YAML files.

```bash
# Generate a base64-encoded secret
echo -n "your-strong-password" | base64

# Apply secrets
kubectl apply -f k8s/secrets.yaml
```

#### 3. Apply ConfigMap

```bash
kubectl apply -f k8s/configmap.yaml
```

#### 4. Deploy Database

```bash
kubectl apply -f k8s/database/pvc.yaml
kubectl apply -f k8s/database/statefulset.yaml
kubectl apply -f k8s/database/service.yaml
```

#### 5. Deploy Redis

```bash
kubectl apply -f k8s/redis/deployment.yaml
kubectl apply -f k8s/redis/service.yaml
```

#### 6. Deploy Backend

```bash
kubectl apply -f k8s/backend/deployment.yaml
kubectl apply -f k8s/backend/service.yaml
kubectl apply -f k8s/backend/hpa.yaml
```

#### 7. Configure Ingress

```bash
kubectl apply -f k8s/ingress.yaml
```

#### 8. Verify Deployment

```bash
# Check pod status
kubectl get pods -n voidpulse

# Check services
kubectl get svc -n voidpulse

# View logs
kubectl logs -f deployment/voidpulse-backend -n voidpulse

# Test health endpoint
kubectl port-forward svc/voidpulse-backend 8080:8080 -n voidpulse
curl http://localhost:8080/api/v1/health
```

#### Scaling

The HPA (Horizontal Pod Autoscaler) automatically scales the backend between the configured min and max replicas based on CPU utilization. To manually scale:

```bash
kubectl scale deployment voidpulse-backend --replicas=3 -n voidpulse
```

---

## CI/CD

The project uses GitHub Actions for continuous integration. The workflow file is at `.github/workflows/ci.yml`.

**Pipeline stages:**

1. **Backend Tests** -- Runs on every push to `main`/`develop` and on PRs to `main`. Starts a PostgreSQL service container, restores dependencies, builds, and runs all tests.

2. **Docker Build** -- Runs only on pushes to `main` (after tests pass). Builds the Docker image.

To extend the pipeline with deployment, add stages to push the image to a container registry and apply Kubernetes manifests.

---

## Troubleshooting

### API fails to start: "Connection refused" to PostgreSQL

**Cause**: PostgreSQL is not running or not ready.

**Solution**:
```bash
# Check if the container is running
docker compose ps

# Check PostgreSQL logs
docker compose logs db

# Wait for health check to pass
docker compose up -d db
sleep 10
docker compose up -d backend
```

### API fails to start: "Connection refused" to Redis

**Cause**: Redis is not running.

**Solution**:
```bash
docker compose up -d redis
docker compose logs redis
```

### JWT authentication returns 401

**Cause**: Token expired, invalid secret, or missing `Authorization` header.

**Solution**:
- Verify the `JWT__Secret` value matches between token generation and validation
- Check that the access token has not expired (default: 30 min)
- Ensure the header format is `Authorization: Bearer <token>` (note the space after "Bearer")
- Use the `/api/v1/auth/refresh` endpoint to get a new access token

### Database migration errors

**Cause**: Migration out of sync with the current database state.

**Solution**:
```bash
# Check current migration status
dotnet ef migrations list \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api

# Force update to latest
dotnet ef database update \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api
```

### Swagger UI not loading

**Cause**: Swagger is only enabled in `Development` environment.

**Solution**: Ensure `ASPNETCORE_ENVIRONMENT=Development` is set. Swagger UI is not available in Production.

### Port 8080 already in use

**Cause**: Another process is using port 8080.

**Solution**:
```bash
# Find the process
# Linux/macOS:
lsof -i :8080

# Windows:
netstat -ano | findstr :8080

# Change the port in docker-compose.yml or use a different port mapping
docker compose up -d  # after editing ports in docker-compose.yml
```

### Redis cache not working

**Cause**: Redis connection failure or misconfigured `REDIS_URL`.

**Solution**:
```bash
# Test Redis connectivity
redis-cli -h localhost -p 6379 ping

# Check the REDIS_URL format (should be host:port, no protocol prefix)
# Correct: localhost:6379
# Incorrect: redis://localhost:6379
```

### Correlation ID not appearing in logs

**Cause**: The `X-Correlation-Id` header is optional. If not provided, one is auto-generated.

**Solution**: Pass the header in your requests for easier tracing:
```bash
curl -H "X-Correlation-Id: my-trace-123" http://localhost:8080/api/v1/health
```
