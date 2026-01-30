using System.Security.Claims;
using VoidPulse.Application.Interfaces;

namespace VoidPulse.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var claim = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var claim = User?.FindFirstValue("tenant_id");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public IEnumerable<string> Roles
    {
        get
        {
            return User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                ?? Enumerable.Empty<string>();
        }
    }

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}
