using AutoMapper;
using FluentValidation;
using VoidPulse.Application.Common;
using VoidPulse.Application.DTOs.Users;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserRequest> _createValidator;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        IValidator<CreateUserRequest> createValidator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, Guid tenantId)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(User), id);

        if (user.TenantId != tenantId)
            throw new NotFoundException(nameof(User), id);

        return MapToResponse(user);
    }

    public async Task<PagedResult<UserResponse>> GetAllByTenantAsync(Guid tenantId, int page, int pageSize)
    {
        var users = await _userRepository.GetByTenantAsync(tenantId, page, pageSize);
        var totalCount = await _userRepository.CountByTenantAsync(tenantId);

        var responses = users.Select(MapToResponse).ToList().AsReadOnly();

        return new PagedResult<UserResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserResponse> CreateAsync(Guid tenantId, CreateUserRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var existing = await _userRepository.GetByEmailAsync(request.Email, tenantId);
        if (existing is not null)
            throw new DomainException($"User with email '{request.Email}' already exists in this tenant.");

        var user = new User
        {
            TenantId = tenantId,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            IsActive = true
        };

        await _userRepository.AddAsync(user);

        // Roles are typically managed through a separate mechanism (RoleRepository)
        // For now, we return the requested roles
        return new UserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            request.Roles,
            user.LastLoginAt,
            user.CreatedAt);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, Guid tenantId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(User), id);

        if (user.TenantId != tenantId)
            throw new NotFoundException(nameof(User), id);

        if (request.FullName is not null)
            user.FullName = request.FullName;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return MapToResponse(user);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(User), id);

        if (user.TenantId != tenantId)
            throw new NotFoundException(nameof(User), id);

        await _userRepository.DeleteAsync(id);
    }

    private static UserResponse MapToResponse(User user)
    {
        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        return new UserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            roles,
            user.LastLoginAt,
            user.CreatedAt);
    }
}
