using FluentValidation;
using VoidPulse.Application.DTOs.Auth;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Exceptions;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtService _jwtService;
    private readonly ICacheService _cacheService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IJwtService jwtService,
        ICacheService cacheService,
        IPasswordHasher passwordHasher,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;
        _jwtService = jwtService;
        _cacheService = cacheService;
        _passwordHasher = passwordHasher;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        await _registerValidator.ValidateAndThrowAsync(request);

        var existingTenant = await _tenantRepository.GetBySlugAsync(request.TenantSlug);
        if (existingTenant is not null)
            throw new DomainException($"Tenant with slug '{request.TenantSlug}' already exists.");

        var tenant = new Tenant
        {
            Name = request.TenantName,
            Slug = request.TenantSlug,
            IsActive = true
        };
        await _tenantRepository.AddAsync(tenant);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, tenant.Id);
        if (existingUser is not null)
            throw new DomainException($"User with email '{request.Email}' already exists.");

        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            IsActive = true
        };

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        await _userRepository.AddAsync(user);

        // Assign TenantAdmin role to the first user of a new tenant
        var tenantAdminRole = await _roleRepository.GetByNameAsync("TenantAdmin");
        if (tenantAdminRole is not null)
        {
            await _roleRepository.AddUserRoleAsync(user.Id, tenantAdminRole.Id);
        }

        var roles = new List<string> { "TenantAdmin" };
        var accessToken = _jwtService.GenerateAccessToken(user, roles, tenant.Id);

        await _cacheService.SetAsync($"refresh:{refreshToken}", user.Id.ToString(), TimeSpan.FromDays(7));

        return new AuthResponse(
            accessToken,
            refreshToken,
            new UserInfo(user.Id, user.Email, user.FullName, roles));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        await _loginValidator.ValidateAndThrowAsync(request);

        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("User account is deactivated.");

        var userTenant = user.Tenant;
        if (!userTenant.IsActive)
            throw new UnauthorizedException("Tenant is deactivated.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtService.GenerateAccessToken(user, roles, userTenant.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        await _cacheService.SetAsync($"refresh:{refreshToken}", user.Id.ToString(), TimeSpan.FromDays(7));

        return new AuthResponse(
            accessToken,
            refreshToken,
            new UserInfo(user.Id, user.Email, user.FullName, roles));
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
    {
        if (!_jwtService.ValidateRefreshToken(request.RefreshToken))
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);
        if (user is null)
            throw new UnauthorizedException("Invalid refresh token.");

        if (user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired.");

        if (!user.IsActive)
            throw new UnauthorizedException("User account is deactivated.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _jwtService.GenerateAccessToken(user, roles, user.TenantId);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Remove old refresh token from cache
        await _cacheService.RemoveAsync($"refresh:{request.RefreshToken}");

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        await _cacheService.SetAsync($"refresh:{newRefreshToken}", user.Id.ToString(), TimeSpan.FromDays(7));

        return new AuthResponse(
            accessToken,
            newRefreshToken,
            new UserInfo(user.Id, user.Email, user.FullName, roles));
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException(nameof(User), userId);

        if (user.RefreshToken is not null)
        {
            await _cacheService.RemoveAsync($"refresh:{user.RefreshToken}");
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _userRepository.UpdateAsync(user);
    }
}
