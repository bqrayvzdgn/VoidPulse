using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class SavedFilter : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string FilterJson { get; set; } = string.Empty;

    // Navigation properties
    public User User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
