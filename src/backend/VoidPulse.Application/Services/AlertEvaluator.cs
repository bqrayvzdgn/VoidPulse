using System.Text.Json;
using VoidPulse.Application.DTOs.Alerts;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

// Case-insensitive JSON options for threshold parsing
file static class AlertJsonOptions
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

public class AlertEvaluator : IAlertEvaluator
{
    private readonly IAlertRuleRepository _ruleRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly ITrafficNotifier _trafficNotifier;

    public AlertEvaluator(
        IAlertRuleRepository ruleRepository,
        IAlertRepository alertRepository,
        ITrafficNotifier trafficNotifier)
    {
        _ruleRepository = ruleRepository;
        _alertRepository = alertRepository;
        _trafficNotifier = trafficNotifier;
    }

    public async Task EvaluateFlowAsync(Guid tenantId, TrafficFlowResponse flow)
    {
        var rules = await _ruleRepository.GetActiveByTenantAsync(tenantId);
        var alerts = new List<Alert>();

        foreach (var rule in rules)
        {
            var alert = EvaluateRule(rule, tenantId, flow);
            if (alert is not null)
                alerts.Add(alert);
        }

        // Port scan detection needs historical data
        foreach (var rule in rules.Where(r => r.Condition == AlertCondition.PortScan))
        {
            var alert = await EvaluatePortScanAsync(rule, tenantId, flow);
            if (alert is not null)
                alerts.Add(alert);
        }

        if (alerts.Count > 0)
        {
            await _alertRepository.AddBatchAsync(alerts);

            // Broadcast alerts via SignalR
            foreach (var alert in alerts)
            {
                var alertResponse = new AlertResponse(
                    alert.Id, alert.AlertRuleId,
                    rules.FirstOrDefault(r => r.Id == alert.AlertRuleId)?.Name ?? "",
                    alert.Message, alert.Severity,
                    alert.SourceIp, alert.DestinationIp, alert.MetadataJson,
                    alert.IsAcknowledged, alert.TriggeredAt, alert.AcknowledgedAt);

                await _trafficNotifier.NotifyAlertAsync(tenantId, alertResponse);
            }
        }
    }

    private static Alert? EvaluateRule(AlertRule rule, Guid tenantId, TrafficFlowResponse flow)
    {
        return rule.Condition switch
        {
            AlertCondition.ByteThreshold => EvaluateByteThreshold(rule, tenantId, flow),
            AlertCondition.ProtocolAnomaly => EvaluateProtocolAnomaly(rule, tenantId, flow),
            // PortScan handled async separately
            // UnknownDestination could be implemented with an allowlist
            _ => null
        };
    }

    private static Alert? EvaluateByteThreshold(AlertRule rule, Guid tenantId, TrafficFlowResponse flow)
    {
        try
        {
            var threshold = JsonSerializer.Deserialize<ByteThresholdConfig>(rule.ThresholdJson, AlertJsonOptions.CaseInsensitive);
            if (threshold is null) return null;

            var totalBytes = flow.BytesSent + flow.BytesReceived;
            if (totalBytes > threshold.MaxBytes)
            {
                return new Alert
                {
                    TenantId = tenantId,
                    AlertRuleId = rule.Id,
                    Message = $"Flow exceeded {threshold.MaxBytes:N0} bytes threshold ({totalBytes:N0} bytes): {flow.SourceIp} -> {flow.DestinationIp}",
                    Severity = rule.Severity,
                    SourceIp = flow.SourceIp,
                    DestinationIp = flow.DestinationIp,
                    TriggeredAt = DateTime.UtcNow
                };
            }
        }
        catch { }

        return null;
    }

    private static Alert? EvaluateProtocolAnomaly(AlertRule rule, Guid tenantId, TrafficFlowResponse flow)
    {
        // Check for non-standard protocols on well-known ports
        var knownPorts = new Dictionary<int, string>
        {
            { 80, "TCP" }, { 443, "TCP" }, { 53, "UDP" },
            { 22, "TCP" }, { 25, "TCP" }, { 3389, "TCP" }
        };

        if (knownPorts.TryGetValue(flow.DestinationPort, out var expectedProtocol))
        {
            if (!string.Equals(flow.Protocol, expectedProtocol, StringComparison.OrdinalIgnoreCase))
            {
                return new Alert
                {
                    TenantId = tenantId,
                    AlertRuleId = rule.Id,
                    Message = $"Protocol anomaly: {flow.Protocol} on port {flow.DestinationPort} (expected {expectedProtocol}): {flow.SourceIp} -> {flow.DestinationIp}",
                    Severity = rule.Severity,
                    SourceIp = flow.SourceIp,
                    DestinationIp = flow.DestinationIp,
                    TriggeredAt = DateTime.UtcNow
                };
            }
        }

        return null;
    }

    private async Task<Alert?> EvaluatePortScanAsync(AlertRule rule, Guid tenantId, TrafficFlowResponse flow)
    {
        try
        {
            var config = JsonSerializer.Deserialize<PortScanConfig>(rule.ThresholdJson, AlertJsonOptions.CaseInsensitive);
            if (config is null) return null;

            var since = DateTime.UtcNow.AddSeconds(-config.TimeWindowSeconds);
            var distinctPorts = await _alertRepository.GetRecentPortCountAsync(tenantId, flow.SourceIp, since);

            if (distinctPorts > config.MaxDistinctPorts)
            {
                return new Alert
                {
                    TenantId = tenantId,
                    AlertRuleId = rule.Id,
                    Message = $"Port scan detected: {flow.SourceIp} hit {distinctPorts} distinct ports in {config.TimeWindowSeconds}s (threshold: {config.MaxDistinctPorts})",
                    Severity = rule.Severity,
                    SourceIp = flow.SourceIp,
                    TriggeredAt = DateTime.UtcNow
                };
            }
        }
        catch { }

        return null;
    }

    private record ByteThresholdConfig(long MaxBytes);
    private record PortScanConfig(int MaxDistinctPorts, int TimeWindowSeconds);
}
