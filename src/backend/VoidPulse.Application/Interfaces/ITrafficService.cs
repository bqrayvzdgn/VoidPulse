using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Traffic;

namespace VoidPulse.Application.Interfaces;

public interface ITrafficService
{
    Task<TrafficFlowResponse> IngestAsync(Guid tenantId, Guid agentKeyId, IngestTrafficRequest request);
    Task<IReadOnlyList<TrafficFlowResponse>> IngestBatchAsync(Guid tenantId, Guid agentKeyId, IEnumerable<IngestTrafficRequest> requests);
    Task<PagedResult<TrafficFlowResponse>> QueryAsync(Guid tenantId, TrafficQueryParams queryParams);
    Task<TrafficFlowResponse> GetByIdAsync(Guid id, Guid tenantId);
    Task<byte[]> ExportCsvAsync(Guid tenantId, TrafficQueryParams queryParams);
}
