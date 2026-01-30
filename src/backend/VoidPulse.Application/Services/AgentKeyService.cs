using System.Security.Cryptography;
using AutoMapper;
using FluentValidation;
using VoidPulse.Application.DTOs.Agents;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class AgentKeyService : IAgentKeyService
{
    private readonly IAgentKeyRepository _agentKeyRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAgentKeyRequest> _createValidator;

    public AgentKeyService(IAgentKeyRepository agentKeyRepository, IMapper mapper, IValidator<CreateAgentKeyRequest> createValidator)
    {
        _agentKeyRepository = agentKeyRepository;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<AgentKeyResponse> GetByIdAsync(Guid id, Guid tenantId)
    {
        var agentKey = await _agentKeyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(AgentKey), id);

        if (agentKey.TenantId != tenantId)
            throw new NotFoundException(nameof(AgentKey), id);

        return new AgentKeyResponse(
            agentKey.Id,
            agentKey.Name,
            null, // API key only shown on create
            agentKey.IsActive,
            agentKey.LastUsedAt,
            agentKey.CreatedAt);
    }

    public async Task<IReadOnlyList<AgentKeyResponse>> GetByTenantAsync(Guid tenantId)
    {
        var agentKeys = await _agentKeyRepository.GetByTenantAsync(tenantId);

        return agentKeys.Select(ak => new AgentKeyResponse(
            ak.Id,
            ak.Name,
            null, // API key only shown on create
            ak.IsActive,
            ak.LastUsedAt,
            ak.CreatedAt)).ToList().AsReadOnly();
    }

    public async Task<AgentKeyResponse> CreateAsync(Guid tenantId, CreateAgentKeyRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var apiKey = GenerateSecureApiKey();

        var agentKey = new AgentKey
        {
            TenantId = tenantId,
            Name = request.Name,
            ApiKey = apiKey,
            IsActive = true
        };

        await _agentKeyRepository.AddAsync(agentKey);

        return new AgentKeyResponse(
            agentKey.Id,
            agentKey.Name,
            apiKey, // Show API key on create
            agentKey.IsActive,
            agentKey.LastUsedAt,
            agentKey.CreatedAt);
    }

    public async Task<AgentKeyResponse> UpdateAsync(Guid id, Guid tenantId, UpdateAgentKeyRequest request)
    {
        var agentKey = await _agentKeyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(AgentKey), id);

        if (agentKey.TenantId != tenantId)
            throw new NotFoundException(nameof(AgentKey), id);

        if (request.Name is not null)
            agentKey.Name = request.Name;

        if (request.IsActive.HasValue)
            agentKey.IsActive = request.IsActive.Value;

        agentKey.UpdatedAt = DateTime.UtcNow;

        await _agentKeyRepository.UpdateAsync(agentKey);

        return new AgentKeyResponse(
            agentKey.Id,
            agentKey.Name,
            null,
            agentKey.IsActive,
            agentKey.LastUsedAt,
            agentKey.CreatedAt);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId)
    {
        var agentKey = await _agentKeyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(AgentKey), id);

        if (agentKey.TenantId != tenantId)
            throw new NotFoundException(nameof(AgentKey), id);

        await _agentKeyRepository.DeleteAsync(id);
    }

    private static string GenerateSecureApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return $"vp_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}";
    }
}
