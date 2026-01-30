namespace VoidPulse.Application.DTOs.Tenants;

public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);
