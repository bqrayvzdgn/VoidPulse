# Project Architecture

## Overview
- **Project Name**: VoidPulse
- **Description**: Multi-tenant network traffic monitoring system with role-based access control, agent-based traffic ingestion, PCAP upload, dashboard analytics, and retention policy management.
- **Technology Stack**:
  - Backend: .NET 10, ASP.NET Core Web API, Entity Framework Core
  - Database: PostgreSQL 16 (shared DB, TenantId column for multi-tenancy)
  - Cache: Redis 7
  - Auth: JWT Bearer (access + refresh tokens)
  - Infrastructure: Docker, Docker Compose, Kubernetes, GitHub Actions

## System Design

```mermaid
graph TB
    Agent[Network Agent] -->|Traffic Data / PCAP| API[ASP.NET Core API]
    Client[Web Client] -->|HTTP/REST| API
    API -->|Auth Middleware| JWT[JWT Service]
    API -->|Multi-Tenant Middleware| TenantResolver[Tenant Resolver]
    API -->|Read/Write| EF[EF Core]
    EF -->|SQL| PG[(PostgreSQL)]
    API -->|Cache| Redis[(Redis)]
    API -->|Health| Health[/health endpoint/]
```

### Request Flow
1. Client/Agent sends HTTP request to API
2. CorrelationId middleware assigns request ID
3. Exception middleware wraps error handling
4. JWT Authentication middleware validates Bearer token
5. Tenant resolution middleware extracts TenantId from JWT claims
6. Controller action executes business logic via Application services
7. Services interact with repositories (EF Core → PostgreSQL) and cache (Redis)
8. Response returned in standard ApiResponse format

### Authentication Strategy
- **JWT Access Token**: 15 minute expiry, contains UserId, TenantId, Roles
- **JWT Refresh Token**: 7 day expiry, stored in database, single-use rotation
- **Password hashing**: BCrypt with cost factor 12
- **Role-based access**: SuperAdmin (cross-tenant), TenantAdmin, Analyst, Viewer

## Data Model

### Entities

#### BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| Id | Guid | PK, auto-generated |
| CreatedAt | DateTime | UTC, auto-set |
| UpdatedAt | DateTime | UTC, auto-set |
| DeletedAt | DateTime? | Soft delete |

#### Tenant : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| Name | string | Required, max 200 |
| Slug | string | Required, unique, max 100 |
| IsActive | bool | Default true |

#### User : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| TenantId | Guid | FK → Tenant, required |
| Email | string | Required, unique per tenant, max 256 |
| PasswordHash | string | Required |
| FullName | string | Required, max 200 |
| IsActive | bool | Default true |
| LastLoginAt | DateTime? | |
| RefreshToken | string? | |
| RefreshTokenExpiresAt | DateTime? | |

#### Role : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| Name | string | Required, unique, max 50 |
| Description | string? | max 200 |

Seed roles: SuperAdmin, TenantAdmin, Analyst, Viewer

#### UserRole
| Property | Type | Constraints |
|----------|------|-------------|
| UserId | Guid | FK → User, composite PK |
| RoleId | Guid | FK → Role, composite PK |

#### TrafficFlow : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| TenantId | Guid | FK → Tenant, required |
| AgentKeyId | Guid | FK → AgentKey, required |
| SourceIp | string | Required, max 45 |
| DestinationIp | string | Required, max 45 |
| SourcePort | int | Required |
| DestinationPort | int | Required |
| Protocol | string | Required, max 10 (TCP/UDP/ICMP) |
| BytesSent | long | Required |
| BytesReceived | long | Required |
| PacketsSent | int | Required |
| PacketsReceived | int | Required |
| StartedAt | DateTime | Required |
| EndedAt | DateTime | Required |
| FlowDuration | double | Computed, seconds |

#### HttpMetadata : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| TrafficFlowId | Guid | FK → TrafficFlow, required, unique |
| Method | string | Required, max 10 |
| Host | string | Required, max 500 |
| Path | string | Required, max 2000 |
| StatusCode | int | Required |
| UserAgent | string? | max 1000 |
| ContentType | string? | max 200 |
| ResponseTimeMs | double | Required |

#### AgentKey : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| TenantId | Guid | FK → Tenant, required |
| Name | string | Required, max 200 |
| ApiKey | string | Required, unique, max 128 |
| IsActive | bool | Default true |
| LastUsedAt | DateTime? | |

#### RetentionPolicy : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| TenantId | Guid | FK → Tenant, required, unique |
| RetentionDays | int | Required, default 90 |

#### SavedFilter : BaseEntity
| Property | Type | Constraints |
|----------|------|-------------|
| UserId | Guid | FK → User, required |
| TenantId | Guid | FK → Tenant, required |
| Name | string | Required, max 200 |
| FilterJson | string | Required, JSON blob |

### ER Diagram

```mermaid
erDiagram
    Tenant ||--o{ User : has
    Tenant ||--o{ AgentKey : has
    Tenant ||--o{ TrafficFlow : has
    Tenant ||--o| RetentionPolicy : has
    Tenant ||--o{ SavedFilter : has
    User ||--o{ UserRole : has
    Role ||--o{ UserRole : has
    User ||--o{ SavedFilter : has
    AgentKey ||--o{ TrafficFlow : ingests
    TrafficFlow ||--o| HttpMetadata : has
```

## API Specification

### Standard Response Format
```json
{
  "success": true,
  "data": {},
  "error": null,
  "meta": { "page": 1, "pageSize": 20, "totalCount": 100, "totalPages": 5 }
}
```

### Error Format
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [{ "field": "email", "message": "Invalid email format" }]
  }
}
```

### Endpoints

#### Health
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/health | Service health check | No |

#### Auth
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/auth/register | Register new user (first user becomes TenantAdmin) | No |
| POST | /api/v1/auth/login | Login, returns JWT pair | No |
| POST | /api/v1/auth/refresh | Refresh access token | No |
| DELETE | /api/v1/auth/logout | Invalidate refresh token | Yes |

#### Tenants (SuperAdmin only)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/tenants | List tenants (paginated) | SuperAdmin |
| POST | /api/v1/tenants | Create tenant | SuperAdmin |
| GET | /api/v1/tenants/{id} | Get tenant by ID | SuperAdmin |
| PUT | /api/v1/tenants/{id} | Update tenant | SuperAdmin |
| DELETE | /api/v1/tenants/{id} | Soft-delete tenant | SuperAdmin |

#### Users (TenantAdmin)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/users | List users in tenant | TenantAdmin |
| POST | /api/v1/users | Create user in tenant | TenantAdmin |
| GET | /api/v1/users/{id} | Get user | TenantAdmin |
| PUT | /api/v1/users/{id} | Update user | TenantAdmin |
| DELETE | /api/v1/users/{id} | Soft-delete user | TenantAdmin |

#### Agents (TenantAdmin)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/agents | List agent keys | TenantAdmin |
| POST | /api/v1/agents | Create agent key | TenantAdmin |
| GET | /api/v1/agents/{id} | Get agent key | TenantAdmin |
| PUT | /api/v1/agents/{id} | Update agent key | TenantAdmin |
| DELETE | /api/v1/agents/{id} | Revoke agent key | TenantAdmin |

#### Traffic (Analyst+)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/traffic/ingest | Ingest traffic flow (Agent API key auth) | AgentKey |
| POST | /api/v1/traffic/ingest/batch | Batch ingest | AgentKey |
| POST | /api/v1/traffic/pcap | Upload PCAP file | Analyst |
| GET | /api/v1/traffic | Query/filter traffic flows | Analyst |
| GET | /api/v1/traffic/{id} | Get flow detail | Analyst |
| GET | /api/v1/traffic/export | Export filtered flows (CSV) | Analyst |

Query parameters for GET /api/v1/traffic:
- `page`, `pageSize` — pagination
- `sourceIp`, `destinationIp` — IP filters
- `protocol` — protocol filter
- `startDate`, `endDate` — time range
- `sortBy`, `sortOrder` — sorting

#### Dashboard (Viewer+)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/dashboard/overview | Summary stats (total flows, bytes, active agents) | Viewer |
| GET | /api/v1/dashboard/top-talkers | Top source/destination IPs by volume | Viewer |
| GET | /api/v1/dashboard/protocol-distribution | Traffic by protocol | Viewer |
| GET | /api/v1/dashboard/bandwidth | Bandwidth over time (hourly/daily) | Viewer |

Query parameters: `period` (1h, 24h, 7d, 30d), `limit` (for top-N)

#### Retention Policies (TenantAdmin)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/retention | Get tenant retention policy | TenantAdmin |
| PUT | /api/v1/retention | Set/update retention policy | TenantAdmin |

#### Saved Filters (Analyst+)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/saved-filters | List user's saved filters | Analyst |
| POST | /api/v1/saved-filters | Save a filter | Analyst |
| GET | /api/v1/saved-filters/{id} | Get filter | Analyst |
| PUT | /api/v1/saved-filters/{id} | Update filter | Analyst |
| DELETE | /api/v1/saved-filters/{id} | Delete filter | Analyst |

## Folder Structure

```
VoidPulse/
├── .agentforge/
│   ├── architecture.md
│   ├── pipeline-status.md
│   └── review-report.md
├── src/
│   └── backend/
│       ├── VoidPulse.Api/
│       │   ├── Controllers/
│       │   │   ├── AuthController.cs
│       │   │   ├── TenantsController.cs
│       │   │   ├── UsersController.cs
│       │   │   ├── AgentsController.cs
│       │   │   ├── TrafficController.cs
│       │   │   ├── DashboardController.cs
│       │   │   ├── RetentionController.cs
│       │   │   ├── SavedFiltersController.cs
│       │   │   └── HealthController.cs
│       │   ├── Middleware/
│       │   │   ├── ExceptionMiddleware.cs
│       │   │   ├── CorrelationIdMiddleware.cs
│       │   │   └── TenantMiddleware.cs
│       │   ├── Extensions/
│       │   │   └── ServiceCollectionExtensions.cs
│       │   ├── Program.cs
│       │   ├── appsettings.json
│       │   ├── appsettings.Development.json
│       │   └── VoidPulse.Api.csproj
│       ├── VoidPulse.Domain/
│       │   ├── Entities/
│       │   │   ├── BaseEntity.cs
│       │   │   ├── Tenant.cs
│       │   │   ├── User.cs
│       │   │   ├── Role.cs
│       │   │   ├── UserRole.cs
│       │   │   ├── TrafficFlow.cs
│       │   │   ├── HttpMetadata.cs
│       │   │   ├── AgentKey.cs
│       │   │   ├── RetentionPolicy.cs
│       │   │   └── SavedFilter.cs
│       │   ├── Interfaces/
│       │   │   ├── IRepository.cs
│       │   │   ├── IUserRepository.cs
│       │   │   ├── ITenantRepository.cs
│       │   │   ├── ITrafficFlowRepository.cs
│       │   │   ├── IAgentKeyRepository.cs
│       │   │   ├── IRetentionPolicyRepository.cs
│       │   │   └── ISavedFilterRepository.cs
│       │   ├── Exceptions/
│       │   │   ├── DomainException.cs
│       │   │   ├── NotFoundException.cs
│       │   │   └── UnauthorizedException.cs
│       │   └── VoidPulse.Domain.csproj
│       ├── VoidPulse.Application/
│       │   ├── DTOs/
│       │   │   ├── Auth/
│       │   │   │   ├── LoginRequest.cs
│       │   │   │   ├── RegisterRequest.cs
│       │   │   │   ├── RefreshRequest.cs
│       │   │   │   └── AuthResponse.cs
│       │   │   ├── Tenants/
│       │   │   │   ├── CreateTenantRequest.cs
│       │   │   │   ├── UpdateTenantRequest.cs
│       │   │   │   └── TenantResponse.cs
│       │   │   ├── Users/
│       │   │   │   ├── CreateUserRequest.cs
│       │   │   │   ├── UpdateUserRequest.cs
│       │   │   │   └── UserResponse.cs
│       │   │   ├── Agents/
│       │   │   │   ├── CreateAgentKeyRequest.cs
│       │   │   │   ├── UpdateAgentKeyRequest.cs
│       │   │   │   └── AgentKeyResponse.cs
│       │   │   ├── Traffic/
│       │   │   │   ├── IngestTrafficRequest.cs
│       │   │   │   ├── TrafficQueryParams.cs
│       │   │   │   └── TrafficFlowResponse.cs
│       │   │   ├── Dashboard/
│       │   │   │   ├── OverviewResponse.cs
│       │   │   │   ├── TopTalkersResponse.cs
│       │   │   │   ├── ProtocolDistributionResponse.cs
│       │   │   │   └── BandwidthResponse.cs
│       │   │   ├── Retention/
│       │   │   │   ├── RetentionPolicyRequest.cs
│       │   │   │   └── RetentionPolicyResponse.cs
│       │   │   └── SavedFilters/
│       │   │       ├── CreateSavedFilterRequest.cs
│       │   │       ├── UpdateSavedFilterRequest.cs
│       │   │       └── SavedFilterResponse.cs
│       │   ├── Interfaces/
│       │   │   ├── IAuthService.cs
│       │   │   ├── ITenantService.cs
│       │   │   ├── IUserService.cs
│       │   │   ├── IAgentKeyService.cs
│       │   │   ├── ITrafficService.cs
│       │   │   ├── IDashboardService.cs
│       │   │   ├── IRetentionPolicyService.cs
│       │   │   ├── ISavedFilterService.cs
│       │   │   ├── IJwtService.cs
│       │   │   └── ICacheService.cs
│       │   ├── Services/
│       │   │   ├── AuthService.cs
│       │   │   ├── TenantService.cs
│       │   │   ├── UserService.cs
│       │   │   ├── AgentKeyService.cs
│       │   │   ├── TrafficService.cs
│       │   │   ├── DashboardService.cs
│       │   │   ├── RetentionPolicyService.cs
│       │   │   └── SavedFilterService.cs
│       │   ├── Validators/
│       │   │   ├── LoginRequestValidator.cs
│       │   │   ├── RegisterRequestValidator.cs
│       │   │   ├── CreateTenantRequestValidator.cs
│       │   │   ├── CreateUserRequestValidator.cs
│       │   │   ├── CreateAgentKeyRequestValidator.cs
│       │   │   ├── IngestTrafficRequestValidator.cs
│       │   │   └── CreateSavedFilterRequestValidator.cs
│       │   ├── Mappings/
│       │   │   └── MappingProfile.cs
│       │   ├── Common/
│       │   │   ├── PagedResult.cs
│       │   │   └── ApiResponse.cs
│       │   └── VoidPulse.Application.csproj
│       ├── VoidPulse.Infrastructure/
│       │   ├── Data/
│       │   │   ├── AppDbContext.cs
│       │   │   └── Configurations/
│       │   │       ├── TenantConfiguration.cs
│       │   │       ├── UserConfiguration.cs
│       │   │       ├── RoleConfiguration.cs
│       │   │       ├── UserRoleConfiguration.cs
│       │   │       ├── TrafficFlowConfiguration.cs
│       │   │       ├── HttpMetadataConfiguration.cs
│       │   │       ├── AgentKeyConfiguration.cs
│       │   │       ├── RetentionPolicyConfiguration.cs
│       │   │       └── SavedFilterConfiguration.cs
│       │   ├── Repositories/
│       │   │   ├── Repository.cs
│       │   │   ├── UserRepository.cs
│       │   │   ├── TenantRepository.cs
│       │   │   ├── TrafficFlowRepository.cs
│       │   │   ├── AgentKeyRepository.cs
│       │   │   ├── RetentionPolicyRepository.cs
│       │   │   └── SavedFilterRepository.cs
│       │   ├── Services/
│       │   │   ├── JwtService.cs
│       │   │   ├── PasswordHasher.cs
│       │   │   └── RedisCacheService.cs
│       │   └── VoidPulse.Infrastructure.csproj
│       ├── VoidPulse.Tests/
│       │   ├── Unit/
│       │   │   ├── Services/
│       │   │   │   ├── AuthServiceTests.cs
│       │   │   │   ├── TrafficServiceTests.cs
│       │   │   │   └── DashboardServiceTests.cs
│       │   │   └── Validators/
│       │   │       ├── LoginRequestValidatorTests.cs
│       │   │       └── RegisterRequestValidatorTests.cs
│       │   ├── Integration/
│       │   │   └── Controllers/
│       │   │       ├── AuthControllerTests.cs
│       │   │       ├── TrafficControllerTests.cs
│       │   │       └── HealthControllerTests.cs
│       │   ├── Helpers/
│       │   │   ├── TestFixture.cs
│       │   │   └── FakeDataGenerator.cs
│       │   └── VoidPulse.Tests.csproj
│       └── VoidPulse.sln
├── docker-compose.yml
├── Dockerfile
├── .dockerignore
├── .env.example
├── Makefile
├── .github/
│   └── workflows/
│       └── ci.yml
├── k8s/
│   ├── namespace.yaml
│   ├── backend/
│   │   ├── deployment.yaml
│   │   ├── service.yaml
│   │   └── hpa.yaml
│   ├── database/
│   │   ├── statefulset.yaml
│   │   ├── service.yaml
│   │   └── pvc.yaml
│   ├── redis/
│   │   ├── deployment.yaml
│   │   └── service.yaml
│   ├── ingress.yaml
│   ├── configmap.yaml
│   └── secrets.yaml
├── docs/
│   ├── api.md
│   ├── architecture.md
│   └── setup.md
├── README.md
└── CONTRIBUTING.md
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| DATABASE_URL | PostgreSQL connection string | postgresql://postgres:postgres@localhost:5432/voidpulse |
| REDIS_URL | Redis connection string | redis://localhost:6379 |
| JWT__Secret | JWT signing secret (min 32 chars) | (required) |
| JWT__AccessTokenExpiryMinutes | Access token TTL | 15 |
| JWT__RefreshTokenExpiryDays | Refresh token TTL | 7 |
| APP_PORT | API listen port | 8080 |
| ASPNETCORE_ENVIRONMENT | Environment name | Development |
| CORS__AllowedOrigins | Comma-separated origins | http://localhost:3000 |
| Logging__LogLevel__Default | Default log level | Information |

### Database
- PostgreSQL 16
- Connection pooling via Npgsql (default pool size 100)
- EF Core migrations applied on startup in Development, CLI in Production

### Redis
- Used for dashboard cache (TTL 60s), rate limiting counters, refresh token blacklist
- StackExchange.Redis client

### CORS
- Configurable allowed origins via environment variable
- Allow methods: GET, POST, PUT, DELETE, OPTIONS
- Allow headers: Authorization, Content-Type, X-Correlation-Id

### Logging
- Serilog with JSON structured output
- Correlation ID propagation via middleware
- Console sink (dev) + File sink (production)

## Non-Functional Requirements

### Performance
- Rate limiting: 100 req/min per IP for public endpoints, 1000 req/min for authenticated
- Agent ingest: 500 req/min per API key
- Pagination: default 20, max 100
- Dashboard cache: Redis TTL 60 seconds
- Database connection pool: 100 connections

### Security
- JWT with HMAC-SHA256 signing
- BCrypt password hashing (cost 12)
- Input validation at API boundary (FluentValidation)
- Parameterized queries via EF Core
- Soft delete for data retention compliance
- Multi-tenant data isolation via TenantId + global query filters
- API key authentication for agent ingestion (separate from JWT)
- Rate limiting to prevent abuse
- CORS restricted to configured origins
- Security headers: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection

### Scalability
- Stateless API (horizontal scaling via K8s HPA)
- Redis for shared cache across instances
- Database read replicas supported via connection string config
- Batch ingestion endpoint for high-throughput agent data
- Async processing for PCAP parsing

### Health Check
- GET /api/v1/health returns: API status, DB connectivity, Redis connectivity
- Used by Docker HEALTHCHECK and K8s readiness/liveness probes

### Graceful Shutdown
- Handle SIGTERM signal
- Drain in-flight requests (30s timeout)
- Close database connections
- Close Redis connections
