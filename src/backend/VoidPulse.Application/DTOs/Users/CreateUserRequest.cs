namespace VoidPulse.Application.DTOs.Users;

public record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    List<string> Roles);
