using VoidPulse.Domain.Entities;

namespace VoidPulse.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, Guid tenantId);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}
