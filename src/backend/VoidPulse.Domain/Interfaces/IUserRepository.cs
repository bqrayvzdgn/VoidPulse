using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, Guid tenantId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<IReadOnlyList<User>> GetByTenantAsync(Guid tenantId, int page, int pageSize);
    Task<int> CountByTenantAsync(Guid tenantId);
}
