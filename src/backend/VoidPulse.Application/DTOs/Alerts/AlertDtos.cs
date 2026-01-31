using VoidPulse.Domain.Entities;

namespace VoidPulse.Application.DTOs.Alerts;

public record AlertRuleResponse(
    Guid Id,
    string Name,
    string? Description,
    AlertCondition Condition,
    string ThresholdJson,
    AlertSeverity Severity,
    bool IsEnabled,
    DateTime CreatedAt);

public record CreateAlertRuleRequest(
    string Name,
    string? Description,
    AlertCondition Condition,
    string ThresholdJson,
    AlertSeverity Severity);

public record UpdateAlertRuleRequest(
    string? Name,
    string? Description,
    AlertCondition? Condition,
    string? ThresholdJson,
    AlertSeverity? Severity,
    bool? IsEnabled);

public record AlertResponse(
    Guid Id,
    Guid? AlertRuleId,
    string RuleName,
    string Message,
    AlertSeverity Severity,
    string? SourceIp,
    string? DestinationIp,
    string? MetadataJson,
    bool IsAcknowledged,
    DateTime TriggeredAt,
    DateTime? AcknowledgedAt);

public record AlertCountResponse(int UnacknowledgedCount);
