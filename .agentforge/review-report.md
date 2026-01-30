# Code Review Report

## Summary
- **Total issues found**: 18
- **Critical**: 3 | **High**: 5 | **Medium**: 7 | **Low**: 3
- **Auto-fixed**: 11 | **Manual attention needed**: 7

## Security Issues

| # | Severity | File | Description | Status |
|---|----------|------|-------------|--------|
| S1 | High | `VoidPulse.Api/Extensions/ServiceCollectionExtensions.cs` | `RequireHttpsMetadata` was set to `false`, allowing JWT tokens to be transmitted over unencrypted connections. Changed to `true`. | **Fixed** |
| S2 | High | `VoidPulse.Api/Extensions/ServiceCollectionExtensions.cs` | JWT `ValidateIssuer` and `ValidateAudience` were set to `false`, weakening token validation. Enabled both with issuer/audience set to "VoidPulse" to match the token generation. | **Fixed** |
| S3 | High | `VoidPulse.Infrastructure/Services/JwtService.cs` | `ValidateRefreshToken` only checked for empty string, providing no real validation. Any non-empty string would pass. Now validates Base64 format and expected 64-byte length. | **Fixed** |
| S4 | Medium | `VoidPulse.Infrastructure/Services/PasswordHasher.cs` | `PasswordHasher` was a static class that could not implement `IPasswordHasher` interface, breaking DI and making it impossible to inject. Refactored to instance class `BcryptPasswordHasher` implementing `IPasswordHasher`. BCrypt work factor is correct at 12. | **Fixed** |
| S5 | Medium | `docker-compose.yml` | `JWT__Secret` in docker-compose uses a hardcoded development secret. This is acceptable for development but the compose file should never be used in production without overriding this value. | **Manual** |
| S6 | Medium | `k8s/secrets.yaml` | K8s secrets contain base64-encoded placeholder values (e.g., "changeme" passwords). Comments warn to replace them, but ideally secrets should be managed by an external secrets manager (Vault, AWS Secrets Manager, etc.) and not committed to the repo at all. | **Manual** |
| S7 | Low | `VoidPulse.Application/Services/AuthService.cs` | Login iterates all tenants to find a user by email (lines 78-90). This is a timing-based information leak -- the more tenants exist, the longer login takes, which could reveal tenant count. Should use a single query with email index. | **Manual** |
| S8 | Medium | `VoidPulse.Api/Controllers/TrafficController.cs` | The `ingest` and `ingest/batch` endpoints accept an `X-Api-Key` header but do not have `[Authorize]` and do not pass the API key to the service layer for validation. The controller was passing non-existent DTO types. Fixed DTO types but the API key flow needs architectural review -- the controller should resolve the tenant/agent from the API key before calling the service. | **Manual** |

## Performance Issues

| # | Severity | File | Description | Status |
|---|----------|------|-------------|--------|
| P1 | High | `VoidPulse.Application/Services/DashboardService.cs` | All dashboard methods fetch ALL traffic flows using `int.MaxValue` as page size (lines 35, 65, 93, 124). For a network traffic monitoring system this could be millions of rows loaded into memory. Dashboard aggregations should be pushed down to SQL queries (GROUP BY) or use materialized views/pre-aggregated tables. | **Manual** |
| P2 | High | `VoidPulse.Application/Services/AuthService.cs` | `LoginAsync` calls `_tenantRepository.GetAllAsync()` and iterates every tenant to find a user by email (lines 78-90). This is an N+1 query pattern. Should add a `GetByEmailAsync(email)` method to `IUserRepository` that searches across all tenants in a single query. | **Manual** |
| P3 | Medium | `VoidPulse.Application/Services/TrafficService.cs` | `ExportCsvAsync` hardcodes a limit of 10,000 rows (line 110). For large exports this builds the entire CSV in memory as a `StringBuilder` then converts to `byte[]`. Should use streaming (write to a `Stream` directly). | **Manual** |
| P4 | Low | `VoidPulse.Infrastructure/Repositories/TrafficFlowRepository.cs` | The `QueryAsync` method calls `CountAsync()` on the full filtered query (line 66) separately from the data fetch. This executes two database round-trips. Consider using a window function or parallel execution. | **Manual** |

## Code Quality Issues

| # | Severity | File | Description | Status |
|---|----------|------|-------------|--------|
| Q1 | Critical | All Controllers (`Auth`, `Tenants`, `Users`, `Agents`, `Traffic`, `Dashboard`, `Retention`, `SavedFilters`) | Controllers used non-existent DTO type names (`UserDto`, `AgentDto`, `TenantDto`, `AuthResponseDto`, `LoginRequestDto`, `RegisterRequestDto`, `RefreshTokenRequestDto`, `CreateTenantDto`, `UpdateTenantDto`, `CreateUserDto`, `UpdateUserDto`, `CreateAgentDto`, `UpdateAgentDto`, `TrafficRecordDto`, `TrafficIngestResultDto`, `TrafficBatchIngestResultDto`, `PcapUploadResultDto`, `DashboardOverviewDto`, `TopTalkerDto`, `ProtocolDistributionDto`, `BandwidthTimeSeriesDto`, `RetentionPolicyDto`, `UpdateRetentionPolicyDto`, `SavedFilterDto`, `CreateSavedFilterDto`, `UpdateSavedFilterDto`) instead of the actual Application layer DTO names (`UserResponse`, `AgentKeyResponse`, `TenantResponse`, `AuthResponse`, `LoginRequest`, `RegisterRequest`, `RefreshRequest`, `CreateTenantRequest`, `UpdateTenantRequest`, `CreateUserRequest`, `UpdateUserRequest`, `CreateAgentKeyRequest`, `UpdateAgentKeyRequest`, `IngestTrafficRequest`, `TrafficFlowResponse`, `OverviewResponse`, `TopTalkersResponse`, `ProtocolDistributionResponse`, `BandwidthResponse`, `RetentionPolicyResponse`, `RetentionPolicyRequest`, `SavedFilterResponse`, `CreateSavedFilterRequest`, `UpdateSavedFilterRequest`). All controllers used `using VoidPulse.Application.DTOs;` (non-existent namespace) instead of specific sub-namespaces. | **Fixed** |
| Q2 | Critical | All Controllers | All controllers called `ApiResponse<T>.Ok(result)` but the actual method is `ApiResponse<T>.Succeed(result)`. The `Ok` method does not exist on `ApiResponse<T>`. | **Fixed** |
| Q3 | Critical | `VoidPulse.Api/Extensions/ServiceCollectionExtensions.cs` | DI registrations referenced non-existent types: `IAgentService`/`AgentService` (should be `IAgentKeyService`/`AgentKeyService`), `IRetentionService`/`RetentionService` (should be `IRetentionPolicyService`/`RetentionPolicyService`), and several phantom repository interfaces (`IAgentRepository`, `ITrafficRecordRepository`, `IRefreshTokenRepository`). Also, `JwtService` was being constructed with `(string, int, int)` parameters but its actual constructor takes `IConfiguration`. Fixed all registrations to use correct types and standard DI resolution. | **Fixed** |
| Q4 | High | `VoidPulse.Api/Middleware/ExceptionMiddleware.cs` | Called `ApiResponse<object>.Fail(ex.Message)` with a single string argument but the actual signature is `Fail(string code, string message, List<FieldError>? details)`. Also referenced non-existent `ValidationError` type instead of `FieldError`. Fixed to use proper error codes and `FieldError` type. | **Fixed** |
| Q5 | Medium | `VoidPulse.Api/Controllers/UsersController.cs` | Argument order in service calls was swapped: controller passed `(tenantId, id)` but `IUserService.GetByIdAsync` expects `(id, tenantId)`. Same issue for `UpdateAsync` and `DeleteAsync`. Fixed argument order. | **Fixed** |
| Q6 | Medium | `VoidPulse.Api/Controllers/SavedFiltersController.cs` | Controller passed arguments as `(tenantId, userId, ...)` but `ISavedFilterService` expects `(userId, tenantId, ...)`. Fixed argument order to match interface signatures. | **Fixed** |

## Recommendations

### Immediate (should address before production)
1. **Traffic ingestion API key flow**: The `TrafficController.Ingest` and `IngestBatch` endpoints receive an API key header but currently pass `Guid.Empty` for tenantId and agentKeyId. Implement an `IAgentKeyService.ResolveByApiKeyAsync(string apiKey)` method that returns the tenant and agent key IDs, then pass those to the traffic service.
2. **Dashboard query performance**: Replace the in-memory aggregation approach in `DashboardService` with database-level aggregation queries. Add dedicated repository methods like `GetOverviewStatsAsync`, `GetTopTalkersAsync`, etc. that use SQL GROUP BY.
3. **Login tenant iteration**: Add a cross-tenant `GetByEmailAsync(string email)` to `IUserRepository` to avoid loading all tenants.

### Short-term improvements
4. **Rate limiting**: Add rate limiting middleware on auth endpoints (`/api/v1/auth/login`, `/api/v1/auth/register`) to prevent brute-force attacks. The K8s ingress has a basic rate limit annotation, but application-level rate limiting (e.g., `AspNetCoreRateLimit`) provides finer control.
5. **Security headers**: Add security headers middleware (X-Content-Type-Options, X-Frame-Options, Strict-Transport-Security, Content-Security-Policy).
6. **Input validation**: FluentValidation is registered but no validator classes were found in the codebase. Add validators for all request DTOs (e.g., email format, password strength, IP address format, port range 0-65535).
7. **K8s secrets management**: Move secrets out of the YAML files and into an external secrets manager. Remove `k8s/secrets.yaml` from version control.
8. **CSV export streaming**: Refactor `ExportCsvAsync` to write directly to a response stream rather than building the entire CSV in memory.

### Long-term improvements
9. **Database indexes**: The EF configurations have good indexes on `TrafficFlow` (TenantId, SourceIp+DestinationIp, StartedAt+EndedAt, Protocol). Consider adding a composite index on `(TenantId, StartedAt)` for the most common dashboard query pattern.
10. **API key hashing**: The `AgentKey.ApiKey` is stored in plaintext. Consider hashing it (like passwords) and only showing the full key on creation.
11. **Audit logging**: Add audit trail for sensitive operations (user creation/deletion, tenant management, retention policy changes).
12. **Health check depth**: The health endpoint returns a simple status. Consider adding deep health checks that verify database and Redis connectivity.
