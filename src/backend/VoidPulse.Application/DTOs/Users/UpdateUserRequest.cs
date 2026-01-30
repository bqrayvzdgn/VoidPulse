namespace VoidPulse.Application.DTOs.Users;

public record UpdateUserRequest(
    string? FullName,
    bool? IsActive,
    List<string>? Roles);
