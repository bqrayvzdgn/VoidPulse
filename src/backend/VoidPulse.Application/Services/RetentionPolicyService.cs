using AutoMapper;
using VoidPulse.Application.DTOs.Retention;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class RetentionPolicyService : IRetentionPolicyService
{
    private readonly IRetentionPolicyRepository _retentionPolicyRepository;
    private readonly IMapper _mapper;

    public RetentionPolicyService(
        IRetentionPolicyRepository retentionPolicyRepository,
        IMapper mapper)
    {
        _retentionPolicyRepository = retentionPolicyRepository;
        _mapper = mapper;
    }

    public async Task<RetentionPolicyResponse> GetByTenantAsync(Guid tenantId)
    {
        var policy = await _retentionPolicyRepository.GetByTenantAsync(tenantId);

        if (policy is null)
        {
            // Return default retention policy
            return new RetentionPolicyResponse(
                Guid.Empty,
                90, // Default 90 days
                DateTime.UtcNow,
                DateTime.UtcNow);
        }

        return _mapper.Map<RetentionPolicyResponse>(policy);
    }

    public async Task<RetentionPolicyResponse> SetAsync(Guid tenantId, RetentionPolicyRequest request)
    {
        var existing = await _retentionPolicyRepository.GetByTenantAsync(tenantId);

        if (existing is not null)
        {
            existing.RetentionDays = request.RetentionDays;
            existing.UpdatedAt = DateTime.UtcNow;
            await _retentionPolicyRepository.UpdateAsync(existing);
            return _mapper.Map<RetentionPolicyResponse>(existing);
        }

        var policy = new RetentionPolicy
        {
            TenantId = tenantId,
            RetentionDays = request.RetentionDays
        };

        await _retentionPolicyRepository.AddAsync(policy);
        return _mapper.Map<RetentionPolicyResponse>(policy);
    }
}
