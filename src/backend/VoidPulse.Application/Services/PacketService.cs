using System.Text.Json;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class PacketService : IPacketService
{
    private readonly ICapturedPacketRepository _packetRepository;
    private readonly ITrafficFlowRepository _flowRepository;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PacketService(ICapturedPacketRepository packetRepository, ITrafficFlowRepository flowRepository)
    {
        _packetRepository = packetRepository;
        _flowRepository = flowRepository;
    }

    public async Task<IReadOnlyList<CapturedPacketResponse>> IngestBatchAsync(
        Guid tenantId, IEnumerable<IngestPacketRequest> requests)
    {
        var packets = requests.Select(r => new CapturedPacket
        {
            TenantId = tenantId,
            CapturedAt = r.CapturedAt,
            SourceIp = r.SourceIp,
            DestinationIp = r.DestinationIp,
            SourcePort = r.SourcePort,
            DestinationPort = r.DestinationPort,
            Protocol = r.Protocol,
            PacketLength = r.PacketLength,
            HeaderBytes = Convert.FromBase64String(r.HeaderBytesBase64),
            ProtocolStack = r.ProtocolStackJson,
            Info = r.Info
        }).ToList();

        // Correlate packets to existing flows by 5-tuple match
        // Cache lookups within the batch to avoid repeated DB queries for same tuple
        var tupleCache = new Dictionary<string, Guid?>();
        foreach (var packet in packets)
        {
            var tupleKey = $"{packet.SourceIp}:{packet.SourcePort}->{packet.DestinationIp}:{packet.DestinationPort}/{packet.Protocol}";
            if (!tupleCache.TryGetValue(tupleKey, out var flowId))
            {
                flowId = await _flowRepository.FindFlowIdByTupleAsync(
                    tenantId, packet.SourceIp, packet.DestinationIp,
                    packet.SourcePort, packet.DestinationPort,
                    packet.Protocol, packet.CapturedAt);
                tupleCache[tupleKey] = flowId;
            }
            packet.TrafficFlowId = flowId;
        }

        await _packetRepository.AddBatchAsync(packets);

        return packets.Select(MapToResponse).ToList();
    }

    public async Task<PagedResult<CapturedPacketResponse>> GetByFlowIdAsync(
        Guid flowId, Guid tenantId, int page, int pageSize)
    {
        // First try direct TrafficFlowId match
        var (items, totalCount) = await _packetRepository.GetByFlowIdAsync(flowId, tenantId, page, pageSize);

        // If no direct matches, fall back to 5-tuple + time window search
        if (totalCount == 0)
        {
            var flow = await _flowRepository.GetByIdAsync(flowId);
            if (flow is not null && flow.TenantId == tenantId)
            {
                // Query packets in the flow's time window with extra margin
                (items, totalCount) = await _packetRepository.QueryAsync(
                    tenantId,
                    sourceIp: null,
                    destIp: null,
                    protocol: flow.Protocol,
                    startDate: flow.StartedAt.AddSeconds(-5),
                    endDate: flow.EndedAt.AddSeconds(5),
                    search: null,
                    page: 1,
                    pageSize: 10000);

                // Filter for exact 5-tuple match (both directions)
                var filtered = items.Where(p =>
                    (p.SourceIp == flow.SourceIp && p.DestinationIp == flow.DestinationIp
                        && p.SourcePort == flow.SourcePort && p.DestinationPort == flow.DestinationPort)
                    || (p.SourceIp == flow.DestinationIp && p.DestinationIp == flow.SourceIp
                        && p.SourcePort == flow.DestinationPort && p.DestinationPort == flow.SourcePort))
                    .ToList();

                totalCount = filtered.Count;
                items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }
        }

        return new PagedResult<CapturedPacketResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<CapturedPacketResponse>> QueryAsync(
        Guid tenantId, PacketQueryParams queryParams)
    {
        var (items, totalCount) = await _packetRepository.QueryAsync(
            tenantId,
            queryParams.SourceIp,
            queryParams.DestinationIp,
            queryParams.Protocol,
            queryParams.StartDate,
            queryParams.EndDate,
            queryParams.Search,
            queryParams.Page,
            queryParams.PageSize);

        return new PagedResult<CapturedPacketResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<CapturedPacketResponse> GetByIdAsync(Guid id, Guid tenantId)
    {
        var packet = await _packetRepository.GetByIdAsync(id);
        if (packet is null || packet.TenantId != tenantId)
            throw new KeyNotFoundException($"Packet {id} not found.");

        return MapToResponse(packet);
    }

    private static CapturedPacketResponse MapToResponse(CapturedPacket packet)
    {
        List<ProtocolLayerDto> protocolStack;
        try
        {
            protocolStack = JsonSerializer.Deserialize<List<ProtocolLayerDto>>(
                packet.ProtocolStack, JsonOptions) ?? [];
        }
        catch
        {
            protocolStack = [];
        }

        return new CapturedPacketResponse(
            packet.Id,
            packet.TrafficFlowId,
            packet.CapturedAt,
            packet.SourceIp,
            packet.DestinationIp,
            packet.SourcePort,
            packet.DestinationPort,
            packet.Protocol,
            packet.PacketLength,
            Convert.ToBase64String(packet.HeaderBytes),
            protocolStack,
            packet.Info);
    }
}
