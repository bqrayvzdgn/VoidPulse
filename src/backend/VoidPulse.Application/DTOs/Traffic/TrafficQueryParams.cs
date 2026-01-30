namespace VoidPulse.Application.DTOs.Traffic;

public record TrafficQueryParams(
    string? SourceIp = null,
    string? DestinationIp = null,
    string? Protocol = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SortBy = null,
    string? SortOrder = null,
    int Page = 1,
    int PageSize = 20);
