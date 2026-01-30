using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class AgentKeyRepository : Repository<AgentKey>, IAgentKeyRepository
{
    public AgentKeyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<AgentKey?> GetByApiKeyAsync(string apiKey)
    {
        return await DbSet.FirstOrDefaultAsync(ak => ak.ApiKey == apiKey);
    }

    public async Task<IReadOnlyList<AgentKey>> GetByTenantAsync(Guid tenantId)
    {
        return await DbSet
            .Where(ak => ak.TenantId == tenantId)
            .ToListAsync();
    }
}
