using System.Collections.Concurrent;
using System.Globalization;
using System.Net;

namespace VoidPulse.Agent.Capture;

/// <summary>
/// Resolves process names from network connections on Linux by reading /proc/net/tcp and /proc/{pid}/comm.
/// Only works when running as root with access to /proc.
/// </summary>
public class LinuxProcessResolver
{
    private readonly ConcurrentDictionary<long, string> _inodeToProcess = new();
    private readonly ConcurrentDictionary<string, (string ProcessName, DateTime CachedAt)> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private DateTime _lastProcScan = DateTime.MinValue;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);

    public string? Resolve(string srcIp, int srcPort, string dstIp, int dstPort, string protocol)
    {
        var key = $"{srcIp}:{srcPort}->{dstIp}:{dstPort}/{protocol}";
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(key, out var cached) && now - cached.CachedAt < CacheDuration)
            return cached.ProcessName;

        // Refresh /proc scan if stale
        if (now - _lastProcScan > ScanInterval)
        {
            ScanProcNet();
            _lastProcScan = now;
        }

        // Look up the local endpoint's inode
        var inode = FindInode(srcIp, srcPort, protocol);
        if (inode <= 0)
            inode = FindInode(dstIp, dstPort, protocol);

        if (inode <= 0) return null;

        if (_inodeToProcess.TryGetValue(inode, out var processName))
        {
            _cache[key] = (processName, now);
            return processName;
        }

        // Scan /proc/{pid}/fd to find which PID owns this inode
        processName = FindProcessByInode(inode);
        if (processName is not null)
        {
            _inodeToProcess[inode] = processName;
            _cache[key] = (processName, now);
        }

        return processName;
    }

    private readonly ConcurrentDictionary<string, long> _endpointToInode = new();

    private void ScanProcNet()
    {
        _endpointToInode.Clear();
        ParseProcNetFile("/proc/net/tcp");
        ParseProcNetFile("/proc/net/tcp6");
        ParseProcNetFile("/proc/net/udp");
        ParseProcNetFile("/proc/net/udp6");
    }

    private void ParseProcNetFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path);

            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 10) continue;

                // local_address format: AABBCCDD:PORT (hex)
                var localAddr = parts[1];
                var inode = long.TryParse(parts[9], out var ino) ? ino : 0;
                if (inode <= 0) continue;

                var (ip, port) = ParseHexEndpoint(localAddr);
                if (ip is not null)
                {
                    var endpointKey = $"{ip}:{port}";
                    _endpointToInode[endpointKey] = inode;
                }
            }
        }
        catch
        {
            // Best effort
        }
    }

    private static (string? Ip, int Port) ParseHexEndpoint(string hexEndpoint)
    {
        try
        {
            var colonIdx = hexEndpoint.IndexOf(':');
            if (colonIdx < 0) return (null, 0);

            var hexIp = hexEndpoint[..colonIdx];
            var hexPort = hexEndpoint[(colonIdx + 1)..];
            var port = int.Parse(hexPort, NumberStyles.HexNumber);

            string ip;
            if (hexIp.Length == 8)
            {
                // IPv4: stored as little-endian hex
                var ipNum = uint.Parse(hexIp, NumberStyles.HexNumber);
                ip = new IPAddress(ipNum).ToString();
            }
            else if (hexIp.Length == 32)
            {
                // IPv6
                var bytes = new byte[16];
                for (int j = 0; j < 4; j++)
                {
                    var chunk = hexIp.Substring(j * 8, 8);
                    var val = uint.Parse(chunk, NumberStyles.HexNumber);
                    bytes[j * 4] = (byte)(val & 0xFF);
                    bytes[j * 4 + 1] = (byte)((val >> 8) & 0xFF);
                    bytes[j * 4 + 2] = (byte)((val >> 16) & 0xFF);
                    bytes[j * 4 + 3] = (byte)((val >> 24) & 0xFF);
                }
                ip = new IPAddress(bytes).ToString();
            }
            else
            {
                return (null, 0);
            }

            return (ip, port);
        }
        catch
        {
            return (null, 0);
        }
    }

    private long FindInode(string ip, int port, string protocol)
    {
        var key = $"{ip}:{port}";
        if (_endpointToInode.TryGetValue(key, out var inode))
            return inode;

        // Try 0.0.0.0 (listening on all interfaces)
        var wildcard = $"0.0.0.0:{port}";
        if (_endpointToInode.TryGetValue(wildcard, out inode))
            return inode;

        // Try :: (IPv6 wildcard)
        wildcard = $":::{port}";
        if (_endpointToInode.TryGetValue(wildcard, out inode))
            return inode;

        return 0;
    }

    private static string? FindProcessByInode(long targetInode)
    {
        try
        {
            var procDirs = Directory.GetDirectories("/proc");
            foreach (var procDir in procDirs)
            {
                var pidStr = Path.GetFileName(procDir);
                if (!int.TryParse(pidStr, out _)) continue;

                var fdDir = Path.Combine(procDir, "fd");
                if (!Directory.Exists(fdDir)) continue;

                try
                {
                    foreach (var fdPath in Directory.GetFiles(fdDir))
                    {
                        try
                        {
                            var link = File.ResolveLinkTarget(fdPath, false)?.ToString() ?? "";
                            if (link.Contains($"socket:[{targetInode}]"))
                            {
                                // Read process name from /proc/{pid}/comm
                                var commPath = Path.Combine(procDir, "comm");
                                if (File.Exists(commPath))
                                    return File.ReadAllText(commPath).Trim();
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }

        return null;
    }
}
