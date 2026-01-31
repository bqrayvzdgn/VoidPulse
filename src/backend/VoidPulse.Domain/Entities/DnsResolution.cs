using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class DnsResolution : BaseEntity
{
    public Guid TenantId { get; set; }

    [MaxLength(512)]
    public string QueriedHostname { get; set; } = string.Empty;

    [MaxLength(45)]
    public string ResolvedIp { get; set; } = string.Empty;

    [MaxLength(10)]
    public string QueryType { get; set; } = "A";

    public int Ttl { get; set; }

    [MaxLength(45)]
    public string? ClientIp { get; set; }

    public DateTime ResolvedAt { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
