namespace VoidPulse.Agent.Capture;

public class FlowRecord
{
    public string SourceIp { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public int SourcePort { get; set; }
    public int DestinationPort { get; set; }
    public string Protocol { get; set; } = "TCP";
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public int PacketsSent { get; set; }
    public int PacketsReceived { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public string? ProcessName { get; set; }
    public int ProcessId { get; set; }
    public string? Hostname { get; set; }
    public string? TlsSni { get; set; }

    public string Key => $"{SourceIp}:{SourcePort}->{DestinationIp}:{DestinationPort}/{Protocol}";
}
