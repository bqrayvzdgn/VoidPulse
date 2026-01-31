using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/traffic")]
[EnableRateLimiting("global")]
public class TrafficController : ControllerBase
{
    private readonly ITrafficService _trafficService;
    private readonly IPacketService _packetService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAgentKeyRepository _agentKeyRepository;
    private readonly IDnsResolutionRepository _dnsResolutionRepository;

    public TrafficController(
        ITrafficService trafficService,
        IPacketService packetService,
        ICurrentUserService currentUser,
        IAgentKeyRepository agentKeyRepository,
        IDnsResolutionRepository dnsResolutionRepository)
    {
        _trafficService = trafficService;
        _packetService = packetService;
        _currentUser = currentUser;
        _agentKeyRepository = agentKeyRepository;
        _dnsResolutionRepository = dnsResolutionRepository;
    }

    [HttpPost("ingest")]
    [EnableRateLimiting("ingest")]
    public async Task<ActionResult<ApiResponse<TrafficFlowResponse>>> Ingest(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] IngestTrafficRequest record)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(ApiResponse<TrafficFlowResponse>.Fail("UNAUTHORIZED", "X-Api-Key header is required."));

        var agentKey = await _agentKeyRepository.GetByApiKeyAsync(apiKey);
        if (agentKey is null || !agentKey.IsActive)
            return Unauthorized(ApiResponse<TrafficFlowResponse>.Fail("UNAUTHORIZED", "Invalid or inactive API key."));

        var result = await _trafficService.IngestAsync(agentKey.TenantId, agentKey.Id, record);
        return Ok(ApiResponse<TrafficFlowResponse>.Succeed(result));
    }

    [HttpPost("ingest/batch")]
    [EnableRateLimiting("ingest")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrafficFlowResponse>>>> IngestBatch(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] List<IngestTrafficRequest> records)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(ApiResponse<IReadOnlyList<TrafficFlowResponse>>.Fail("UNAUTHORIZED", "X-Api-Key header is required."));

        var agentKey = await _agentKeyRepository.GetByApiKeyAsync(apiKey);
        if (agentKey is null || !agentKey.IsActive)
            return Unauthorized(ApiResponse<IReadOnlyList<TrafficFlowResponse>>.Fail("UNAUTHORIZED", "Invalid or inactive API key."));

        var result = await _trafficService.IngestBatchAsync(agentKey.TenantId, agentKey.Id, records);
        return Ok(ApiResponse<IReadOnlyList<TrafficFlowResponse>>.Succeed(result));
    }

    [Authorize(Policy = "Analyst")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TrafficFlowResponse>>>> Query([FromQuery] TrafficQueryParams queryParams)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _trafficService.QueryAsync(tenantId, queryParams);
        return Ok(ApiResponse<PagedResult<TrafficFlowResponse>>.Succeed(result));
    }

    [Authorize(Policy = "Analyst")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TrafficFlowResponse>>> GetById([FromRoute] Guid id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _trafficService.GetByIdAsync(id, tenantId);
        return Ok(ApiResponse<TrafficFlowResponse>.Succeed(result));
    }

    [Authorize(Policy = "Analyst")]
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] TrafficQueryParams queryParams)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var csvBytes = await _trafficService.ExportCsvAsync(tenantId, queryParams);
        return File(csvBytes, "text/csv", $"traffic-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpPost("dns")]
    [EnableRateLimiting("ingest")]
    public async Task<ActionResult<ApiResponse<object>>> IngestDns(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] List<IngestDnsRequest> records)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "X-Api-Key header is required."));

        var agentKey = await _agentKeyRepository.GetByApiKeyAsync(apiKey);
        if (agentKey is null || !agentKey.IsActive)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Invalid or inactive API key."));

        var resolutions = records.Select(r => new DnsResolution
        {
            TenantId = agentKey.TenantId,
            QueriedHostname = r.QueriedHostname,
            ResolvedIp = r.ResolvedIp,
            QueryType = r.QueryType,
            Ttl = r.Ttl,
            ClientIp = r.ClientIp,
            ResolvedAt = r.ResolvedAt ?? DateTime.UtcNow
        }).ToList();

        await _dnsResolutionRepository.AddBatchAsync(resolutions);

        return Ok(ApiResponse<object>.Succeed(new { count = resolutions.Count }));
    }

    // ── Packet Capture Endpoints ──────────────────────────

    [HttpPost("packets/ingest")]
    [EnableRateLimiting("ingest")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CapturedPacketResponse>>>> IngestPackets(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] List<IngestPacketRequest> records)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(ApiResponse<IReadOnlyList<CapturedPacketResponse>>.Fail("UNAUTHORIZED", "X-Api-Key header is required."));

        var agentKey = await _agentKeyRepository.GetByApiKeyAsync(apiKey);
        if (agentKey is null || !agentKey.IsActive)
            return Unauthorized(ApiResponse<IReadOnlyList<CapturedPacketResponse>>.Fail("UNAUTHORIZED", "Invalid or inactive API key."));

        var result = await _packetService.IngestBatchAsync(agentKey.TenantId, records);
        return Ok(ApiResponse<IReadOnlyList<CapturedPacketResponse>>.Succeed(result));
    }

    [Authorize(Policy = "Analyst")]
    [HttpGet("{flowId:guid}/packets")]
    public async Task<ActionResult<ApiResponse<PagedResult<CapturedPacketResponse>>>> GetPacketsForFlow(
        [FromRoute] Guid flowId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _packetService.GetByFlowIdAsync(flowId, tenantId, page, pageSize);
        return Ok(ApiResponse<PagedResult<CapturedPacketResponse>>.Succeed(result));
    }

    [Authorize(Policy = "Analyst")]
    [HttpGet("packets")]
    public async Task<ActionResult<ApiResponse<PagedResult<CapturedPacketResponse>>>> QueryPackets(
        [FromQuery] PacketQueryParams queryParams)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _packetService.QueryAsync(tenantId, queryParams);
        return Ok(ApiResponse<PagedResult<CapturedPacketResponse>>.Succeed(result));
    }

    [Authorize(Policy = "Analyst")]
    [HttpGet("packets/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CapturedPacketResponse>>> GetPacketById(
        [FromRoute] Guid id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _packetService.GetByIdAsync(id, tenantId);
        return Ok(ApiResponse<CapturedPacketResponse>.Succeed(result));
    }
}
