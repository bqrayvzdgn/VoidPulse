using System.ComponentModel.DataAnnotations;

namespace VoidPulse.Domain.Entities;

public class HttpMetadata : BaseEntity
{
    public Guid TrafficFlowId { get; set; }

    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Host { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    [MaxLength(1000)]
    public string? UserAgent { get; set; }

    [MaxLength(200)]
    public string? ContentType { get; set; }

    public double ResponseTimeMs { get; set; }

    // Navigation properties
    public TrafficFlow TrafficFlow { get; set; } = null!;
}
