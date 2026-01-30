using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Tenants;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
[Authorize(Policy = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;

    public TenantsController(ITenantService tenantService, ICurrentUserService currentUser)
    {
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TenantResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _tenantService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<TenantResponse>>.Succeed(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Create([FromBody] CreateTenantRequest request)
    {
        var result = await _tenantService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<TenantResponse>.Succeed(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> GetById([FromRoute] Guid id)
    {
        var result = await _tenantService.GetByIdAsync(id);
        return Ok(ApiResponse<TenantResponse>.Succeed(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateTenantRequest request)
    {
        var result = await _tenantService.UpdateAsync(id, request);
        return Ok(ApiResponse<TenantResponse>.Succeed(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute] Guid id)
    {
        await _tenantService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Succeed(true));
    }
}
