using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Alerts;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Application.Interfaces;

public interface IAlertService
{
    // Rules
    Task<IReadOnlyList<AlertRuleResponse>> GetRulesAsync(Guid tenantId);
    Task<AlertRuleResponse> CreateRuleAsync(Guid tenantId, CreateAlertRuleRequest request);
    Task<AlertRuleResponse> UpdateRuleAsync(Guid tenantId, Guid ruleId, UpdateAlertRuleRequest request);
    Task DeleteRuleAsync(Guid tenantId, Guid ruleId);

    // Alerts
    Task<PagedResult<AlertResponse>> GetAlertsAsync(Guid tenantId, bool? isAcknowledged, AlertSeverity? severity, int page, int pageSize);
    Task<AlertCountResponse> GetUnacknowledgedCountAsync(Guid tenantId);
    Task AcknowledgeAlertAsync(Guid tenantId, Guid alertId);
}
