namespace VoidPulse.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}
