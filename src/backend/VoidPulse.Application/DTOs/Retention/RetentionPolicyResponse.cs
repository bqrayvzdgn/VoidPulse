namespace VoidPulse.Application.DTOs.Retention;

public record RetentionPolicyResponse(
    Guid Id,
    int RetentionDays,
    DateTime CreatedAt,
    DateTime UpdatedAt);
