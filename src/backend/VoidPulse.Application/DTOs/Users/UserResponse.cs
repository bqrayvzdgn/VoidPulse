namespace VoidPulse.Application.DTOs.Users;

public record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    List<string> Roles,
    DateTime? LastLoginAt,
    DateTime CreatedAt);
