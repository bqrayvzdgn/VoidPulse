using VoidPulse.Application.DTOs.Retention;

namespace VoidPulse.Application.Interfaces;

public interface IRetentionPolicyService
{
    Task<RetentionPolicyResponse> GetByTenantAsync(Guid tenantId);
    Task<RetentionPolicyResponse> SetAsync(Guid tenantId, RetentionPolicyRequest request);
}
