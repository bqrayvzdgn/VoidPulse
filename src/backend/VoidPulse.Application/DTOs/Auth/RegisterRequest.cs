namespace VoidPulse.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string TenantName,
    string TenantSlug);
