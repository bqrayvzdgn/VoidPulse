using System.Collections.Concurrent;

namespace VoidPulse.Agent.Capture;

public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, FlowRecord> _activeFlows = new();
    private readonly int _idleTimeoutSeconds;

    public ConnectionTracker(int idleTimeoutSeconds = 30)
    {
        _idleTimeoutSeconds = idleTimeoutSeconds;
    }

    public void RecordSend(string srcIp, int srcPort, string dstIp, int dstPort, string protocol, int bytes, int pid)
    {
        var key = $"{srcIp}:{srcPort}->{dstIp}:{dstPort}/{protocol}";
        var now = DateTime.UtcNow;

        _activeFlows.AddOrUpdate(key,
            _ => new FlowRecord
            {
                SourceIp = srcIp,
                DestinationIp = dstIp,
                SourcePort = srcPort,
                DestinationPort = dstPort,
                Protocol = protocol,
                BytesSent = bytes,
                PacketsSent = 1,
                StartedAt = now,
                LastActivity = now,
                ProcessId = pid
            },
            (_, existing) =>
            {
                existing.BytesSent += bytes;
                existing.PacketsSent++;
                existing.LastActivity = now;
                if (pid > 0 && existing.ProcessId == 0) existing.ProcessId = pid;
                return existing;
            });
    }

    public void RecordReceive(string srcIp, int srcPort, string dstIp, int dstPort, string protocol, int bytes, int pid)
    {
        // For receive, the "flow" key is reversed: we track from local -> remote
        var key = $"{dstIp}:{dstPort}->{srcIp}:{srcPort}/{protocol}";
        var now = DateTime.UtcNow;

        _activeFlows.AddOrUpdate(key,
            _ => new FlowRecord
            {
                SourceIp = dstIp,
                DestinationIp = srcIp,
                SourcePort = dstPort,
                DestinationPort = srcPort,
                Protocol = protocol,
                BytesReceived = bytes,
                PacketsReceived = 1,
                StartedAt = now,
                LastActivity = now,
                ProcessId = pid
            },
            (_, existing) =>
            {
                existing.BytesReceived += bytes;
                existing.PacketsReceived++;
                existing.LastActivity = now;
                if (pid > 0 && existing.ProcessId == 0) existing.ProcessId = pid;
                return existing;
            });
    }

    public void RecordConnect(string srcIp, int srcPort, string dstIp, int dstPort, int pid)
    {
        var key = $"{srcIp}:{srcPort}->{dstIp}:{dstPort}/TCP";
        var now = DateTime.UtcNow;

        _activeFlows.TryAdd(key, new FlowRecord
        {
            SourceIp = srcIp,
            DestinationIp = dstIp,
            SourcePort = srcPort,
            DestinationPort = dstPort,
            Protocol = "TCP",
            StartedAt = now,
            LastActivity = now,
            ProcessId = pid
        });
    }

    public void RecordDisconnect(string srcIp, int srcPort, string dstIp, int dstPort)
    {
        var key = $"{srcIp}:{srcPort}->{dstIp}:{dstPort}/TCP";
        // Mark as completed by setting last activity far in the past so it gets flushed
        if (_activeFlows.TryGetValue(key, out var flow))
        {
            flow.LastActivity = DateTime.UtcNow.AddSeconds(-_idleTimeoutSeconds - 1);
        }
    }

    /// <summary>
    /// Flush flows that have been idle longer than the timeout.
    /// </summary>
    public List<FlowRecord> FlushCompletedFlows()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-_idleTimeoutSeconds);
        var flushed = new List<FlowRecord>();

        foreach (var kvp in _activeFlows)
        {
            if (kvp.Value.LastActivity < cutoff)
            {
                if (_activeFlows.TryRemove(kvp.Key, out var flow))
                {
                    if (flow.BytesSent > 0 || flow.BytesReceived > 0)
                        flushed.Add(flow);
                }
            }
        }

        return flushed;
    }

    /// <summary>
    /// Flush all flows regardless of idle state (for shutdown).
    /// </summary>
    public List<FlowRecord> FlushAllFlows()
    {
        var flushed = new List<FlowRecord>();
        foreach (var kvp in _activeFlows)
        {
            if (_activeFlows.TryRemove(kvp.Key, out var flow))
            {
                if (flow.BytesSent > 0 || flow.BytesReceived > 0)
                    flushed.Add(flow);
            }
        }
        return flushed;
    }

    public void EnrichProcessName(string key, string processName)
    {
        if (_activeFlows.TryGetValue(key, out var flow))
        {
            if (string.IsNullOrEmpty(flow.ProcessName))
                flow.ProcessName = processName;
        }
    }

    /// <summary>
    /// Track a captured packet as a flow (for Linux where ETW is unavailable).
    /// Merges bidirectional traffic into a single flow record.
    /// </summary>
    public void TrackPacket(PacketRecord packet)
    {
        var forwardKey = $"{packet.SourceIp}:{packet.SourcePort}->{packet.DestinationIp}:{packet.DestinationPort}/{packet.Protocol}";
        var reverseKey = $"{packet.DestinationIp}:{packet.DestinationPort}->{packet.SourceIp}:{packet.SourcePort}/{packet.Protocol}";
        var now = packet.Timestamp;
        var sni = packet.Info.Contains("SNI=") ? packet.Info.Split("SNI=").Last().Split(' ').First() : null;

        // Check if a flow already exists in the reverse direction (response packet)
        if (_activeFlows.TryGetValue(reverseKey, out var reverseFlow))
        {
            reverseFlow.BytesReceived += packet.Length;
            reverseFlow.PacketsReceived++;
            reverseFlow.LastActivity = now;
            if (!string.IsNullOrEmpty(sni) && string.IsNullOrEmpty(reverseFlow.TlsSni))
                reverseFlow.TlsSni = sni;
            return;
        }

        // Forward direction — create or update
        _activeFlows.AddOrUpdate(forwardKey,
            _ => new FlowRecord
            {
                SourceIp = packet.SourceIp,
                DestinationIp = packet.DestinationIp,
                SourcePort = packet.SourcePort,
                DestinationPort = packet.DestinationPort,
                Protocol = packet.Protocol,
                BytesSent = packet.Length,
                PacketsSent = 1,
                StartedAt = now,
                LastActivity = now,
                TlsSni = sni
            },
            (_, existing) =>
            {
                existing.BytesSent += packet.Length;
                existing.PacketsSent++;
                existing.LastActivity = now;
                if (!string.IsNullOrEmpty(sni) && string.IsNullOrEmpty(existing.TlsSni))
                    existing.TlsSni = sni;
                return existing;
            });
    }

    public int ActiveFlowCount => _activeFlows.Count;
}
