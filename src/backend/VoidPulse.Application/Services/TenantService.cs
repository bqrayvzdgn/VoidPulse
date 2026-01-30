using AutoMapper;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Tenants;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;

    public TenantService(ITenantRepository tenantRepository, IMapper mapper)
    {
        _tenantRepository = tenantRepository;
        _mapper = mapper;
    }

    public async Task<TenantResponse> GetByIdAsync(Guid id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Tenant), id);

        return _mapper.Map<TenantResponse>(tenant);
    }

    public async Task<PagedResult<TenantResponse>> GetAllAsync(int page, int pageSize)
    {
        var (items, totalCount) = await _tenantRepository.GetPagedAsync(page, pageSize);

        return new PagedResult<TenantResponse>
        {
            Items = _mapper.Map<IReadOnlyList<TenantResponse>>(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
    {
        var existing = await _tenantRepository.GetBySlugAsync(request.Slug);
        if (existing is not null)
            throw new DomainException($"Tenant with slug '{request.Slug}' already exists.");

        var tenant = _mapper.Map<Tenant>(request);
        tenant.IsActive = true;

        await _tenantRepository.AddAsync(tenant);

        return _mapper.Map<TenantResponse>(tenant);
    }

    public async Task<TenantResponse> UpdateAsync(Guid id, UpdateTenantRequest request)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Tenant), id);

        tenant.Name = request.Name;
        tenant.IsActive = request.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _tenantRepository.UpdateAsync(tenant);

        return _mapper.Map<TenantResponse>(tenant);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Tenant), id);

        await _tenantRepository.DeleteAsync(id);
    }
}
