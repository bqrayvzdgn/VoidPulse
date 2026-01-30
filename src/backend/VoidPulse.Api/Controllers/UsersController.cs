using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Users;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "TenantAdmin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _userService.GetAllByTenantAsync(tenantId, page, pageSize);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Succeed(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] CreateUserRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _userService.CreateAsync(tenantId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<UserResponse>.Succeed(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById([FromRoute] Guid id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _userService.GetByIdAsync(id, tenantId);
        return Ok(ApiResponse<UserResponse>.Succeed(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _userService.UpdateAsync(id, tenantId, request);
        return Ok(ApiResponse<UserResponse>.Succeed(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute] Guid id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        await _userService.DeleteAsync(id, tenantId);
        return Ok(ApiResponse<bool>.Succeed(true));
    }
}
