namespace VoidPulse.Application.DTOs.Agents;

public record AgentKeyResponse(
    Guid Id,
    string Name,
    string? ApiKey,
    bool IsActive,
    DateTime? LastUsedAt,
    DateTime CreatedAt);
