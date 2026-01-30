using VoidPulse.Application.DTOs.Dashboard;

namespace VoidPulse.Application.Interfaces;

public interface IDashboardService
{
    Task<OverviewResponse> GetOverviewAsync(Guid tenantId, string period);
    Task<TopTalkersResponse> GetTopTalkersAsync(Guid tenantId, string period);
    Task<ProtocolDistributionResponse> GetProtocolDistributionAsync(Guid tenantId, string period);
    Task<BandwidthResponse> GetBandwidthAsync(Guid tenantId, string period);
}
