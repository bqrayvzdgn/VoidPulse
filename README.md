# VoidPulse

Multi-tenant network traffic monitoring system with role-based access control, agent-based traffic ingestion, PCAP upload, dashboard analytics, and data retention policy management.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10, ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Database | PostgreSQL 16 |
| Cache | Redis 7 |
| Auth | JWT Bearer (access + refresh tokens) |
| Logging | Serilog (structured JSON) |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| CI/CD | GitHub Actions |
| Containers | Docker, Docker Compose |
| Orchestration | Kubernetes |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) and Docker Compose
- [PostgreSQL 16](https://www.postgresql.org/download/) (or use Docker)
- [Redis 7](https://redis.io/download) (or use Docker)

## Quick Start

### Using Docker Compose (recommended)

```bash
# Clone the repository
git clone https://github.com/your-org/VoidPulse.git
cd VoidPulse

# Copy environment file and update values
cp .env.example .env

# Start all services
docker compose up -d

# Verify the API is running
curl http://localhost:8080/api/v1/health
```

The API will be available at `http://localhost:8080`. PostgreSQL runs on port `5432` and Redis on port `6379`.

### Local Development

```bash
# Start only infrastructure (PostgreSQL + Redis)
docker compose up -d db redis

# Restore NuGet packages
dotnet restore src/backend/

# Run database migrations
dotnet ef database update \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api

# Run the API
dotnet run --project src/backend/VoidPulse.Api
```

### Using the Makefile

```bash
make dev          # Start all services via Docker Compose
make dev-down     # Stop all services
make build        # Build Docker images
make test         # Run tests
make restore      # Restore NuGet packages
make clean        # Clean build artifacts
make migrate      # Run database migrations
make logs         # Tail logs from all services
```

## Project Structure

```
VoidPulse/
├── src/backend/
│   ├── VoidPulse.Api/              # API layer (controllers, middleware, DI)
│   │   ├── Controllers/            # REST endpoint controllers
│   │   ├── Middleware/             # Exception, CorrelationId, Tenant middleware
│   │   ├── Extensions/            # Service collection extensions
│   │   └── Program.cs             # Application entry point
│   ├── VoidPulse.Domain/          # Domain layer (entities, interfaces, exceptions)
│   │   ├── Entities/              # Domain entities (Tenant, User, TrafficFlow, etc.)
│   │   ├── Interfaces/            # Repository interfaces
│   │   └── Exceptions/            # Domain exceptions
│   ├── VoidPulse.Application/     # Application layer (services, DTOs, validators)
│   │   ├── DTOs/                  # Request/response data transfer objects
│   │   ├── Interfaces/            # Service interfaces
│   │   ├── Services/              # Business logic implementations
│   │   ├── Validators/            # FluentValidation validators
│   │   ├── Mappings/              # AutoMapper profiles
│   │   └── Common/                # ApiResponse, PagedResult
│   ├── VoidPulse.Infrastructure/  # Infrastructure layer (EF Core, Redis, JWT)
│   │   ├── Data/                  # DbContext, entity configurations
│   │   ├── Repositories/          # Repository implementations
│   │   └── Services/              # JWT, password hashing, Redis cache
│   ├── VoidPulse.Tests/           # Unit and integration tests
│   └── VoidPulse.sln              # Solution file
├── k8s/                           # Kubernetes manifests
├── .github/workflows/             # GitHub Actions CI/CD
├── docker-compose.yml             # Local development compose
├── Dockerfile                     # Multi-stage production build
├── Makefile                       # Developer convenience commands
├── .env.example                   # Environment variable template
└── docs/                          # Documentation
    ├── api.md                     # API reference
    ├── architecture.md            # Architecture guide
    └── setup.md                   # Setup guide
```

## API Overview

All endpoints are prefixed with `/api/v1`. Responses use a standard envelope format.

### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/auth/register` | Register user and tenant | None |
| POST | `/api/v1/auth/login` | Login, returns JWT pair | None |
| POST | `/api/v1/auth/refresh` | Refresh access token | None |
| DELETE | `/api/v1/auth/logout` | Invalidate refresh token | Bearer |

### Tenants (SuperAdmin only)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/tenants` | List tenants (paginated) |
| POST | `/api/v1/tenants` | Create tenant |
| GET | `/api/v1/tenants/{id}` | Get tenant by ID |
| PUT | `/api/v1/tenants/{id}` | Update tenant |
| DELETE | `/api/v1/tenants/{id}` | Soft-delete tenant |

### Users (TenantAdmin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/users` | List users in tenant (paginated) |
| POST | `/api/v1/users` | Create user |
| GET | `/api/v1/users/{id}` | Get user by ID |
| PUT | `/api/v1/users/{id}` | Update user |
| DELETE | `/api/v1/users/{id}` | Soft-delete user |

### Agent Keys (TenantAdmin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/agents` | List agent keys |
| POST | `/api/v1/agents` | Create agent key |
| GET | `/api/v1/agents/{id}` | Get agent key |
| PUT | `/api/v1/agents/{id}` | Update agent key |
| DELETE | `/api/v1/agents/{id}` | Revoke agent key |

### Traffic (Analyst+ or Agent API Key)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/traffic/ingest` | Ingest single traffic flow | X-Api-Key |
| POST | `/api/v1/traffic/ingest/batch` | Batch ingest traffic flows | X-Api-Key |
| GET | `/api/v1/traffic` | Query/filter traffic flows | Analyst |
| GET | `/api/v1/traffic/{id}` | Get flow detail | Analyst |
| GET | `/api/v1/traffic/export` | Export flows as CSV | Analyst |

### Dashboard (Viewer+)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/dashboard/overview` | Summary stats |
| GET | `/api/v1/dashboard/top-talkers` | Top IPs by volume |
| GET | `/api/v1/dashboard/protocol-distribution` | Traffic by protocol |
| GET | `/api/v1/dashboard/bandwidth` | Bandwidth over time |

### Retention Policy (TenantAdmin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/retention` | Get retention policy |
| PUT | `/api/v1/retention` | Set/update retention policy |

### Saved Filters (Analyst+)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/saved-filters` | List saved filters |
| POST | `/api/v1/saved-filters` | Save a filter |
| GET | `/api/v1/saved-filters/{id}` | Get filter |
| PUT | `/api/v1/saved-filters/{id}` | Update filter |
| DELETE | `/api/v1/saved-filters/{id}` | Delete filter |

### Health

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/health` | Service health check | None |

For detailed API documentation with request/response examples, see [docs/api.md](docs/api.md).

## Standard Response Format

### Success

```json
{
  "success": true,
  "data": { },
  "error": null,
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5
  }
}
```

### Error

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [
      { "field": "email", "message": "Invalid email format" }
    ]
  }
}
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DATABASE_URL` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=voidpulse;Username=postgres;Password=postgres` |
| `REDIS_URL` | Redis connection string | `localhost:6379` |
| `JWT__Secret` | JWT signing secret (min 32 chars) | **(required)** |
| `JWT__AccessTokenExpiryMinutes` | Access token TTL in minutes | `30` |
| `JWT__RefreshTokenExpiryDays` | Refresh token TTL in days | `7` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |
| `CORS__AllowedOrigins` | Comma-separated allowed CORS origins | `http://localhost:3000` |
| `Logging__LogLevel__Default` | Default log level | `Information` |

## Testing

```bash
# Run all tests
dotnet test src/backend/

# Run with verbose output
dotnet test src/backend/ --verbosity normal

# Run only unit tests
dotnet test src/backend/VoidPulse.Tests/ --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test src/backend/VoidPulse.Tests/ --filter "FullyQualifiedName~Integration"
```

## Deployment

### Docker

```bash
# Build the image
docker build -t voidpulse-backend:latest .

# Run with environment variables
docker run -d \
  -p 8080:8080 \
  -e DATABASE_URL="Host=db;Port=5432;Database=voidpulse;Username=postgres;Password=secret" \
  -e REDIS_URL="redis:6379" \
  -e JWT__Secret="your-production-secret-min-32-chars" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  voidpulse-backend:latest
```

### Kubernetes

```bash
# Create the namespace
kubectl apply -f k8s/namespace.yaml

# Update secrets (replace placeholder values first!)
kubectl apply -f k8s/secrets.yaml

# Deploy all resources
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/database/
kubectl apply -f k8s/redis/
kubectl apply -f k8s/backend/
kubectl apply -f k8s/ingress.yaml
```

For detailed deployment instructions, see [docs/setup.md](docs/setup.md).

## Documentation

- [API Reference](docs/api.md) -- Complete endpoint documentation with request/response examples
- [Architecture Guide](docs/architecture.md) -- System design, diagrams, and key decisions
- [Setup Guide](docs/setup.md) -- Detailed installation and deployment instructions

## License

This project is licensed under the MIT License.
