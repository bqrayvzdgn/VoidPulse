using System.Collections.Concurrent;
using System.Net;

namespace VoidPulse.Agent.Capture;

public class DnsResolver
{
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly ConcurrentQueue<DnsEntry> _newResolutions = new();

    public DnsResolver()
    {
        // Pre-populate from Windows DNS cache via PowerShell is not practical in C#.
        // We rely on reverse DNS and accumulate resolutions as traffic flows.
    }

    public string? Resolve(string ip)
    {
        if (_cache.TryGetValue(ip, out var hostname))
            return hostname;

        // Attempt reverse DNS (non-blocking best-effort)
        try
        {
            var entry = Dns.GetHostEntry(ip);
            if (!string.IsNullOrEmpty(entry.HostName) && entry.HostName != ip)
            {
                _cache.TryAdd(ip, entry.HostName);
                _newResolutions.Enqueue(new DnsEntry
                {
                    QueriedHostname = entry.HostName,
                    ResolvedIp = ip,
                    QueryType = "PTR",
                    Ttl = 300,
                    ResolvedAt = DateTime.UtcNow
                });
                return entry.HostName;
            }
        }
        catch
        {
            // Reverse DNS failed — not all IPs have PTR records
        }

        return null;
    }

    public void AddResolution(string ip, string hostname, string queryType = "A", int ttl = 300)
    {
        _cache.TryAdd(ip, hostname);
        _newResolutions.Enqueue(new DnsEntry
        {
            QueriedHostname = hostname,
            ResolvedIp = ip,
            QueryType = queryType,
            Ttl = ttl,
            ResolvedAt = DateTime.UtcNow
        });
    }

    public List<DnsEntry> FlushNewResolutions()
    {
        var entries = new List<DnsEntry>();
        while (_newResolutions.TryDequeue(out var entry))
        {
            entries.Add(entry);
        }
        return entries;
    }
}

public class DnsEntry
{
    public string QueriedHostname { get; set; } = string.Empty;
    public string ResolvedIp { get; set; } = string.Empty;
    public string QueryType { get; set; } = "A";
    public int Ttl { get; set; }
    public DateTime ResolvedAt { get; set; }
}
