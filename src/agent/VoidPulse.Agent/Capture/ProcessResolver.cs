using System.Collections.Concurrent;
using System.Diagnostics;

namespace VoidPulse.Agent.Capture;

public class ProcessResolver
{
    private readonly ConcurrentDictionary<int, CacheEntry> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public string? Resolve(int processId)
    {
        if (processId <= 0) return null;

        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(processId, out var cached) && now - cached.ResolvedAt < CacheDuration)
            return cached.Name;

        try
        {
            var proc = Process.GetProcessById(processId);
            var name = proc.ProcessName;
            _cache[processId] = new CacheEntry(name, now);
            return name;
        }
        catch
        {
            // Process may have exited
            _cache.TryRemove(processId, out _);
            return null;
        }
    }

    private record CacheEntry(string? Name, DateTime ResolvedAt);
}
