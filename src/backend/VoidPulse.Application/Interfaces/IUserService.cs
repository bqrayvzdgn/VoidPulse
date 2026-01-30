using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Users;

namespace VoidPulse.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid id, Guid tenantId);
    Task<PagedResult<UserResponse>> GetAllByTenantAsync(Guid tenantId, int page, int pageSize);
    Task<UserResponse> CreateAsync(Guid tenantId, CreateUserRequest request);
    Task<UserResponse> UpdateAsync(Guid id, Guid tenantId, UpdateUserRequest request);
    Task DeleteAsync(Guid id, Guid tenantId);
}
