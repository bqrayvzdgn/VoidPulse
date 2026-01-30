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

        var (flows, totalCount) = await _trafficFlowRepository.QueryAsync(
            tenantId, null, null, null, startDate, endDate, null, null, 1, int.MaxValue);

        var totalBytes = flows.Sum(f => f.BytesSent + f.BytesReceived);
        var uniqueSourceIps = flows.Select(f => f.SourceIp).Distinct().Count();
        var uniqueDestIps = flows.Select(f => f.DestinationIp).Distinct().Count();

        var agentKeys = await _agentKeyRepository.GetByTenantAsync(tenantId);
        var activeAgents = agentKeys.Count(a => a.IsActive);

        var result = new OverviewResponse(
            totalCount,
            totalBytes,
            activeAgents,
            uniqueSourceIps,
            uniqueDestIps);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<TopTalkersResponse> GetTopTalkersAsync(Guid tenantId, string period)
    {
        var cacheKey = $"dashboard:top-talkers:{tenantId}:{period}";
        var cached = await _cacheService.GetAsync<TopTalkersResponse>(cacheKey);
        if (cached is not null)
            return cached;

        var (startDate, endDate) = ParsePeriod(period);

        var (flows, _) = await _trafficFlowRepository.QueryAsync(
            tenantId, null, null, null, startDate, endDate, null, null, 1, int.MaxValue);

        var entries = flows
            .GroupBy(f => f.SourceIp)
            .Select(g => new TalkerEntry(
                g.Key,
                g.Sum(f => f.BytesSent + f.BytesReceived),
                g.Count()))
            .OrderByDescending(e => e.TotalBytes)
            .Take(10)
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

        var (flows, _) = await _trafficFlowRepository.QueryAsync(
            tenantId, null, null, null, startDate, endDate, null, null, 1, int.MaxValue);

        var totalBytes = flows.Sum(f => f.BytesSent + f.BytesReceived);

        var entries = flows
            .GroupBy(f => f.Protocol)
            .Select(g =>
            {
                var groupBytes = g.Sum(f => f.BytesSent + f.BytesReceived);
                var percentage = totalBytes > 0 ? (double)groupBytes / totalBytes * 100.0 : 0.0;
                return new ProtocolEntry(g.Key, groupBytes, g.Count(), Math.Round(percentage, 2));
            })
            .OrderByDescending(e => e.TotalBytes)
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

        var (flows, _) = await _trafficFlowRepository.QueryAsync(
            tenantId, null, null, null, startDate, endDate, null, null, 1, int.MaxValue);

        var entries = flows
            .GroupBy(f => new DateTime(f.StartedAt.Year, f.StartedAt.Month, f.StartedAt.Day,
                f.StartedAt.Hour, 0, 0, DateTimeKind.Utc))
            .Select(g => new BandwidthEntry(
                g.Key,
                g.Sum(f => f.BytesSent),
                g.Sum(f => f.BytesReceived),
                g.Sum(f => f.BytesSent + f.BytesReceived)))
            .OrderBy(e => e.Timestamp)
            .ToList();

        var result = new BandwidthResponse(entries);

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
            _ => endDate.AddHours(-24) // Default to 24 hours
        };

        return (startDate, endDate);
    }
}
