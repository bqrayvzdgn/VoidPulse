using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class Alert : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? AlertRuleId { get; set; }

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; }

    [MaxLength(45)]
    public string? SourceIp { get; set; }

    [MaxLength(45)]
    public string? DestinationIp { get; set; }

    [MaxLength(2000)]
    public string? MetadataJson { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public AlertRule? AlertRule { get; set; }
}
