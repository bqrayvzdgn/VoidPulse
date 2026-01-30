using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Agents;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/agents")]
[Authorize(Policy = "TenantAdmin")]
public class AgentsController : ControllerBase
{
    private readonly IAgentKeyService _agentKeyService;
    private readonly ICurrentUserService _currentUser;

    public AgentsController(IAgentKeyService agentKeyService, ICurrentUserService currentUser)
    {
        _agentKeyService = agentKeyService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentKeyResponse>>>> GetAll()
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _agentKeyService.GetByTenantAsync(tenantId);
        return Ok(ApiResponse<IReadOnlyList<AgentKeyResponse>>.Succeed(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AgentKeyResponse>>> Create([FromBody] CreateAgentKeyRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _agentKeyService.CreateAsync(tenantId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<AgentKeyResponse>.Succeed(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AgentKeyResponse>>> GetById([FromRoute] Guid id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _agentKeyService.GetByIdAsync(id, tenantId);
        return Ok(ApiResponse<AgentKeyResponse>.Succeed(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AgentKeyResponse>>> Update([FromRoute] Guid id, [FromBody] UpdateAgentKeyRequest request)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var result = await _agentKeyService.UpdateAsync(id, tenantId, request);
        return Ok(ApiResponse<AgentKeyResponse>.Succeed(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromRoute] Guid id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        await _agentKeyService.DeleteAsync(id, tenantId);
        return Ok(ApiResponse<bool>.Succeed(true));
    }
}
