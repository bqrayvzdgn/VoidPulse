using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Alerts;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ICurrentUserService _currentUser;

    public AlertController(IAlertService alertService, ICurrentUserService currentUser)
    {
        _alertService = alertService;
        _currentUser = currentUser;
    }

    // --- Rules ---

    [HttpGet("rules")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AlertRuleResponse>>>> GetRules()
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _alertService.GetRulesAsync(tenantId);
        return Ok(ApiResponse<IReadOnlyList<AlertRuleResponse>>.Succeed(result));
    }

    [HttpPost("rules")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<ActionResult<ApiResponse<AlertRuleResponse>>> CreateRule(
        [FromBody] CreateAlertRuleRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _alertService.CreateRuleAsync(tenantId, request);
        return Created($"/api/v1/alerts/rules/{result.Id}", ApiResponse<AlertRuleResponse>.Succeed(result));
    }

    [HttpPut("rules/{ruleId:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<ActionResult<ApiResponse<AlertRuleResponse>>> UpdateRule(
        Guid ruleId, [FromBody] UpdateAlertRuleRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _alertService.UpdateRuleAsync(tenantId, ruleId, request);
        return Ok(ApiResponse<AlertRuleResponse>.Succeed(result));
    }

    [HttpDelete("rules/{ruleId:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRule(Guid ruleId)
    {
        var tenantId = _currentUser.TenantId!.Value;
        await _alertService.DeleteRuleAsync(tenantId, ruleId);
        return Ok(ApiResponse<object>.Succeed(null!));
    }

    // --- Alerts ---

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AlertResponse>>>> GetAlerts(
        [FromQuery] bool? isAcknowledged = null,
        [FromQuery] AlertSeverity? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _alertService.GetAlertsAsync(tenantId, isAcknowledged, severity, page, pageSize);
        return Ok(ApiResponse<PagedResult<AlertResponse>>.Succeed(result));
    }

    [HttpGet("count")]
    public async Task<ActionResult<ApiResponse<AlertCountResponse>>> GetUnacknowledgedCount()
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _alertService.GetUnacknowledgedCountAsync(tenantId);
        return Ok(ApiResponse<AlertCountResponse>.Succeed(result));
    }

    [HttpPost("{alertId:guid}/acknowledge")]
    public async Task<ActionResult<ApiResponse<object>>> AcknowledgeAlert(Guid alertId)
    {
        var tenantId = _currentUser.TenantId!.Value;
        await _alertService.AcknowledgeAlertAsync(tenantId, alertId);
        return Ok(ApiResponse<object>.Succeed(null!));
    }
}
