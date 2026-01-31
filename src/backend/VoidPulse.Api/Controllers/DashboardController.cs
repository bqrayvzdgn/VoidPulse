using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Dashboard;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<OverviewResponse>>> GetOverview([FromQuery] string period = "24h")
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _dashboardService.GetOverviewAsync(tenantId, period);
        return Ok(ApiResponse<OverviewResponse>.Succeed(result));
    }

    [HttpGet("top-talkers")]
    public async Task<ActionResult<ApiResponse<TopTalkersResponse>>> GetTopTalkers(
        [FromQuery] string period = "24h",
        [FromQuery] int limit = 10)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _dashboardService.GetTopTalkersAsync(tenantId, period, limit);
        return Ok(ApiResponse<TopTalkersResponse>.Succeed(result));
    }

    [HttpGet("protocol-distribution")]
    public async Task<ActionResult<ApiResponse<ProtocolDistributionResponse>>> GetProtocolDistribution(
        [FromQuery] string period = "24h")
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _dashboardService.GetProtocolDistributionAsync(tenantId, period);
        return Ok(ApiResponse<ProtocolDistributionResponse>.Succeed(result));
    }

    [HttpGet("bandwidth")]
    public async Task<ActionResult<ApiResponse<BandwidthResponse>>> GetBandwidth(
        [FromQuery] string period = "24h")
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _dashboardService.GetBandwidthAsync(tenantId, period);
        return Ok(ApiResponse<BandwidthResponse>.Succeed(result));
    }

    [HttpGet("sites")]
    public async Task<ActionResult<ApiResponse<SitesResponse>>> GetTopSites(
        [FromQuery] string period = "24h",
        [FromQuery] int limit = 20)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _dashboardService.GetTopSitesAsync(tenantId, period, limit);
        return Ok(ApiResponse<SitesResponse>.Succeed(result));
    }

    [HttpGet("processes")]
    public async Task<ActionResult<ApiResponse<ProcessesResponse>>> GetTopProcesses(
        [FromQuery] string period = "24h",
        [FromQuery] int limit = 20)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _dashboardService.GetTopProcessesAsync(tenantId, period, limit);
        return Ok(ApiResponse<ProcessesResponse>.Succeed(result));
    }
}
