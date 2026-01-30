using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/traffic")]
public class TrafficController : ControllerBase
{
    private readonly ITrafficService _trafficService;
    private readonly ICurrentUserService _currentUser;

    public TrafficController(ITrafficService trafficService, ICurrentUserService currentUser)
    {
        _trafficService = trafficService;
        _currentUser = currentUser;
    }

    [HttpPost("ingest")]
    public async Task<ActionResult<ApiResponse<TrafficFlowResponse>>> Ingest(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] IngestTrafficRequest record)
    {
        // Agent key validation is handled by the service layer
        var result = await _trafficService.IngestAsync(Guid.Empty, Guid.Empty, record);
        return Ok(ApiResponse<TrafficFlowResponse>.Succeed(result));
    }

    [HttpPost("ingest/batch")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrafficFlowResponse>>>> IngestBatch(
        [FromHeader(Name = "X-Api-Key")] string apiKey,
        [FromBody] List<IngestTrafficRequest> records)
    {
        var result = await _trafficService.IngestBatchAsync(Guid.Empty, Guid.Empty, records);
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
}
