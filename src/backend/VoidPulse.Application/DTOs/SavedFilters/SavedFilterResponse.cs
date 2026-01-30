namespace VoidPulse.Application.DTOs.SavedFilters;

public record SavedFilterResponse(
    Guid Id,
    string Name,
    string FilterJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);
