using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class TrafficFlow : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid AgentKeyId { get; set; }

    [MaxLength(45)]
    public string SourceIp { get; set; } = string.Empty;

    [MaxLength(45)]
    public string DestinationIp { get; set; } = string.Empty;

    public int SourcePort { get; set; }

    public int DestinationPort { get; set; }

    [MaxLength(10)]
    public string Protocol { get; set; } = string.Empty;

    public long BytesSent { get; set; }

    public long BytesReceived { get; set; }

    public int PacketsSent { get; set; }

    public int PacketsReceived { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime EndedAt { get; set; }

    public double FlowDuration { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public AgentKey AgentKey { get; set; } = null!;
    public HttpMetadata? HttpMetadata { get; set; }
}
