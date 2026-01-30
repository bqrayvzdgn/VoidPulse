using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class RetentionPolicyRepository : Repository<RetentionPolicy>, IRetentionPolicyRepository
{
    public RetentionPolicyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RetentionPolicy?> GetByTenantAsync(Guid tenantId)
    {
        return await DbSet.FirstOrDefaultAsync(rp => rp.TenantId == tenantId);
    }
}
