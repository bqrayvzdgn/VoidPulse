using VoidPulse.Application.DTOs.Dashboard;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITrafficFlowRepository _trafficFlowRepository;
    private readonly IAgentKeyRepository _agentKeyRepository;
    private readonly ICacheService _cacheService;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public DashboardService(
        ITrafficFlowRepository trafficFlowRepository,
        IAgentKeyRepository agentKeyRepository,
        ICacheService cacheService)
    {
        _trafficFlowRepository = trafficFlowRepository;
        _agentKeyRepository = agentKeyRepository;
        _cacheService = cacheService;
    }

    public async Task<OverviewResponse> GetOverviewAsync(Guid tenantId, string period)
    {
        var cacheKey = $"dashboard:overview:{tenantId}:{period}";
        var cached = await _cacheService.GetAsync<OverviewResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var (totalFlows, totalBytes, uniqueSourceIps, uniqueDestIps) =
            await _trafficFlowRepository.GetOverviewStatsAsync(tenantId, startDate, endDate);

        var agentKeys = await _agentKeyRepository.GetByTenantAsync(tenantId);
        var activeAgents = agentKeys.Count(a => a.IsActive);

        var result = new OverviewResponse(
            totalFlows,
            totalBytes,
            activeAgents,
            uniqueSourceIps,
            uniqueDestIps);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<TopTalkersResponse> GetTopTalkersAsync(Guid tenantId, string period, int limit = 10)
    {
        var cacheKey = $"dashboard:top-talkers:{tenantId}:{period}:{limit}";
        var cached = await _cacheService.GetAsync<TopTalkersResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var talkers = await _trafficFlowRepository.GetTopTalkersAsync(tenantId, startDate, endDate, limit);

        var entries = talkers
            .Select(t => new TalkerEntry(t.Ip, t.TotalBytes, t.FlowCount))
            .ToList();

        var result = new TopTalkersResponse(entries);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<ProtocolDistributionResponse> GetProtocolDistributionAsync(Guid tenantId, string period)
    {
        var cacheKey = $"dashboard:protocol-dist:{tenantId}:{period}";
        var cached = await _cacheService.GetAsync<ProtocolDistributionResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var protocols = await _trafficFlowRepository.GetProtocolDistributionAsync(tenantId, startDate, endDate);

        var totalBytes = protocols.Sum(p => p.TotalBytes);

        var entries = protocols
            .Select(p =>
            {
                var percentage = totalBytes > 0 ? (double)p.TotalBytes / totalBytes * 100.0 : 0.0;
                return new ProtocolEntry(p.Protocol, p.TotalBytes, p.FlowCount, Math.Round(percentage, 2));
            })
            .ToList();

        var result = new ProtocolDistributionResponse(entries);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<BandwidthResponse> GetBandwidthAsync(Guid tenantId, string period)
    {
        var cacheKey = $"dashboard:bandwidth:{tenantId}:{period}";
        var cached = await _cacheService.GetAsync<BandwidthResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var timeline = await _trafficFlowRepository.GetBandwidthTimelineAsync(tenantId, startDate, endDate);

        var entries = timeline
            .Select(t => new BandwidthEntry(t.Hour, t.BytesSent, t.BytesReceived, t.BytesSent + t.BytesReceived))
            .ToList();

        var result = new BandwidthResponse(entries);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<SitesResponse> GetTopSitesAsync(Guid tenantId, string period, int limit = 20)
    {
        var cacheKey = $"dashboard:sites:{tenantId}:{period}:{limit}";
        var cached = await _cacheService.GetAsync<SitesResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var sites = await _trafficFlowRepository.GetTopSitesAsync(tenantId, startDate, endDate, limit);

        var entries = sites
            .Select(s => new SiteEntry(s.Hostname, s.TotalBytes, s.FlowCount, s.LastSeen))
            .ToList();

        var result = new SitesResponse(entries, entries.Count);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<ProcessesResponse> GetTopProcessesAsync(Guid tenantId, string period, int limit = 20)
    {
        var cacheKey = $"dashboard:processes:{tenantId}:{period}:{limit}";
        var cached = await _cacheService.GetAsync<ProcessesResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var processes = await _trafficFlowRepository.GetTopProcessesAsync(tenantId, startDate, endDate, limit);

        var entries = processes
            .Select(p => new ProcessEntry(p.ProcessName, p.TotalBytes, p.FlowCount, p.LastSeen))
            .ToList();

        var result = new ProcessesResponse(entries, entries.Count);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    private static (DateTime StartDate, DateTime EndDate) ParsePeriod(string period)
    {
        var endDate = DateTime.UtcNow;
        var startDate = period.ToLowerInvariant() switch
        {
            "1h" => endDate.AddHours(-1),
            "6h" => endDate.AddHours(-6),
            "24h" => endDate.AddHours(-24),
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            "90d" => endDate.AddDays(-90),
            _ => endDate.AddHours(-24)
        };

        return (startDate, endDate);
    }
}
