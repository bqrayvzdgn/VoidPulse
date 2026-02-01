#!/usr/bin/env bash
set -euo pipefail

ENV_FILE=".env"

if [ -f "$ENV_FILE" ]; then
    echo "=== .env file already exists. Skipping generation. ==="
    echo "Delete .env and re-run this script to regenerate."
    exit 0
fi

echo "=== VoidPulse Setup ==="
echo "Generating secrets and configuration..."
echo ""

# Generate random values
POSTGRES_PASSWORD=$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9' | head -c 24)
JWT_SECRET=$(openssl rand -base64 64 | tr -dc 'A-Za-z0-9' | head -c 64)
ADMIN_PASSWORD=$(openssl rand -base64 18 | tr -dc 'A-Za-z0-9' | head -c 16)
AGENT_KEY="vp_$(openssl rand -base64 48 | tr '+/' '-_' | tr -d '=')"

cat > "$ENV_FILE" <<EOF
# ============================================
# VoidPulse Configuration (auto-generated)
# ============================================

# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
POSTGRES_DB=voidpulse

# JWT Authentication
JWT_SECRET=${JWT_SECRET}
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7

# Default Admin Account (created on first run)
VOIDPULSE_ADMIN_EMAIL=admin@voidpulse.local
VOIDPULSE_ADMIN_PASSWORD=${ADMIN_PASSWORD}
VOIDPULSE_ADMIN_FULLNAME=System Administrator

# Default Tenant
VOIDPULSE_DEFAULT_TENANT_NAME=Default
VOIDPULSE_DEFAULT_TENANT_SLUG=default

# Agent API Key (shared between backend seeder and agent)
VOIDPULSE_AGENT_API_KEY=${AGENT_KEY}

# CORS (use * to allow all origins, or comma-separated list)
CORS_ALLOWED_ORIGINS=*

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
EOF

echo "=== Configuration generated ==="
echo ""
echo "  Admin Email    : admin@voidpulse.local"
echo "  Admin Password : ${ADMIN_PASSWORD}"
echo ""
echo "  Credentials are saved in .env"
echo ""
echo "=== Next Steps ==="
echo "  docker compose up -d"
echo "  Open http://localhost:3000"
echo ""
