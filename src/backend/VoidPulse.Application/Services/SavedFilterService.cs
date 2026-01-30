using AutoMapper;
using FluentValidation;
using VoidPulse.Application.DTOs.SavedFilters;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class SavedFilterService : ISavedFilterService
{
    private readonly ISavedFilterRepository _savedFilterRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSavedFilterRequest> _createValidator;

    public SavedFilterService(ISavedFilterRepository savedFilterRepository, IMapper mapper, IValidator<CreateSavedFilterRequest> createValidator)
    {
        _savedFilterRepository = savedFilterRepository;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<SavedFilterResponse> GetByIdAsync(Guid id, Guid userId, Guid tenantId)
    {
        var filter = await _savedFilterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(SavedFilter), id);

        if (filter.UserId != userId || filter.TenantId != tenantId)
            throw new NotFoundException(nameof(SavedFilter), id);

        return _mapper.Map<SavedFilterResponse>(filter);
    }

    public async Task<IReadOnlyList<SavedFilterResponse>> GetAllAsync(Guid userId, Guid tenantId)
    {
        var filters = await _savedFilterRepository.GetByUserAsync(userId, tenantId);
        return _mapper.Map<IReadOnlyList<SavedFilterResponse>>(filters);
    }

    public async Task<SavedFilterResponse> CreateAsync(Guid userId, Guid tenantId, CreateSavedFilterRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var filter = new SavedFilter
        {
            UserId = userId,
            TenantId = tenantId,
            Name = request.Name,
            FilterJson = request.FilterJson
        };

        await _savedFilterRepository.AddAsync(filter);
        return _mapper.Map<SavedFilterResponse>(filter);
    }

    public async Task<SavedFilterResponse> UpdateAsync(Guid id, Guid userId, Guid tenantId, UpdateSavedFilterRequest request)
    {
        var filter = await _savedFilterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(SavedFilter), id);

        if (filter.UserId != userId || filter.TenantId != tenantId)
            throw new NotFoundException(nameof(SavedFilter), id);

        if (request.Name is not null)
            filter.Name = request.Name;

        if (request.FilterJson is not null)
            filter.FilterJson = request.FilterJson;

        filter.UpdatedAt = DateTime.UtcNow;

        await _savedFilterRepository.UpdateAsync(filter);
        return _mapper.Map<SavedFilterResponse>(filter);
    }

    public async Task DeleteAsync(Guid id, Guid userId, Guid tenantId)
    {
        var filter = await _savedFilterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(SavedFilter), id);

        if (filter.UserId != userId || filter.TenantId != tenantId)
            throw new NotFoundException(nameof(SavedFilter), id);

        await _savedFilterRepository.DeleteAsync(id);
    }
}
