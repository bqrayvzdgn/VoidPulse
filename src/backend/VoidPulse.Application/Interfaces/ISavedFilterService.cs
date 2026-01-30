using VoidPulse.Application.DTOs.SavedFilters;

namespace VoidPulse.Application.Interfaces;

public interface ISavedFilterService
{
    Task<SavedFilterResponse> GetByIdAsync(Guid id, Guid userId, Guid tenantId);
    Task<IReadOnlyList<SavedFilterResponse>> GetAllAsync(Guid userId, Guid tenantId);
    Task<SavedFilterResponse> CreateAsync(Guid userId, Guid tenantId, CreateSavedFilterRequest request);
    Task<SavedFilterResponse> UpdateAsync(Guid id, Guid userId, Guid tenantId, UpdateSavedFilterRequest request);
    Task DeleteAsync(Guid id, Guid userId, Guid tenantId);
}
