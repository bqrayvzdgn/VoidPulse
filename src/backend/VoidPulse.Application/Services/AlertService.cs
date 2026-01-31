using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Alerts;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class AlertService : IAlertService
{
    private readonly IAlertRuleRepository _ruleRepository;
    private readonly IAlertRepository _alertRepository;

    public AlertService(IAlertRuleRepository ruleRepository, IAlertRepository alertRepository)
    {
        _ruleRepository = ruleRepository;
        _alertRepository = alertRepository;
    }

    public async Task<IReadOnlyList<AlertRuleResponse>> GetRulesAsync(Guid tenantId)
    {
        var rules = await _ruleRepository.GetByTenantAsync(tenantId);
        return rules.Select(r => MapRule(r)).ToList();
    }

    public async Task<AlertRuleResponse> CreateRuleAsync(Guid tenantId, CreateAlertRuleRequest request)
    {
        var rule = new AlertRule
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Condition = request.Condition,
            ThresholdJson = request.ThresholdJson,
            Severity = request.Severity,
            IsEnabled = true
        };

        await _ruleRepository.AddAsync(rule);
        return MapRule(rule);
    }

    public async Task<AlertRuleResponse> UpdateRuleAsync(Guid tenantId, Guid ruleId, UpdateAlertRuleRequest request)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId)
            ?? throw new NotFoundException(nameof(AlertRule), ruleId);

        if (rule.TenantId != tenantId)
            throw new NotFoundException(nameof(AlertRule), ruleId);

        if (request.Name is not null) rule.Name = request.Name;
        if (request.Description is not null) rule.Description = request.Description;
        if (request.Condition.HasValue) rule.Condition = request.Condition.Value;
        if (request.ThresholdJson is not null) rule.ThresholdJson = request.ThresholdJson;
        if (request.Severity.HasValue) rule.Severity = request.Severity.Value;
        if (request.IsEnabled.HasValue) rule.IsEnabled = request.IsEnabled.Value;

        await _ruleRepository.UpdateAsync(rule);
        return MapRule(rule);
    }

    public async Task DeleteRuleAsync(Guid tenantId, Guid ruleId)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId)
            ?? throw new NotFoundException(nameof(AlertRule), ruleId);

        if (rule.TenantId != tenantId)
            throw new NotFoundException(nameof(AlertRule), ruleId);

        await _ruleRepository.DeleteAsync(ruleId);
    }

    public async Task<PagedResult<AlertResponse>> GetAlertsAsync(
        Guid tenantId, bool? isAcknowledged, AlertSeverity? severity, int page, int pageSize)
    {
        var (items, totalCount) = await _alertRepository.GetByTenantAsync(
            tenantId, isAcknowledged, severity, page, pageSize);

        return new PagedResult<AlertResponse>
        {
            Items = items.Select(a => MapAlert(a)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AlertCountResponse> GetUnacknowledgedCountAsync(Guid tenantId)
    {
        var count = await _alertRepository.GetUnacknowledgedCountAsync(tenantId);
        return new AlertCountResponse(count);
    }

    public async Task AcknowledgeAlertAsync(Guid tenantId, Guid alertId)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId)
            ?? throw new NotFoundException(nameof(Alert), alertId);

        if (alert.TenantId != tenantId)
            throw new NotFoundException(nameof(Alert), alertId);

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = DateTime.UtcNow;
        await _alertRepository.UpdateAsync(alert);
    }

    private static AlertRuleResponse MapRule(AlertRule r) => new(
        r.Id, r.Name, r.Description, r.Condition, r.ThresholdJson,
        r.Severity, r.IsEnabled, r.CreatedAt);

    private static AlertResponse MapAlert(Alert a) => new(
        a.Id, a.AlertRuleId, a.AlertRule?.Name ?? "", a.Message, a.Severity,
        a.SourceIp, a.DestinationIp, a.MetadataJson,
        a.IsAcknowledged, a.TriggeredAt, a.AcknowledgedAt);
}
