using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await DbSet.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task AddUserRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
        Context.UserRoles.Add(userRole);
        await Context.SaveChangesAsync();
    }

    public async Task RemoveUserRolesAsync(Guid userId)
    {
        var userRoles = await Context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        Context.UserRoles.RemoveRange(userRoles);
        await Context.SaveChangesAsync();
    }
}
