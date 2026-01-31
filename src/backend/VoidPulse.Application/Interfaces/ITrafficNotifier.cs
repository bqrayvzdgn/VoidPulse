using VoidPulse.Application.DTOs.Alerts;
using VoidPulse.Application.DTOs.Traffic;

namespace VoidPulse.Application.Interfaces;

public interface ITrafficNotifier
{
    Task NotifyFlowIngestedAsync(Guid tenantId, TrafficFlowResponse flow);
    Task NotifyBatchIngestedAsync(Guid tenantId, IReadOnlyList<TrafficFlowResponse> flows);
    Task NotifyAlertAsync(Guid tenantId, AlertResponse alert);
}
