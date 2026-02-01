$ErrorActionPreference = "Stop"

$envFile = ".env"

if (Test-Path $envFile) {
    Write-Host "=== .env file already exists. Skipping generation. ===" -ForegroundColor Yellow
    Write-Host "Delete .env and re-run this script to regenerate."
    exit 0
}

Write-Host "=== VoidPulse Setup ===" -ForegroundColor Cyan
Write-Host "Generating secrets and configuration..."
Write-Host ""

# Generate random values
function Get-RandomString([int]$length) {
    $bytes = New-Object byte[] $length
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    $result = -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
    return $result
}

function Get-AgentKey {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $b64 = [Convert]::ToBase64String($bytes).Replace("+", "-").Replace("/", "_").TrimEnd("=")
    return "vp_$b64"
}

$pgPassword = Get-RandomString 24
$jwtSecret = Get-RandomString 64
$adminPassword = Get-RandomString 16
$agentKey = Get-AgentKey

$content = @"
# ============================================
# VoidPulse Configuration (auto-generated)
# ============================================

# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$pgPassword
POSTGRES_DB=voidpulse

# JWT Authentication
JWT_SECRET=$jwtSecret
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7

# Default Admin Account (created on first run)
VOIDPULSE_ADMIN_EMAIL=admin@voidpulse.local
VOIDPULSE_ADMIN_PASSWORD=$adminPassword
VOIDPULSE_ADMIN_FULLNAME=System Administrator

# Default Tenant
VOIDPULSE_DEFAULT_TENANT_NAME=Default
VOIDPULSE_DEFAULT_TENANT_SLUG=default

# Agent API Key (shared between backend seeder and agent)
VOIDPULSE_AGENT_API_KEY=$agentKey

# CORS (use * to allow all origins, or comma-separated list)
CORS_ALLOWED_ORIGINS=*

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
"@

Set-Content -Path $envFile -Value $content -Encoding UTF8

Write-Host "=== Configuration generated ===" -ForegroundColor Green
Write-Host ""
Write-Host "  Admin Email    : admin@voidpulse.local"
Write-Host "  Admin Password : $adminPassword" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Credentials are saved in .env"
Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "  docker compose up -d"
Write-Host "  Open http://localhost:3000"
Write-Host ""
