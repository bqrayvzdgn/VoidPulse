using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySlugAsync(string slug);
}
