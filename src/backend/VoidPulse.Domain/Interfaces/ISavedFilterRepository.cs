using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface ISavedFilterRepository : IRepository<SavedFilter>
{
    Task<IReadOnlyList<SavedFilter>> GetByUserAsync(Guid userId, Guid tenantId);
}
