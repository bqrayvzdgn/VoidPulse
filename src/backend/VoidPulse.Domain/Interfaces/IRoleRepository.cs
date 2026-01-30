using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
    Task AddUserRoleAsync(Guid userId, Guid roleId);
    Task RemoveUserRolesAsync(Guid userId);
}
