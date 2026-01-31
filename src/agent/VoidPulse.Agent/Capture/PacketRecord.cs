namespace VoidPulse.Agent.Capture;

public class PacketRecord
{
    public DateTime Timestamp { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public int SourcePort { get; set; }
    public int DestinationPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int Length { get; set; }
    public byte[] HeaderBytes { get; set; } = [];
    public List<ProtocolLayer> ProtocolStack { get; set; } = [];
    public string Info { get; set; } = string.Empty;
}

public class ProtocolLayer
{
    public string Name { get; set; } = string.Empty;
    public int Offset { get; set; }
    public int Length { get; set; }
    public Dictionary<string, string> Fields { get; set; } = new();
}
