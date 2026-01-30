using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class SavedFilterRepository : Repository<SavedFilter>, ISavedFilterRepository
{
    public SavedFilterRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SavedFilter>> GetByUserAsync(Guid userId, Guid tenantId)
    {
        return await DbSet
            .Where(sf => sf.UserId == userId && sf.TenantId == tenantId)
            .ToListAsync();
    }
}
