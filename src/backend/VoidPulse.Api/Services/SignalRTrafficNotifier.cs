using Microsoft.AspNetCore.SignalR;
using VoidPulse.Api.Hubs;
using VoidPulse.Application.DTOs.Alerts;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Services;

public class SignalRTrafficNotifier : ITrafficNotifier
{
    private readonly IHubContext<TrafficHub> _hubContext;
    private readonly ILogger<SignalRTrafficNotifier> _logger;

    public SignalRTrafficNotifier(IHubContext<TrafficHub> hubContext, ILogger<SignalRTrafficNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyFlowIngestedAsync(Guid tenantId, TrafficFlowResponse flow)
    {
        var groupName = $"tenant:{tenantId}";
        _logger.LogInformation("Broadcasting FlowIngested to group {Group}, flowId={FlowId}", groupName, flow.Id);
        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("FlowIngested", flow);
    }

    public async Task NotifyBatchIngestedAsync(Guid tenantId, IReadOnlyList<TrafficFlowResponse> flows)
    {
        await _hubContext.Clients
            .Group($"tenant:{tenantId}")
            .SendAsync("BatchIngested", flows);
    }

    public async Task NotifyAlertAsync(Guid tenantId, AlertResponse alert)
    {
        await _hubContext.Clients
            .Group($"tenant:{tenantId}")
            .SendAsync("AlertTriggered", alert);
    }
}
