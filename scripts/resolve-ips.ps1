param(
    [string]$ApiUrl = "http://localhost:8080",
    [string]$Email = "admin@voidpulse.local",
    [string]$Password = "Admin1234"
)

# Build DNS map from Windows DNS cache (IP -> hostname)
$dnsMap = @{}
try {
    $cache = Get-DnsClientCache -ErrorAction SilentlyContinue
    foreach ($entry in $cache) {
        if ($entry.Data -and $entry.Entry) {
            $dnsMap[$entry.Data] = $entry.Entry
        }
    }
} catch {}

# Login
$loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "$ApiUrl/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $auth.data.accessToken

# Get all traffic for 192.168.1.172
$traffic = Invoke-RestMethod -Uri "$ApiUrl/api/v1/traffic?sourceIp=192.168.1.172&pageSize=100" -Headers @{ Authorization = "Bearer $token" }

Write-Host ""
Write-Host "=== 192.168.1.172 Internet Akisi ===" -ForegroundColor Cyan
Write-Host ""
Write-Host ("{0,-22} {1,-6} {2,-45} {3,-12} {4,-12} {5}" -f "Hedef IP", "Port", "Site / Hostname", "Gonderilen", "Alinan", "Saat") -ForegroundColor Yellow
Write-Host ("-" * 120)

foreach ($flow in $traffic.data.items) {
    $ip = $flow.destinationIp
    $port = $flow.destinationPort

    # 1) DNS cache lookup
    $hostname = ""
    if ($dnsMap.ContainsKey($ip)) {
        $hostname = $dnsMap[$ip]
    }

    # 2) Fallback: reverse DNS
    if (-not $hostname) {
        try {
            $dns = [System.Net.Dns]::GetHostEntry($ip)
            $hostname = $dns.HostName
        } catch {
            $hostname = "(bilinmiyor)"
        }
    }

    # Service
    $service = switch ($port) {
        80   { "HTTP" }
        443  { "HTTPS" }
        53   { "DNS" }
        22   { "SSH" }
        3389 { "RDP" }
        default { "TCP:$port" }
    }

    # Format bytes
    $bytesSent = if ($flow.bytesSent -gt 1MB) { "{0:N1} MB" -f ($flow.bytesSent / 1MB) }
                 elseif ($flow.bytesSent -gt 1KB) { "{0:N1} KB" -f ($flow.bytesSent / 1KB) }
                 else { "$($flow.bytesSent) B" }

    $bytesRecv = if ($flow.bytesReceived -gt 1MB) { "{0:N1} MB" -f ($flow.bytesReceived / 1MB) }
                 elseif ($flow.bytesReceived -gt 1KB) { "{0:N1} KB" -f ($flow.bytesReceived / 1KB) }
                 else { "$($flow.bytesReceived) B" }

    $time = ([DateTime]$flow.startedAt).ToLocalTime().ToString("HH:mm:ss")

    # Color by site
    $color = switch -Regex ($hostname) {
        "claude|anthropic"          { "Magenta" }
        "google|youtube|goog"       { "Red" }
        "microsoft|azure|live|bing" { "Blue" }
        "cloudflare|cdn"            { "DarkYellow" }
        "amazon|aws|s3"             { "Yellow" }
        "riot|discord|steam|valve"  { "Green" }
        "facebook|meta|instagram"   { "Cyan" }
        "github|gitlab"             { "DarkCyan" }
        default                     { "White" }
    }

    Write-Host ("{0,-22} {1,-6} {2,-45} {3,-12} {4,-12} {5}" -f $ip, $service, $hostname, $bytesSent, $bytesRecv, $time) -ForegroundColor $color
}

Write-Host ""
Write-Host "Toplam: $($traffic.data.totalCount) flow" -ForegroundColor Gray
Write-Host ""
Write-Host "Not: DNS cache'teki hostname'ler gosterilir." -ForegroundColor DarkGray
Write-Host "     Daha fazla site gormek icin capture-traffic.ps1 calistirin." -ForegroundColor DarkGray
