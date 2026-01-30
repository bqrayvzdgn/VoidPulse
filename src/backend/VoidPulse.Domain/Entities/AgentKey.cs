using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class AgentKey : BaseEntity
{
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ApiKey { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<TrafficFlow> TrafficFlows { get; set; } = new List<TrafficFlow>();
}
