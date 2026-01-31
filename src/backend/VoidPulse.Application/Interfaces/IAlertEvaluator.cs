using VoidPulse.Application.DTOs.Traffic;

namespace VoidPulse.Application.Interfaces;

public interface IAlertEvaluator
{
    Task EvaluateFlowAsync(Guid tenantId, TrafficFlowResponse flow);
}
