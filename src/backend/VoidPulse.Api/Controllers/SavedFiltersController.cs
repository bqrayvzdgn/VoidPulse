using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.SavedFilters;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/saved-filters")]
[Authorize(Policy = "Analyst")]
public class SavedFiltersController : ControllerBase
{
    private readonly ISavedFilterService _savedFilterService;
    private readonly ICurrentUserService _currentUser;

    public SavedFiltersController(ISavedFilterService savedFilterService, ICurrentUserService currentUser)
    {
        _savedFilterService = savedFilterService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedFilterResponse>>>> GetAll()
    {
        var userId = _currentUser.UserId!.Value;
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _savedFilterService.GetAllAsync(userId, tenantId);
        return Ok(ApiResponse<IReadOnlyList<SavedFilterResponse>>.Succeed(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavedFilterResponse>>> Create([FromBody] CreateSavedFilterRequest request)
    {
        var userId = _currentUser.UserId!.Value;
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _savedFilterService.CreateAsync(userId, tenantId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<SavedFilterResponse>.Succeed(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SavedFilterResponse>>> GetById([FromRoute] Guid id)
    {
        var userId = _currentUser.UserId!.Value;
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _savedFilterService.GetByIdAsync(id, userId, tenantId);
        return Ok(ApiResponse<SavedFilterResponse>.Succeed(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SavedFilterResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateSavedFilterRequest request)
    {
        var userId = _currentUser.UserId!.Value;
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _savedFilterService.UpdateAsync(id, userId, tenantId, request);
        return Ok(ApiResponse<SavedFilterResponse>.Succeed(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute] Guid id)
    {
        var userId = _currentUser.UserId!.Value;
        var tenantId = _currentUser.TenantId!.Value;
        await _savedFilterService.DeleteAsync(id, userId, tenantId);
        return Ok(ApiResponse<bool>.Succeed(true));
    }
}
