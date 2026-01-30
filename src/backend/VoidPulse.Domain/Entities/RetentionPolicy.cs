namespace VoidPulse.Domain.Entities;

public class RetentionPolicy : BaseEntity
{
    public Guid TenantId { get; set; }

    public int RetentionDays { get; set; } = 90;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
