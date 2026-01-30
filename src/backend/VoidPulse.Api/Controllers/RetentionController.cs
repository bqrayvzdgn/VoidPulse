using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Retention;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/retention")]
[Authorize(Policy = "TenantAdmin")]
public class RetentionController : ControllerBase
{
    private readonly IRetentionPolicyService _retentionPolicyService;
    private readonly ICurrentUserService _currentUser;

    public RetentionController(IRetentionPolicyService retentionPolicyService, ICurrentUserService currentUser)
    {
        _retentionPolicyService = retentionPolicyService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RetentionPolicyResponse>>> Get()
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _retentionPolicyService.GetByTenantAsync(tenantId);
        return Ok(ApiResponse<RetentionPolicyResponse>.Succeed(result));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<RetentionPolicyResponse>>> Update([FromBody] RetentionPolicyRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _retentionPolicyService.SetAsync(tenantId, request);
        return Ok(ApiResponse<RetentionPolicyResponse>.Succeed(result));
    }
}
