using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class Tenant : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<AgentKey> AgentKeys { get; set; } = new List<AgentKey>();
    public ICollection<TrafficFlow> TrafficFlows { get; set; } = new List<TrafficFlow>();
    public RetentionPolicy? RetentionPolicy { get; set; }
    public ICollection<SavedFilter> SavedFilters { get; set; } = new List<SavedFilter>();
}
