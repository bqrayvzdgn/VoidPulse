# VoidPulse API Reference

## Base URL

```
http://localhost:8080/api/v1
```

## Authentication

Most endpoints require a JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

Traffic ingestion endpoints use an API key in the `X-Api-Key` header instead:

```
X-Api-Key: <agent_api_key>
```

## Standard Response Format

All responses use the following envelope:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "meta": null
}
```

Paginated responses include a `meta` field:

```json
{
  "success": true,
  "data": {
    "items": [],
    "totalCount": 100,
    "page": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "error": null,
  "meta": null
}
```

### Error Response

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

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Request body validation failed |
| `UNAUTHORIZED` | 401 | Missing or invalid authentication |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `NOT_FOUND` | 404 | Resource not found |
| `DOMAIN_ERROR` | 400 | Business logic violation |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

## Roles

| Role | Level | Description |
|------|-------|-------------|
| SuperAdmin | Global | Cross-tenant management |
| TenantAdmin | Tenant | Manage users, agents, retention within a tenant |
| Analyst | Tenant | Query traffic, manage saved filters, export |
| Viewer | Tenant | View dashboard data |

Role hierarchy: SuperAdmin > TenantAdmin > Analyst > Viewer

---

## Health

### GET /api/v1/health

Service health check. No authentication required.

**Response**

```json
{
  "status": "healthy",
  "timestamp": "2026-01-30T12:00:00Z"
}
```

---

## Auth

### POST /api/v1/auth/register

Register a new user and tenant. The first user of the tenant becomes TenantAdmin.

**Auth**: None

**Request Body**

```json
{
  "email": "admin@example.com",
  "password": "SecureP@ss123",
  "fullName": "Jane Admin",
  "tenantName": "Acme Corp",
  "tenantSlug": "acme-corp"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | Yes | User email address |
| `password` | string | Yes | User password |
| `fullName` | string | Yes | User display name |
| `tenantName` | string | Yes | Organization name |
| `tenantSlug` | string | Yes | URL-safe tenant identifier |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "admin@example.com",
      "fullName": "Jane Admin",
      "roles": ["TenantAdmin"]
    }
  }
}
```

**Errors**

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Invalid email, weak password, or missing fields |
| `DOMAIN_ERROR` | Tenant slug already taken or email already registered |

---

### POST /api/v1/auth/login

Authenticate and receive a JWT token pair.

**Auth**: None

**Request Body**

```json
{
  "email": "admin@example.com",
  "password": "SecureP@ss123"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | Yes | User email |
| `password` | string | Yes | User password |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "admin@example.com",
      "fullName": "Jane Admin",
      "roles": ["TenantAdmin"]
    }
  }
}
```

**Errors**

| Code | Description |
|------|-------------|
| `UNAUTHORIZED` | Invalid email or password |
| `DOMAIN_ERROR` | Account is deactivated |

---

### POST /api/v1/auth/refresh

Refresh an expired access token using a valid refresh token.

**Auth**: None

**Request Body**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `refreshToken` | string | Yes | Previously issued refresh token |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "admin@example.com",
      "fullName": "Jane Admin",
      "roles": ["TenantAdmin"]
    }
  }
}
```

**Errors**

| Code | Description |
|------|-------------|
| `UNAUTHORIZED` | Invalid or expired refresh token |

---

### DELETE /api/v1/auth/logout

Invalidate the current refresh token.

**Auth**: Bearer token required

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": true
}
```

---

## Tenants

All tenant endpoints require **SuperAdmin** role.

### GET /api/v1/tenants

List all tenants with pagination.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page (max 100) |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "Acme Corp",
        "slug": "acme-corp",
        "isActive": true,
        "createdAt": "2026-01-15T10:00:00Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

---

### POST /api/v1/tenants

Create a new tenant.

**Request Body**

```json
{
  "name": "New Organization",
  "slug": "new-org"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Tenant display name (max 200) |
| `slug` | string | Yes | URL-safe identifier (max 100, unique) |

**Success Response** `201 Created`

```json
{
  "success": true,
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "name": "New Organization",
    "slug": "new-org",
    "isActive": true,
    "createdAt": "2026-01-30T12:00:00Z"
  }
}
```

---

### GET /api/v1/tenants/{id}

Get a single tenant by ID.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Tenant ID |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Acme Corp",
    "slug": "acme-corp",
    "isActive": true,
    "createdAt": "2026-01-15T10:00:00Z"
  }
}
```

**Errors**

| Code | Description |
|------|-------------|
| `NOT_FOUND` | Tenant with given ID does not exist |

---

### PUT /api/v1/tenants/{id}

Update a tenant.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Tenant ID |

**Request Body**

```json
{
  "name": "Acme Corporation",
  "isActive": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Updated name |
| `isActive` | bool | Yes | Active status |

**Success Response** `200 OK`

Returns the updated tenant object (same shape as GET).

---

### DELETE /api/v1/tenants/{id}

Soft-delete a tenant.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Tenant ID |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": true
}
```

---

## Users

All user endpoints require **TenantAdmin** role. Users are scoped to the authenticated user's tenant.

### GET /api/v1/users

List users in the current tenant.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page (max 100) |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "email": "analyst@example.com",
        "fullName": "John Analyst",
        "isActive": true,
        "roles": ["Analyst"],
        "lastLoginAt": "2026-01-29T15:30:00Z",
        "createdAt": "2026-01-15T10:00:00Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

---

### POST /api/v1/users

Create a new user in the current tenant.

**Request Body**

```json
{
  "email": "analyst@example.com",
  "password": "SecureP@ss123",
  "fullName": "John Analyst",
  "roles": ["Analyst"]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | Yes | User email (unique per tenant, max 256) |
| `password` | string | Yes | User password |
| `fullName` | string | Yes | Display name (max 200) |
| `roles` | string[] | Yes | Role names: `TenantAdmin`, `Analyst`, `Viewer` |

**Success Response** `201 Created`

Returns the created user object.

---

### GET /api/v1/users/{id}

Get a user by ID within the current tenant.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | User ID |

**Success Response** `200 OK`

Returns the user object.

**Errors**

| Code | Description |
|------|-------------|
| `NOT_FOUND` | User not found or belongs to a different tenant |

---

### PUT /api/v1/users/{id}

Update a user within the current tenant.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | User ID |

**Request Body**: Same fields as create (email, fullName, roles). Password optional for updates.

**Success Response** `200 OK`

Returns the updated user object.

---

### DELETE /api/v1/users/{id}

Soft-delete a user within the current tenant.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | User ID |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": true
}
```

---

## Agent Keys

All agent key endpoints require **TenantAdmin** role. Agent keys are scoped to the authenticated user's tenant.

### GET /api/v1/agents

List all agent keys for the current tenant.

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "name": "Production Agent",
      "apiKey": null,
      "isActive": true,
      "lastUsedAt": "2026-01-30T11:00:00Z",
      "createdAt": "2026-01-15T10:00:00Z"
    }
  ]
}
```

Note: The `apiKey` field is only returned when the key is first created.

---

### POST /api/v1/agents

Create a new agent key.

**Request Body**

```json
{
  "name": "Production Agent"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Agent key name (max 200) |

**Success Response** `201 Created`

```json
{
  "success": true,
  "data": {
    "id": "880e8400-e29b-41d4-a716-446655440003",
    "name": "Production Agent",
    "apiKey": "vp_a1b2c3d4e5f6...",
    "isActive": true,
    "lastUsedAt": null,
    "createdAt": "2026-01-30T12:00:00Z"
  }
}
```

**Important**: The `apiKey` value is only shown once at creation time. Store it securely.

---

### GET /api/v1/agents/{id}

Get an agent key by ID.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Agent key ID |

**Success Response** `200 OK`

Returns the agent key object (without the full `apiKey` value).

---

### PUT /api/v1/agents/{id}

Update an agent key.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Agent key ID |

**Request Body**: Fields to update (name, isActive).

**Success Response** `200 OK`

Returns the updated agent key object.

---

### DELETE /api/v1/agents/{id}

Revoke (soft-delete) an agent key. Traffic ingestion using this key will stop working.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Agent key ID |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": true
}
```

---

## Traffic

### POST /api/v1/traffic/ingest

Ingest a single traffic flow record. Used by network agents.

**Auth**: `X-Api-Key` header with a valid agent API key.

**Request Body**

```json
{
  "sourceIp": "192.168.1.100",
  "destinationIp": "10.0.0.50",
  "sourcePort": 54321,
  "destinationPort": 443,
  "protocol": "TCP",
  "bytesSent": 1024,
  "bytesReceived": 2048,
  "packetsSent": 10,
  "packetsReceived": 15,
  "startedAt": "2026-01-30T11:00:00Z",
  "endedAt": "2026-01-30T11:00:05Z",
  "httpMetadata": {
    "method": "GET",
    "host": "api.example.com",
    "path": "/v1/data",
    "statusCode": 200,
    "userAgent": "curl/7.88",
    "contentType": "application/json",
    "responseTimeMs": 125.5
  }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sourceIp` | string | Yes | Source IP address (max 45, supports IPv6) |
| `destinationIp` | string | Yes | Destination IP address (max 45) |
| `sourcePort` | int | Yes | Source port (0-65535) |
| `destinationPort` | int | Yes | Destination port (0-65535) |
| `protocol` | string | Yes | Protocol: TCP, UDP, ICMP (max 10) |
| `bytesSent` | long | Yes | Bytes sent |
| `bytesReceived` | long | Yes | Bytes received |
| `packetsSent` | int | Yes | Packets sent |
| `packetsReceived` | int | Yes | Packets received |
| `startedAt` | DateTime | Yes | Flow start time (UTC) |
| `endedAt` | DateTime | Yes | Flow end time (UTC) |
| `httpMetadata` | object | No | Optional HTTP-layer metadata |

**HttpMetadata Fields**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `method` | string | Yes | HTTP method (max 10) |
| `host` | string | Yes | Request host (max 500) |
| `path` | string | Yes | Request path (max 2000) |
| `statusCode` | int | Yes | HTTP status code |
| `userAgent` | string | No | User-Agent header (max 1000) |
| `contentType` | string | No | Content-Type header (max 200) |
| `responseTimeMs` | double | Yes | Response time in milliseconds |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "990e8400-e29b-41d4-a716-446655440004",
    "sourceIp": "192.168.1.100",
    "destinationIp": "10.0.0.50",
    "sourcePort": 54321,
    "destinationPort": 443,
    "protocol": "TCP",
    "bytesSent": 1024,
    "bytesReceived": 2048,
    "packetsSent": 10,
    "packetsReceived": 15,
    "startedAt": "2026-01-30T11:00:00Z",
    "endedAt": "2026-01-30T11:00:05Z",
    "flowDuration": 5.0,
    "httpMetadata": {
      "method": "GET",
      "host": "api.example.com",
      "path": "/v1/data",
      "statusCode": 200,
      "userAgent": "curl/7.88",
      "contentType": "application/json",
      "responseTimeMs": 125.5
    },
    "createdAt": "2026-01-30T11:00:06Z"
  }
}
```

---

### POST /api/v1/traffic/ingest/batch

Ingest multiple traffic flow records in a single request.

**Auth**: `X-Api-Key` header

**Request Body**: Array of `IngestTrafficRequest` objects (same schema as single ingest).

```json
[
  {
    "sourceIp": "192.168.1.100",
    "destinationIp": "10.0.0.50",
    "sourcePort": 54321,
    "destinationPort": 443,
    "protocol": "TCP",
    "bytesSent": 1024,
    "bytesReceived": 2048,
    "packetsSent": 10,
    "packetsReceived": 15,
    "startedAt": "2026-01-30T11:00:00Z",
    "endedAt": "2026-01-30T11:00:05Z",
    "httpMetadata": null
  }
]
```

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": [
    {
      "id": "...",
      "sourceIp": "192.168.1.100",
      "...": "..."
    }
  ]
}
```

---

### GET /api/v1/traffic

Query and filter traffic flows with pagination.

**Auth**: Analyst role required

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `sourceIp` | string | null | Filter by source IP |
| `destinationIp` | string | null | Filter by destination IP |
| `protocol` | string | null | Filter by protocol (TCP, UDP, ICMP) |
| `startDate` | DateTime | null | Filter flows starting after this time |
| `endDate` | DateTime | null | Filter flows ending before this time |
| `sortBy` | string | null | Sort field (e.g., `startedAt`, `bytesSent`) |
| `sortOrder` | string | null | `asc` or `desc` |
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page (max 100) |

**Example Request**

```
GET /api/v1/traffic?sourceIp=192.168.1.100&protocol=TCP&startDate=2026-01-29&page=1&pageSize=50
```

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "990e8400-e29b-41d4-a716-446655440004",
        "sourceIp": "192.168.1.100",
        "destinationIp": "10.0.0.50",
        "sourcePort": 54321,
        "destinationPort": 443,
        "protocol": "TCP",
        "bytesSent": 1024,
        "bytesReceived": 2048,
        "packetsSent": 10,
        "packetsReceived": 15,
        "startedAt": "2026-01-30T11:00:00Z",
        "endedAt": "2026-01-30T11:00:05Z",
        "flowDuration": 5.0,
        "httpMetadata": null,
        "createdAt": "2026-01-30T11:00:06Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 50,
    "totalPages": 1
  }
}
```

---

### GET /api/v1/traffic/{id}

Get a single traffic flow by ID.

**Auth**: Analyst role required

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Traffic flow ID |

**Success Response** `200 OK`

Returns a single `TrafficFlowResponse` object.

**Errors**

| Code | Description |
|------|-------------|
| `NOT_FOUND` | Traffic flow not found or belongs to a different tenant |

---

### GET /api/v1/traffic/export

Export filtered traffic flows as a CSV file.

**Auth**: Analyst role required

**Query Parameters**: Same as `GET /api/v1/traffic` (filters apply to the export).

**Response**: `200 OK` with `Content-Type: text/csv`

The response is a downloadable CSV file with the filename `traffic-export-{timestamp}.csv`.

---

## Dashboard

All dashboard endpoints require authentication (Viewer role or above). Data is scoped to the current tenant.

### GET /api/v1/dashboard/overview

Get summary statistics for the tenant.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `period` | string | `24h` | Time period: `1h`, `24h`, `7d`, `30d` |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "totalFlows": 152340,
    "totalBytes": 8573920000,
    "activeAgents": 5,
    "uniqueSourceIps": 342,
    "uniqueDestinationIps": 1205
  }
}
```

---

### GET /api/v1/dashboard/top-talkers

Get the top source/destination IPs by traffic volume.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `period` | string | `24h` | Time period: `1h`, `24h`, `7d`, `30d` |
| `limit` | int | 10 | Number of top entries to return |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "entries": [
      {
        "ip": "192.168.1.100",
        "totalBytes": 1234567890,
        "flowCount": 5432
      },
      {
        "ip": "10.0.0.50",
        "totalBytes": 987654321,
        "flowCount": 3210
      }
    ]
  }
}
```

---

### GET /api/v1/dashboard/protocol-distribution

Get traffic distribution by protocol.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `period` | string | `24h` | Time period: `1h`, `24h`, `7d`, `30d` |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "entries": [
      {
        "protocol": "TCP",
        "totalBytes": 7500000000,
        "flowCount": 120000,
        "percentage": 87.5
      },
      {
        "protocol": "UDP",
        "totalBytes": 900000000,
        "flowCount": 28000,
        "percentage": 10.5
      },
      {
        "protocol": "ICMP",
        "totalBytes": 173920000,
        "flowCount": 4340,
        "percentage": 2.0
      }
    ]
  }
}
```

---

### GET /api/v1/dashboard/bandwidth

Get bandwidth usage over time (hourly or daily buckets).

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `period` | string | `24h` | Time period: `1h`, `24h`, `7d`, `30d` |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "entries": [
      {
        "timestamp": "2026-01-30T00:00:00Z",
        "bytesSent": 500000000,
        "bytesReceived": 750000000,
        "totalBytes": 1250000000
      },
      {
        "timestamp": "2026-01-30T01:00:00Z",
        "bytesSent": 450000000,
        "bytesReceived": 680000000,
        "totalBytes": 1130000000
      }
    ]
  }
}
```

---

## Retention Policy

Retention policy endpoints require **TenantAdmin** role. Each tenant has one retention policy.

### GET /api/v1/retention

Get the retention policy for the current tenant.

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "aa0e8400-e29b-41d4-a716-446655440005",
    "retentionDays": 90,
    "createdAt": "2026-01-15T10:00:00Z",
    "updatedAt": "2026-01-20T14:00:00Z"
  }
}
```

---

### PUT /api/v1/retention

Set or update the retention policy for the current tenant.

**Request Body**

```json
{
  "retentionDays": 60
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `retentionDays` | int | Yes | Number of days to retain traffic data |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "aa0e8400-e29b-41d4-a716-446655440005",
    "retentionDays": 60,
    "createdAt": "2026-01-15T10:00:00Z",
    "updatedAt": "2026-01-30T12:00:00Z"
  }
}
```

---

## Saved Filters

Saved filter endpoints require **Analyst** role or above. Filters are scoped to the authenticated user within their tenant.

### GET /api/v1/saved-filters

List all saved filters for the current user.

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": [
    {
      "id": "bb0e8400-e29b-41d4-a716-446655440006",
      "name": "Production TCP Traffic",
      "filterJson": "{\"protocol\":\"TCP\",\"sourceIp\":\"10.0.0.*\"}",
      "createdAt": "2026-01-20T10:00:00Z",
      "updatedAt": "2026-01-25T14:00:00Z"
    }
  ]
}
```

---

### POST /api/v1/saved-filters

Save a new filter.

**Request Body**

```json
{
  "name": "Production TCP Traffic",
  "filterJson": "{\"protocol\":\"TCP\",\"sourceIp\":\"10.0.0.*\"}"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Filter name (max 200) |
| `filterJson` | string | Yes | JSON string of filter criteria |

**Success Response** `201 Created`

Returns the created saved filter object.

---

### GET /api/v1/saved-filters/{id}

Get a saved filter by ID.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Saved filter ID |

**Success Response** `200 OK`

Returns the saved filter object.

**Errors**

| Code | Description |
|------|-------------|
| `NOT_FOUND` | Filter not found or belongs to a different user/tenant |

---

### PUT /api/v1/saved-filters/{id}

Update a saved filter.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Saved filter ID |

**Request Body**: Same fields as create.

**Success Response** `200 OK`

Returns the updated saved filter object.

---

### DELETE /api/v1/saved-filters/{id}

Delete a saved filter.

**Path Parameters**

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid | Saved filter ID |

**Success Response** `200 OK`

```json
{
  "success": true,
  "data": true
}
```
