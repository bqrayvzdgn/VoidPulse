#!/bin/bash
# VoidPulse API Endpoint Tests
# Usage: ./tests/test-endpoints.sh [BASE_URL]
# Default: http://localhost:8080

BASE_URL="${1:-http://localhost:8080}"
API="$BASE_URL/api/v1"
PASS=0
FAIL=0

green() { echo -e "\033[32m✓ $1\033[0m"; PASS=$((PASS+1)); }
red()   { echo -e "\033[31m✗ $1\033[0m"; FAIL=$((FAIL+1)); }

check_status() {
    local desc="$1" expected="$2" actual="$3"
    if [ "$actual" = "$expected" ]; then
        green "$desc (HTTP $actual)"
    else
        red "$desc (expected $expected, got $actual)"
    fi
}

check_header() {
    local desc="$1" header="$2" response="$3"
    if echo "$response" | grep -qi "$header"; then
        green "$desc"
    else
        red "$desc — header '$header' not found"
    fi
}

echo "=== VoidPulse API Endpoint Tests ==="
echo "Base URL: $API"
echo ""

# ─── Health Check ───
echo "--- Health Check ---"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API/health")
check_status "GET /health returns 200" "200" "$STATUS"

# ─── Security Headers ───
echo ""
echo "--- Security Headers ---"
HEADERS=$(curl -s -I "$API/health")
check_header "X-Content-Type-Options present" "X-Content-Type-Options: nosniff" "$HEADERS"
check_header "X-Frame-Options present" "X-Frame-Options: DENY" "$HEADERS"
check_header "X-XSS-Protection present" "X-XSS-Protection" "$HEADERS"
check_header "Referrer-Policy present" "Referrer-Policy" "$HEADERS"
check_header "Permissions-Policy present" "Permissions-Policy" "$HEADERS"

# ─── Auth: Register ───
echo ""
echo "--- Auth ---"
TIMESTAMP=$(date +%s)
REG_BODY=$(cat <<EOF
{
  "email": "test-${TIMESTAMP}@voidpulse.dev",
  "password": "TestPass123!",
  "fullName": "Test User",
  "tenantName": "Test Org ${TIMESTAMP}",
  "tenantSlug": "test-org-${TIMESTAMP}"
}
EOF
)
REG_RESP=$(curl -s -w "\n%{http_code}" -X POST "$API/auth/register" \
  -H "Content-Type: application/json" \
  -d "$REG_BODY")
REG_STATUS=$(echo "$REG_RESP" | tail -1)
REG_JSON=$(echo "$REG_RESP" | head -n -1)
check_status "POST /auth/register" "200" "$REG_STATUS"

ACCESS_TOKEN=$(echo "$REG_JSON" | grep -o '"accessToken":"[^"]*"' | head -1 | cut -d'"' -f4)
REFRESH_TOKEN=$(echo "$REG_JSON" | grep -o '"refreshToken":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -n "$ACCESS_TOKEN" ]; then
    green "Access token received"
else
    red "No access token in response"
fi

# ─── Auth: Login ───
LOGIN_BODY=$(cat <<EOF
{
  "email": "test-${TIMESTAMP}@voidpulse.dev",
  "password": "TestPass123!"
}
EOF
)
LOGIN_RESP=$(curl -s -w "\n%{http_code}" -X POST "$API/auth/login" \
  -H "Content-Type: application/json" \
  -d "$LOGIN_BODY")
LOGIN_STATUS=$(echo "$LOGIN_RESP" | tail -1)
check_status "POST /auth/login" "200" "$LOGIN_STATUS"

# ─── Auth: Login with wrong password ───
BAD_LOGIN_BODY='{"email":"test-'${TIMESTAMP}'@voidpulse.dev","password":"wrong"}'
BAD_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$API/auth/login" \
  -H "Content-Type: application/json" \
  -d "$BAD_LOGIN_BODY")
check_status "POST /auth/login (wrong password) returns 401" "401" "$BAD_STATUS"

# ─── Auth: Refresh ───
if [ -n "$REFRESH_TOKEN" ]; then
    REFRESH_RESP=$(curl -s -w "\n%{http_code}" -X POST "$API/auth/refresh" \
      -H "Content-Type: application/json" \
      -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}")
    REFRESH_STATUS=$(echo "$REFRESH_RESP" | tail -1)
    REFRESH_JSON=$(echo "$REFRESH_RESP" | head -n -1)
    check_status "POST /auth/refresh" "200" "$REFRESH_STATUS"
    # Update tokens
    ACCESS_TOKEN=$(echo "$REFRESH_JSON" | grep -o '"accessToken":"[^"]*"' | head -1 | cut -d'"' -f4)
fi

# ─── Traffic Ingest: No API Key ───
echo ""
echo "--- Traffic Ingestion (API Key Validation) ---"
NO_KEY_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$API/traffic/ingest" \
  -H "Content-Type: application/json" \
  -d '{"sourceIp":"10.0.0.1","destinationIp":"10.0.0.2","sourcePort":12345,"destinationPort":80,"protocol":"TCP","bytesSent":1024,"bytesReceived":2048,"packetsSent":10,"packetsReceived":20,"startedAt":"2024-01-01T00:00:00Z","endedAt":"2024-01-01T00:01:00Z"}')
check_status "POST /traffic/ingest (no API key) returns 401" "401" "$NO_KEY_STATUS"

# ─── Traffic Ingest: Invalid API Key ───
BAD_KEY_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$API/traffic/ingest" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: invalid-key-12345" \
  -d '{"sourceIp":"10.0.0.1","destinationIp":"10.0.0.2","sourcePort":12345,"destinationPort":80,"protocol":"TCP","bytesSent":1024,"bytesReceived":2048,"packetsSent":10,"packetsReceived":20,"startedAt":"2024-01-01T00:00:00Z","endedAt":"2024-01-01T00:01:00Z"}')
check_status "POST /traffic/ingest (invalid API key) returns 401" "401" "$BAD_KEY_STATUS"

# ─── Agent Keys (create one for testing traffic ingest) ───
echo ""
echo "--- Agent Keys ---"
if [ -n "$ACCESS_TOKEN" ]; then
    AGENT_RESP=$(curl -s -w "\n%{http_code}" -X POST "$API/agents" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -d '{"name":"Test Agent"}')
    AGENT_STATUS=$(echo "$AGENT_RESP" | tail -1)
    AGENT_JSON=$(echo "$AGENT_RESP" | head -n -1)
    check_status "POST /agents (create agent key)" "200" "$AGENT_STATUS"

    AGENT_API_KEY=$(echo "$AGENT_JSON" | grep -o '"apiKey":"[^"]*"' | head -1 | cut -d'"' -f4)

    # ─── Traffic Ingest: Valid API Key ───
    if [ -n "$AGENT_API_KEY" ]; then
        INGEST_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$API/traffic/ingest" \
          -H "Content-Type: application/json" \
          -H "X-Api-Key: $AGENT_API_KEY" \
          -d '{"sourceIp":"10.0.0.1","destinationIp":"10.0.0.2","sourcePort":12345,"destinationPort":80,"protocol":"TCP","bytesSent":1024,"bytesReceived":2048,"packetsSent":10,"packetsReceived":20,"startedAt":"2024-01-01T00:00:00Z","endedAt":"2024-01-01T00:01:00Z"}')
        check_status "POST /traffic/ingest (valid API key)" "200" "$INGEST_STATUS"
    else
        red "No agent API key received — skipping ingest test"
    fi
fi

# ─── Dashboard ───
echo ""
echo "--- Dashboard ---"
if [ -n "$ACCESS_TOKEN" ]; then
    for ENDPOINT in "overview?period=24h" "top-talkers?period=24h" "protocol-distribution?period=24h" "bandwidth?period=24h"; do
        DASH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API/dashboard/$ENDPOINT" \
          -H "Authorization: Bearer $ACCESS_TOKEN")
        check_status "GET /dashboard/$ENDPOINT" "200" "$DASH_STATUS"
    done
fi

# ─── Traffic Query ───
echo ""
echo "--- Traffic Query ---"
if [ -n "$ACCESS_TOKEN" ]; then
    QUERY_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API/traffic?page=1&pageSize=10" \
      -H "Authorization: Bearer $ACCESS_TOKEN")
    check_status "GET /traffic (query)" "200" "$QUERY_STATUS"

    EXPORT_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API/traffic/export" \
      -H "Authorization: Bearer $ACCESS_TOKEN")
    check_status "GET /traffic/export (CSV)" "200" "$EXPORT_STATUS"
fi

# ─── Protected endpoints without auth ───
echo ""
echo "--- Authorization ---"
NOAUTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API/traffic")
check_status "GET /traffic (no auth) returns 401" "401" "$NOAUTH_STATUS"

NOAUTH_USERS=$(curl -s -o /dev/null -w "%{http_code}" "$API/users")
check_status "GET /users (no auth) returns 401" "401" "$NOAUTH_USERS"

# ─── Auth: Logout ───
echo ""
echo "--- Logout ---"
if [ -n "$ACCESS_TOKEN" ]; then
    LOGOUT_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$API/auth/logout" \
      -H "Authorization: Bearer $ACCESS_TOKEN")
    check_status "DELETE /auth/logout" "200" "$LOGOUT_STATUS"
fi

# ─── Summary ───
echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="
[ "$FAIL" -eq 0 ] && exit 0 || exit 1
