using System.Globalization;
using System.Text;
using AutoMapper;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class TrafficService : ITrafficService
{
    private readonly ITrafficFlowRepository _trafficFlowRepository;
    private readonly IAgentKeyRepository _agentKeyRepository;
    private readonly IMapper _mapper;
    private readonly ITrafficNotifier _trafficNotifier;
    private readonly IAlertEvaluator _alertEvaluator;

    public TrafficService(
        ITrafficFlowRepository trafficFlowRepository,
        IAgentKeyRepository agentKeyRepository,
        IMapper mapper,
        ITrafficNotifier trafficNotifier,
        IAlertEvaluator alertEvaluator)
    {
        _trafficFlowRepository = trafficFlowRepository;
        _agentKeyRepository = agentKeyRepository;
        _mapper = mapper;
        _trafficNotifier = trafficNotifier;
        _alertEvaluator = alertEvaluator;
    }

    public async Task<TrafficFlowResponse> IngestAsync(Guid tenantId, Guid agentKeyId, IngestTrafficRequest request)
    {
        var agentKey = await _agentKeyRepository.GetByIdAsync(agentKeyId)
            ?? throw new NotFoundException(nameof(AgentKey), agentKeyId);

        if (agentKey.TenantId != tenantId || !agentKey.IsActive)
            throw new UnauthorizedException("Invalid or inactive agent key.");

        var flow = CreateTrafficFlow(tenantId, agentKeyId, request);
        await _trafficFlowRepository.AddAsync(flow);

        agentKey.LastUsedAt = DateTime.UtcNow;
        await _agentKeyRepository.UpdateAsync(agentKey);

        var response = _mapper.Map<TrafficFlowResponse>(flow);
        await _trafficNotifier.NotifyFlowIngestedAsync(tenantId, response);
        await _alertEvaluator.EvaluateFlowAsync(tenantId, response);
        return response;
    }

    public async Task<IReadOnlyList<TrafficFlowResponse>> IngestBatchAsync(
        Guid tenantId, Guid agentKeyId, IEnumerable<IngestTrafficRequest> requests)
    {
        var agentKey = await _agentKeyRepository.GetByIdAsync(agentKeyId)
            ?? throw new NotFoundException(nameof(AgentKey), agentKeyId);

        if (agentKey.TenantId != tenantId || !agentKey.IsActive)
            throw new UnauthorizedException("Invalid or inactive agent key.");

        var flows = requests.Select(r => CreateTrafficFlow(tenantId, agentKeyId, r)).ToList();
        await _trafficFlowRepository.AddBatchAsync(flows);

        agentKey.LastUsedAt = DateTime.UtcNow;
        await _agentKeyRepository.UpdateAsync(agentKey);

        var responses = _mapper.Map<IReadOnlyList<TrafficFlowResponse>>(flows);
        await _trafficNotifier.NotifyBatchIngestedAsync(tenantId, responses);

        foreach (var response in responses)
        {
            await _alertEvaluator.EvaluateFlowAsync(tenantId, response);
        }

        return responses;
    }

    public async Task<PagedResult<TrafficFlowResponse>> QueryAsync(Guid tenantId, TrafficQueryParams queryParams)
    {
        var (items, totalCount) = await _trafficFlowRepository.QueryAsync(
            tenantId,
            queryParams.SourceIp,
            queryParams.DestinationIp,
            queryParams.Protocol,
            queryParams.StartDate,
            queryParams.EndDate,
            queryParams.SortBy,
            queryParams.SortOrder,
            queryParams.Page,
            queryParams.PageSize);

        return new PagedResult<TrafficFlowResponse>
        {
            Items = _mapper.Map<IReadOnlyList<TrafficFlowResponse>>(items),
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<TrafficFlowResponse> GetByIdAsync(Guid id, Guid tenantId)
    {
        var flow = await _trafficFlowRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(TrafficFlow), id);

        if (flow.TenantId != tenantId)
            throw new NotFoundException(nameof(TrafficFlow), id);

        return _mapper.Map<TrafficFlowResponse>(flow);
    }

    public async Task<byte[]> ExportCsvAsync(Guid tenantId, TrafficQueryParams queryParams)
    {
        var (items, _) = await _trafficFlowRepository.QueryAsync(
            tenantId,
            queryParams.SourceIp,
            queryParams.DestinationIp,
            queryParams.Protocol,
            queryParams.StartDate,
            queryParams.EndDate,
            queryParams.SortBy,
            queryParams.SortOrder,
            1,
            10000); // Export up to 10k rows

        var sb = new StringBuilder();
        sb.AppendLine("Id,SourceIp,DestinationIp,SourcePort,DestinationPort,Protocol,BytesSent,BytesReceived,PacketsSent,PacketsReceived,StartedAt,EndedAt,FlowDuration");

        foreach (var flow in items)
        {
            sb.AppendLine(string.Join(",",
                flow.Id,
                EscapeCsv(flow.SourceIp),
                EscapeCsv(flow.DestinationIp),
                flow.SourcePort,
                flow.DestinationPort,
                EscapeCsv(flow.Protocol),
                flow.BytesSent,
                flow.BytesReceived,
                flow.PacketsSent,
                flow.PacketsReceived,
                flow.StartedAt.ToString("O", CultureInfo.InvariantCulture),
                flow.EndedAt.ToString("O", CultureInfo.InvariantCulture),
                flow.FlowDuration.ToString(CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static TrafficFlow CreateTrafficFlow(Guid tenantId, Guid agentKeyId, IngestTrafficRequest request)
    {
        var flow = new TrafficFlow
        {
            TenantId = tenantId,
            AgentKeyId = agentKeyId,
            SourceIp = request.SourceIp,
            DestinationIp = request.DestinationIp,
            SourcePort = request.SourcePort,
            DestinationPort = request.DestinationPort,
            Protocol = request.Protocol,
            BytesSent = request.BytesSent,
            BytesReceived = request.BytesReceived,
            PacketsSent = request.PacketsSent,
            PacketsReceived = request.PacketsReceived,
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
            FlowDuration = (request.EndedAt - request.StartedAt).TotalSeconds,
            ProcessName = request.ProcessName,
            ResolvedHostname = request.Hostname,
            TlsSni = request.TlsSni
        };

        if (request.HttpMetadata is not null)
        {
            flow.HttpMetadata = new HttpMetadata
            {
                TrafficFlowId = flow.Id,
                Method = request.HttpMetadata.Method,
                Host = request.HttpMetadata.Host,
                Path = request.HttpMetadata.Path,
                StatusCode = request.HttpMetadata.StatusCode,
                UserAgent = request.HttpMetadata.UserAgent,
                ContentType = request.HttpMetadata.ContentType,
                ResponseTimeMs = request.HttpMetadata.ResponseTimeMs
            };
        }

        return flow;
    }

    private static string EscapeCsv(string value)
    {
        // Prevent CSV formula injection (=, +, -, @, tab, carriage return)
        if (value.Length > 0 && "=+@-\t\r".Contains(value[0]))
            value = "'" + value;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
