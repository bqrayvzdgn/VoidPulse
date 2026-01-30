using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Tenants;

namespace VoidPulse.Application.Interfaces;

public interface ITenantService
{
    Task<TenantResponse> GetByIdAsync(Guid id);
    Task<PagedResult<TenantResponse>> GetAllAsync(int page, int pageSize);
    Task<TenantResponse> CreateAsync(CreateTenantRequest request);
    Task<TenantResponse> UpdateAsync(Guid id, UpdateTenantRequest request);
    Task DeleteAsync(Guid id);
}
