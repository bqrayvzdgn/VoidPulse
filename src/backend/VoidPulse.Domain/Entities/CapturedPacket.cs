using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class CapturedPacket : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid? TrafficFlowId { get; set; }

    public DateTime CapturedAt { get; set; }

    [MaxLength(45)]
    public string SourceIp { get; set; } = string.Empty;

    [MaxLength(45)]
    public string DestinationIp { get; set; } = string.Empty;

    public int SourcePort { get; set; }

    public int DestinationPort { get; set; }

    [MaxLength(10)]
    public string Protocol { get; set; } = string.Empty;

    public int PacketLength { get; set; }

    public byte[] HeaderBytes { get; set; } = [];

    public string ProtocolStack { get; set; } = "[]";

    [MaxLength(500)]
    public string Info { get; set; } = string.Empty;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public TrafficFlow? TrafficFlow { get; set; }
}
