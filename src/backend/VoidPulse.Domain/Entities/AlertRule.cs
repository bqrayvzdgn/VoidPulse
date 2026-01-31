using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class AlertRule : BaseEntity
{
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public AlertCondition Condition { get; set; }

    [MaxLength(1000)]
    public string ThresholdJson { get; set; } = "{}";

    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    public bool IsEnabled { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}

public enum AlertCondition
{
    ByteThreshold = 0,
    UnknownDestination = 1,
    PortScan = 2,
    ProtocolAnomaly = 3
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
