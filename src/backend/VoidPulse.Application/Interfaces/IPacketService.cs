using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Traffic;

namespace VoidPulse.Application.Interfaces;

public interface IPacketService
{
    Task<IReadOnlyList<CapturedPacketResponse>> IngestBatchAsync(Guid tenantId, IEnumerable<IngestPacketRequest> requests);
    Task<PagedResult<CapturedPacketResponse>> GetByFlowIdAsync(Guid flowId, Guid tenantId, int page, int pageSize);
    Task<PagedResult<CapturedPacketResponse>> QueryAsync(Guid tenantId, PacketQueryParams queryParams);
    Task<CapturedPacketResponse> GetByIdAsync(Guid id, Guid tenantId);
}
